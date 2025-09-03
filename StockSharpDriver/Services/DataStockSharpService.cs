////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;
using SharedLib;
using DbcLib;

namespace StockSharpDriver;

/// <summary>
/// StockSharpDataService
/// </summary>
public class DataStockSharpService(IDbContextFactory<StockSharpAppContext> toolsDbFactory,
    IParametersStorage storageRepo,
    IDbContextFactory<PropertiesStorageContext> cloudParametersDbFactory) : IDataStockSharpService
{
    #region CashFlow
    /// <inheritdoc/>
    public async Task<ResponseBaseModel> CashFlowDelete(int cashFlowId, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        context.CashFlows.RemoveRange(context.CashFlows.Where(x => x.Id == cashFlowId));

        if (await context.SaveChangesAsync(cancellationToken) == 0)
            return ResponseBaseModel.CreateWarning($"CashFlow #{cashFlowId} not found");

        return ResponseBaseModel.CreateSuccess($"Ok. CashFlow #{cashFlowId} deleted");
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> CashFlowUpdateAsync(CashFlowViewModel req, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);

        if (req.Id == 0)
        {
            await context.CashFlows.AddAsync(new()
            {
                Id = req.Id,
                InstrumentId = req.InstrumentId,
                Notional = req.Notional,
                Coupon = req.Coupon,
                CouponRate = req.CouponRate,
                EndDate = req.EndDate,
                StartDate = req.StartDate,
            }, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return ResponseBaseModel.CreateInfo("Ok. CashFlow created");
        }

        CashFlowModelDB cashFlowDb = await context.CashFlows.FirstAsync(x => x.Id == req.Id, cancellationToken: cancellationToken);
        cashFlowDb.SetUpdate(req);
        context.CashFlows.Update(cashFlowDb);
        await context.SaveChangesAsync(cancellationToken);
        return ResponseBaseModel.CreateSuccess("Ok. CashFlow updated");
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<List<CashFlowViewModel>>> CashFlowList(int instrumentId, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        List<CashFlowModelDB> res = await context.CashFlows
            .Where(x => x.InstrumentId == instrumentId)
            .OrderBy(x => x.StartDate)
            .ToListAsync(cancellationToken: cancellationToken);

        return new()
        {
            Response = [.. res.Select(x => new CashFlowViewModel()
            {
                Id = x.Id,
                InstrumentId = x.InstrumentId,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                CouponRate = x.CouponRate,
                Coupon = x.Coupon,
                Notional = x.Notional,
            })],
        };
    }
    #endregion

    #region instrument`s
    /// <inheritdoc/>
    public async Task<ResponseBaseModel> UpdateInstrumentAsync(InstrumentTradeStockSharpViewModel req, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        InstrumentStockSharpModelDB instrumentDb = await context.Instruments.FirstAsync(x => x.Id == req.Id, cancellationToken: cancellationToken);
        instrumentDb.SetUpdate(req, true);

        context.Update(instrumentDb);
        await context.SaveChangesAsync(cancellationToken);
        return ResponseBaseModel.CreateSuccess("Ok. Instrument updated");
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<List<InstrumentTradeStockSharpViewModel>>> GetInstrumentsAsync(int[]? ids = null, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<InstrumentStockSharpModelDB> q = ids is null || ids.Length == 0
            ? context.Instruments.AsQueryable()
            : context.Instruments.Where(x => ids.Contains(x.Id));

        List<InstrumentStockSharpModelDB> data = await q
            .Include(x => x.Markers)
            .Include(x => x.Board)
            .ThenInclude(x => x!.Exchange)
            .ToListAsync(cancellationToken: cancellationToken);

        return new()
        {
            Response = [.. data]
        };
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<List<MarkerInstrumentStockSharpViewModel>>> GetMarkersForInstrumentAsync(int instrumentId, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        InstrumentStockSharpModelDB? instrumentDb = await context
            .Instruments
            .Include(x => x.Markers)
            .FirstOrDefaultAsync(x => x.Id == instrumentId, cancellationToken: cancellationToken);

        TResponseModel<List<MarkerInstrumentStockSharpViewModel>> res = new();

        if (instrumentDb?.Markers is null)
        {
            res.AddError($"Instrument #{instrumentId} not found");
            return res;
        }
        res.Response = [.. instrumentDb.Markers.Select(x=> new MarkerInstrumentStockSharpViewModel()
        {
            Id = x.Id,
            MarkerDescriptor = x.MarkerDescriptor,
        })];
        return res;
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> SetMarkersForInstrumentAsync(SetMarkersForInstrumentRequestModel req, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<InstrumentMarkersModelDB> q = context.InstrumentsMarkers.Where(x => x.InstrumentId == req.InstrumentId);

        if (req.SetMarkers is null || req.SetMarkers.Length == 0)
        {
            if (!await q.AnyAsync(cancellationToken: cancellationToken))
                return ResponseBaseModel.CreateInfo($"{nameof(SetMarkersForInstrumentAsync)}: no exist thin");

            context.InstrumentsMarkers.RemoveRange(q);
            int res = await context.SaveChangesAsync(cancellationToken);
            return res == 0
                ? ResponseBaseModel.CreateInfo($"{nameof(SetMarkersForInstrumentAsync)}: no changes caused")
                : ResponseBaseModel.CreateInfo($"{nameof(SetMarkersForInstrumentAsync)}: {res} elements removed");
        }

        InstrumentMarkersModelDB[] markersDb = await q.ToArrayAsync(cancellationToken: cancellationToken);
        int _resCount = 0;
        InstrumentMarkersModelDB[] _markers = [.. markersDb.Where(x => !req.SetMarkers.Contains(x.MarkerDescriptor))];
        if (_markers.Length != 0)
        {
            context.InstrumentsMarkers.RemoveRange(_markers);
            _resCount += await context.SaveChangesAsync(cancellationToken);
        }

        _markers = [..req.SetMarkers
            .Where(x => !markersDb.Any(y => y.MarkerDescriptor == x))
            .Select(x => new InstrumentMarkersModelDB()
            {
                InstrumentId = req.InstrumentId,
                MarkerDescriptor = x,
            })];

        if (_markers.Length != 0)
        {
            await context.InstrumentsMarkers.AddRangeAsync(_markers, cancellationToken);
            _resCount += await context.SaveChangesAsync(cancellationToken);
        }

        return ResponseBaseModel.CreateInfo($"changed items: {_resCount}");
    }

    /// <inheritdoc/>
    public async Task<TPaginationResponseModel<InstrumentTradeStockSharpViewModel>> InstrumentsSelectAsync(InstrumentsRequestModel req, CancellationToken cancellationToken = default)
    {
        if (req.PageSize < 10)
            req.PageSize = 10;

        MarkersInstrumentStockSharpEnum[] _allMarkers = [.. Enum.GetValues<MarkersInstrumentStockSharpEnum>()];

        bool _notSet = req.MarkersFilter?.Contains(null) == true;
        IEnumerable<MarkersInstrumentStockSharpEnum>? _woq = req.MarkersFilter?.Where(x => x is not null).Select(x => x!.Value);

        MarkersInstrumentStockSharpEnum[]? markersFilterShow = _woq is null || !_woq.Any()
            ? null
            : [.. _woq];

        MarkersInstrumentStockSharpEnum[]? markersFilterSkip = markersFilterShow is null || markersFilterShow.Length == 0 || markersFilterShow.Length == _allMarkers.Length
           ? null
           : [.. _allMarkers.Where(x => !markersFilterShow.Contains(x))];

        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<InstrumentStockSharpModelDB> q = context.Instruments.AsQueryable();

        if (markersFilterShow is null || markersFilterShow.Length == 0)
        {
            if (_notSet)
                q = q.Where(x => !context.InstrumentsMarkers.Any(y => y.InstrumentId == x.Id));
        }
        else
        {
            if (markersFilterSkip is not null && markersFilterSkip.Length != 0)
                q = q.Where(x => !context.InstrumentsMarkers.Any(y => y.InstrumentId == x.Id && markersFilterSkip.Contains(y.MarkerDescriptor)));

            if (!_notSet)
                q = q.Where(x => context.InstrumentsMarkers.Any(y => y.InstrumentId == x.Id));
            else
                q = q.Where(x => context.InstrumentsMarkers.Any(y => markersFilterShow.Contains(y.MarkerDescriptor)));
        }

        if (req.BoardsFilter is not null && req.BoardsFilter.Length != 0)
            q = q.Where(x => req.BoardsFilter.Any(y => y == x.BoardId));

        if (req.CurrenciesFilter is not null && req.CurrenciesFilter.Length != 0)
            q = q.Where(x => req.CurrenciesFilter.Any(y => y == x.Currency));

        if (req.TypesFilter is not null && req.TypesFilter.Length != 0)
        {
            q = q.Where(x => req.TypesFilter.Any(y => y == x.TypeInstrument));
        }

        if (!string.IsNullOrWhiteSpace(req.FindQuery))
        {
            req.FindQuery = req.FindQuery.ToUpper();
            q = q.Where(x => x.IdRemoteNormalizedUpper != null && EF.Functions.Like(x.IdRemoteNormalizedUpper, $"%{req.FindQuery}%") || (x.NameNormalizedUpper != null && EF.Functions.Like(x.NameNormalizedUpper, $"%{req.FindQuery}%")));
        }

        List<InstrumentStockSharpModelDB> _data = await q
            .Include(x => x.Markers)
            .Include(x => x.Board)
            .ThenInclude(x => x!.Exchange)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Board!.Code)
            .Skip(req.PageSize * req.PageNum)
            .Take(req.PageSize)
            .ToListAsync(cancellationToken: cancellationToken);

        // string sql = q.ToQueryString();

        TPaginationResponseModel<InstrumentTradeStockSharpViewModel> res = new()
        {
            PageSize = req.PageSize,
            PageNum = req.PageNum,
            SortBy = req.SortBy,
            TotalRowsCount = await q.CountAsync(cancellationToken: cancellationToken),
            SortingDirection = req.SortingDirection,
            Response = [.. _data.Select(x => new InstrumentTradeStockSharpViewModel().Bind(x))]
        };

        return res;
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<List<InstrumentTradeStockSharpViewModel>>> ReadTradeInstrumentsAsync(CancellationToken cancellationToken = default)
    {
        int[]? _boardsFilter = default;
        MarkersInstrumentStockSharpEnum?[]? _markersFilter = default;
        await Task.WhenAll([
            Task.Run(async () => { _markersFilter = await storageRepo.ReadAsync<MarkersInstrumentStockSharpEnum?[]>(GlobalStaticCloudStorageMetadata.MarkersDashboard); }, cancellationToken),
            Task.Run(async () => { _boardsFilter = await storageRepo.ReadAsync<int[]>(GlobalStaticCloudStorageMetadata.BoardsDashboard); }, cancellationToken)]);

        InstrumentsRequestModel req = new()
        {
            PageNum = 0,
            PageSize = int.MaxValue,
        };

        if (_markersFilter is not null)
            req.MarkersFilter = _markersFilter;

        if (_boardsFilter is not null)
            req.BoardsFilter = [.. _boardsFilter];

        TPaginationResponseModel<InstrumentTradeStockSharpViewModel> res = await InstrumentsSelectAsync(req, cancellationToken);

        return new()
        {
            Response = res.Response,
        };
    }
    #endregion

    #region rubrics/instruments
    /// <inheritdoc/>
    public async Task<ResponseBaseModel> RubricsInstrumentUpdateAsync(RubricsInstrumentUpdateModel req, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);

        if (req.RubricsIds is null || req.RubricsIds.Length != 1)
        {
            context.RemoveRange(context.RubricsInstruments.Where(x => x.InstrumentId == req.InstrumentId));
            return ResponseBaseModel.CreateInfo($"Ok (clear rubrics for instrument). changes: {await context.SaveChangesAsync(cancellationToken)}");
        }

        int[] rubricsIdsDb = await context.RubricsInstruments
            .Where(x => x.InstrumentId == req.InstrumentId)
            .Select(x => x.RubricId)
            .ToArrayAsync(cancellationToken: cancellationToken);

        ResponseBaseModel _final = new();

        int[] _rubricsIds = [.. rubricsIdsDb.Where(x => !req.RubricsIds.Contains(x))];
        if (_rubricsIds.Length != 0)
        {
            context.RemoveRange(context.RubricsInstruments.Where(x => x.InstrumentId == req.InstrumentId && _rubricsIds.Contains(x.RubricId)));
            _final.AddInfo($"Deleted link`s: {await context.SaveChangesAsync(cancellationToken)}");
        }

        _rubricsIds = [.. req.RubricsIds.Where(x => !rubricsIdsDb.Contains(x))];
        if (_rubricsIds.Length != 0)
        {
            try
            {
                await context.AddRangeAsync(_rubricsIds.Select(x => new RubricInstrumentStockSharpModelDB() { RubricId = x, InstrumentId = req.InstrumentId }), cancellationToken);
                _final.AddInfo($"Added link`s: {await context.SaveChangesAsync(cancellationToken)}");
            }
            catch (Exception ex)
            {
                _final.Messages.InjectException(ex);
            }
        }

        return _final;
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> InstrumentRubricUpdateAsync(InstrumentRubricUpdateModel req, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);

        if (req.Set)
        {
            if (await context.RubricsInstruments.AnyAsync(x => x.RubricId == req.RubricId && x.InstrumentId == req.InstrumentId, cancellationToken: cancellationToken))
                return ResponseBaseModel.CreateInfo($"Инструмент уже находится в рубрике");

            RubricInstrumentStockSharpModelDB? _otherDb = await context.RubricsInstruments
                .FirstOrDefaultAsync(x => x.RubricId != req.RubricId && x.InstrumentId == req.InstrumentId, cancellationToken: cancellationToken);

            if (_otherDb is not null)
            {
                PropertiesStorageContext storeCtx = await cloudParametersDbFactory.CreateDbContextAsync(cancellationToken);
                RubricModelDB? rubricDb = await storeCtx.Rubrics.FirstOrDefaultAsync(x => x.Id == _otherDb.RubricId, cancellationToken: cancellationToken);
                if (rubricDb is not null)
                    return ResponseBaseModel.CreateError($"Instrument abuse: #{rubricDb.Id} {rubricDb.Name}");
            }

            try
            {
                await context.RubricsInstruments.AddAsync(new()
                {
                    InstrumentId = req.InstrumentId,
                    RubricId = req.RubricId,
                }, cancellationToken);
                context.RemoveRange(context.RubricsInstruments.Where(x => x.RubricId != req.RubricId && x.InstrumentId == req.InstrumentId));
                await context.SaveChangesAsync(cancellationToken);
                return ResponseBaseModel.CreateSuccess($"Инструмент добавлен в рубрику");
            }
            catch (Exception ex)
            {
                return ResponseBaseModel.CreateError(ex);
            }
        }
        //else
        //{
        context.RemoveRange(context.RubricsInstruments.Where(x => x.RubricId == req.RubricId && x.InstrumentId == req.InstrumentId));
        if (await context.SaveChangesAsync(cancellationToken) == 0)
            return ResponseBaseModel.CreateInfo($"Удаление связи не требуется (отсутствует)");

        return ResponseBaseModel.CreateInfo($"Инструмент успешно исключён");
        //}
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<List<UniversalBaseModel>>> GetRubricsForInstrumentAsync(int idInstrument, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext contextMain = await toolsDbFactory.CreateDbContextAsync(cancellationToken);

        int[] resRubricsDbIds = await contextMain.RubricsInstruments
            .Where(x => x.InstrumentId == idInstrument)
            .Select(x => x.RubricId)
            .Distinct()
            .ToArrayAsync(cancellationToken: cancellationToken);

        PropertiesStorageContext contextProps = await cloudParametersDbFactory.CreateDbContextAsync(cancellationToken);
        return new()
        {
            Response = await contextProps.Rubrics
            .Where(x => resRubricsDbIds.Contains(x.Id))
            .Select(x => new UniversalBaseModel()
            {
                Id = x.Id,
                CreatedAtUTC = x.CreatedAtUTC,
                Description = x.Description,
                IsDisabled = x.IsDisabled,
                LastUpdatedAtUTC = x.LastUpdatedAtUTC,
                Name = x.Name,
                ParentId = x.ParentId,
                ProjectId = x.ProjectId,
                SortIndex = x.SortIndex,
            })
            .ToListAsync(cancellationToken: cancellationToken)
        };
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<List<InstrumentTradeStockSharpViewModel>>> GetInstrumentsForRubricAsync(int idRubric, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);

        List<InstrumentStockSharpModelDB> resInstrumentsDb = await context.Instruments
            .Where(x => context.RubricsInstruments.Any(y => y.RubricId == idRubric && x.Id == y.InstrumentId))
            .Include(x => x.Board)
            .ToListAsync(cancellationToken: cancellationToken);

        return new()
        {
            Response = [.. resInstrumentsDb.Select(x => new InstrumentTradeStockSharpViewModel().Build(x))]
        };
    }
    #endregion

    #region board`s
    /// <inheritdoc/>
    public async Task<TResponseModel<List<BoardStockSharpViewModel>>> GetBoardsAsync(int[]? ids = null, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<BoardStockSharpModelDB> q = ids is null || ids.Length == 0
            ? context.Boards.AsQueryable()
            : context.Boards.Where(x => ids.Contains(x.Id));

        List<BoardStockSharpModelDB> data = await q
            .Include(x => x.Exchange)
            .ToListAsync(cancellationToken: cancellationToken);

        return new()
        {
            Response = [.. await InjectStatistic(data, context, cancellationToken)]
        };
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<List<BoardStockSharpViewModel>>> FindBoardsAsync(BoardStockSharpModel req, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        ExchangeStockSharpModelDB? _exc = req.Exchange is null
            ? null
            : await context.Exchanges.SingleAsync(x => x.Name == req.Exchange.Name && x.CountryCode == req.Exchange.CountryCode, cancellationToken: cancellationToken);

        IQueryable<BoardStockSharpModelDB> q = context.Boards.AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Code))
            q = q.Where(x => x.Code != null && EF.Functions.Like(x.Code, $"%{req.Code}%"));

        if (_exc is not null)
            q = q.Where(x => x.ExchangeId == _exc.Id);

        List<BoardStockSharpModelDB> data = await q
            .Include(x => x.Exchange)
            .ToListAsync(cancellationToken: cancellationToken);

        return new()
        {
            Response = [.. await InjectStatistic(data, context, cancellationToken)]
        };
    }

    static async Task<List<BoardStockSharpModelDB>> InjectStatistic(List<BoardStockSharpModelDB> data, StockSharpAppContext context, CancellationToken cancellationToken = default)
    {
        int[] ids = [.. data.Select(x => x.Id)];

        var query = from p in context.Instruments.Where(x => ids.Contains(x.BoardId))
                    group p by p.BoardId into g
                    select new
                    {
                        boardId = g.Key,
                        count = g.Count()
                    };
        var _statDb = await query.ToArrayAsync(cancellationToken: cancellationToken);

        if (_statDb.Count(x => x.count != 0) <= 1)
            return data;

        DateTime? lastConnectedAt = DriverStockSharpService.LastConnectedAt;

        if (lastConnectedAt is not null)
        {
            query = from p in context.Instruments.Where(x => x.LastUpdatedAtUTC >= lastConnectedAt && ids.Contains(x.BoardId))
                    group p by p.BoardId into g
                    select new
                    {
                        boardId = g.Key,
                        count = g.Count()
                    };
            var _subStatDb = await query.ToArrayAsync(cancellationToken: cancellationToken);
            data.ForEach(x =>
            {
                var _cs = _statDb.FirstOrDefault(y => y.boardId == x.Id);
                var _cs2 = _subStatDb.FirstOrDefault(y => y.boardId == x.Id);
                x.Code += $" [x{_cs?.count ?? 0} /✡{_cs2?.count ?? 0}]";
            });
            return data;
        }

        data.ForEach(x =>
        {
            var _cs = _statDb.FirstOrDefault(y => y.boardId == x.Id);
            x.Code += $" [x{_cs?.count ?? 0}]";
        });

        return data;
    }

    #endregion

    /// <inheritdoc/>
    public async Task<TResponseModel<List<ExchangeStockSharpModel>>> GetExchangesAsync(int[]? ids = null, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<ExchangeStockSharpModelDB> q = ids is null || ids.Length == 0
            ? context.Exchanges.AsQueryable()
            : context.Exchanges.Where(x => ids.Contains(x.Id));
        List<ExchangeStockSharpModelDB> data = await q.Include(x => x.Boards).ToListAsync(cancellationToken: cancellationToken);

        return new()
        {
            Response = [.. data]
        };
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<List<OrderStockSharpModel>>> GetOrdersAsync(int[]? ids = null, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<OrderStockSharpModelDB> q = ids is null || ids.Length == 0
            ? context.Orders.AsQueryable()
            : context.Orders.Where(x => ids.Contains(x.IdPK));
        List<OrderStockSharpModelDB> data = await q.Include(x => x.Instrument).Include(x => x.Portfolio).ToListAsync(cancellationToken: cancellationToken);

        return new()
        {
            Response = [.. data]
        };
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<List<PortfolioStockSharpViewModel>>> GetPortfoliosAsync(int[]? ids = null, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<PortfolioTradeModelDB> q = ids is null || ids.Length == 0
            ? context.Portfolios.AsQueryable()
            : context.Portfolios.Where(x => ids.Contains(x.Id));
        List<PortfolioTradeModelDB> data = await q.Include(x => x.Board).ThenInclude(x => x!.Exchange).ToListAsync(cancellationToken: cancellationToken);

        return new()
        {
            Response = [.. data]
        };
    }
}