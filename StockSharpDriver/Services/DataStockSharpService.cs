////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using DbcLib;
using Microsoft.EntityFrameworkCore;
using SharedLib;

namespace StockSharpDriver;

/// <summary>
/// StockSharpDataService
/// </summary>
public class DataStockSharpService(IDbContextFactory<StockSharpAppContext> toolsDbFactory, IDbContextFactory<PropertiesStorageContext> cloudParametersDbFactory) : IDataStockSharpService
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
                CashFlowType = req.CashFlowType,
                InstrumentId = req.InstrumentId,
                PaymentDate = req.PaymentDate,
                PaymentValue = req.PaymentValue,
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
            .OrderBy(x => x.PaymentDate)
            .ToListAsync(cancellationToken: cancellationToken);

        return new()
        {
            Response = [.. res.Select(x => new CashFlowViewModel()
            {
                PaymentValue = x.PaymentValue,
                CashFlowType = x.CashFlowType,
                PaymentDate = x.PaymentDate,
                Id = x.Id,
                InstrumentId = x.InstrumentId,
            })],
        };
    }
    #endregion

    #region Instrument
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
    public async Task<TResponseModel<List<InstrumentTradeStockSharpViewModel>>> GetInstrumentsAsync(int[] ids = null, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<InstrumentStockSharpModelDB> q = ids is null || ids.Length == 0
            ? context.Instruments.AsQueryable()
            : context.Instruments.Where(x => ids.Contains(x.Id));

        List<InstrumentStockSharpModelDB> data = await q
            .Include(x => x.Board)
            .ThenInclude(x => x.Exchange)
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
        InstrumentStockSharpModelDB instrumentDb = await context
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
        InstrumentMarkersModelDB[] _markers = [.. markersDb.Where(x => !req.SetMarkers.Select(x => (int)x).Contains(x.MarkerDescriptor))];
        if (_markers.Length != 0)
        {
            context.InstrumentsMarkers.RemoveRange(_markers);
            _resCount += await context.SaveChangesAsync(cancellationToken);
        }

        _markers = [..req.SetMarkers
            .Where(x => !markersDb.Any(y => y.MarkerDescriptor == (int)x))
            .Select(x => new InstrumentMarkersModelDB()
            {
                InstrumentId = req.InstrumentId,
                MarkerDescriptor = (int)x,
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
        bool _notSet = req.MarkersFilter?.Contains(null) == true;
        int[] markersFilter = req.MarkersFilter is null ? null : [.. req.MarkersFilter.Where(x => x is not null).Select(x => (int)x)];
        // 
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<InstrumentStockSharpModelDB> q = context
            .Instruments
            .Where(x => (markersFilter == null || (markersFilter.Length == 0 && !_notSet)) || context.InstrumentsMarkers.Any(y => y.InstrumentId == x.Id && markersFilter.Any(z => z == y.MarkerDescriptor)) || (_notSet && !context.InstrumentsMarkers.Any(y => y.InstrumentId == x.Id)))
            .AsQueryable();

        if (req.BoardsFilter is not null && req.BoardsFilter.Length != 0)
            q = q.Where(x => req.BoardsFilter.Any(y => y == x.BoardId));

        if (req.CurrenciesFilter is not null && req.CurrenciesFilter.Length != 0)
        {
            int[] ids = [.. req.CurrenciesFilter.Cast<int>()];
            q = q.Where(x => ids.Any(y => y == x.Currency));
        }

        if (req.TypesFilter is not null && req.TypesFilter.Length != 0)
        {
            int[] ids = [.. req.TypesFilter.Cast<int>()];
            q = q.Where(x => ids.Any(y => y == x.TypeInstrument));
        }

        if (!string.IsNullOrWhiteSpace(req.FindQuery))
        {
            req.FindQuery = req.FindQuery.ToUpper();
            q = q.Where(x => EF.Functions.Like(x.IdRemoteNormalizedUpper, $"%{req.FindQuery}%") || EF.Functions.Like(x.NameNormalizedUpper, $"%{req.FindQuery}%"));
        }

        List<InstrumentStockSharpModelDB> _data = await q
            .Include(x => x.Markers)
            .Include(x => x.Board)
            .ThenInclude(x => x.Exchange)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Board.Code)
            .Skip(req.PageSize * req.PageNum)
            .Take(req.PageSize)
            .ToListAsync(cancellationToken: cancellationToken);

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
    #endregion

    #region rubrics/instruments
    /// <inheritdoc/>
    public async Task<ResponseBaseModel> RubricsInstrumentUpdateAsync(RubricsInstrumentUpdateModel req, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);

        if (req.RubricsIds is null || req.RubricsIds.Length != 1)
        {
            context.RemoveRange(context.RubricsInstruments.Where(x => x.InstrumentId == req.InstrumentId));
            return ResponseBaseModel.CreateInfo($"Ok. changes: {await context.SaveChangesAsync(cancellationToken)}");
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
            _final.AddInfo($"Удалено связей: {await context.SaveChangesAsync(cancellationToken)}");
        }

        _rubricsIds = [.. req.RubricsIds.Where(x => !rubricsIdsDb.Contains(x))];
        if (_rubricsIds.Length != 0)
        {
            try
            {
                await context.AddRangeAsync(_rubricsIds.Select(x => new RubricInstrumentStockSharpModelDB() { RubricId = x, InstrumentId = req.InstrumentId }), cancellationToken);
                _final.AddInfo($"Добавлено связей: {await context.SaveChangesAsync(cancellationToken)}");
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

            RubricInstrumentStockSharpModelDB _otherDb = await context.RubricsInstruments
                .FirstOrDefaultAsync(x => x.RubricId != req.RubricId && x.InstrumentId == req.InstrumentId, cancellationToken: cancellationToken);

            if (_otherDb is not null)
            {
                PropertiesStorageContext storeCtx = await cloudParametersDbFactory.CreateDbContextAsync(cancellationToken);
                RubricModelDB rubricDb = await storeCtx.Rubrics.FirstOrDefaultAsync(x => x.Id == _otherDb.RubricId, cancellationToken: cancellationToken);
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

    /// <inheritdoc/>
    public async Task<TResponseModel<List<BoardStockSharpViewModel>>> GetBoardsAsync(int[] ids = null, CancellationToken cancellationToken = default)
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
            Response = [.. data]
        };
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<List<ExchangeStockSharpModel>>> GetExchangesAsync(int[] ids = null, CancellationToken cancellationToken = default)
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
    public async Task<TResponseModel<List<OrderStockSharpModel>>> GetOrdersAsync(int[] ids = null, CancellationToken cancellationToken = default)
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
    public async Task<TResponseModel<List<PortfolioStockSharpViewModel>>> GetPortfoliosAsync(int[] ids = null, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<PortfolioTradeModelDB> q = ids is null || ids.Length == 0
            ? context.Portfolios.AsQueryable()
            : context.Portfolios.Where(x => ids.Contains(x.Id));
        List<PortfolioTradeModelDB> data = await q.Include(x => x.Board).ThenInclude(x => x.Exchange).ToListAsync(cancellationToken: cancellationToken);

        return new()
        {
            Response = [.. data]
        };
    }

    /// <inheritdoc/>
    public Task<ResponseBaseModel> PingAsync(CancellationToken cancellationToken = default)
    {
        //StockSharp.Algo.Connector Connector = new();
        return Task.FromResult(ResponseBaseModel.CreateSuccess($"Ok - {nameof(DriverStockSharpService)}"));
    }
}