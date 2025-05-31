////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;
using StockSharp.Algo;
using SharedLib;
using DbcLib;

namespace StockSharpDriver;

/// <summary>
/// StockSharpDataService
/// </summary>
public class DataStockSharpService(IDbContextFactory<StockSharpAppContext> toolsDbFactory) : IDataStockSharpService
{
    /// <inheritdoc/>
    public async Task<TResponseModel<List<InstrumentTradeStockSharpViewModel>>> GetInstrumentsAsync(int[] ids = null, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<InstrumentStockSharpModelDB> q = ids is null || ids.Length == 0
            ? context.Instruments.AsQueryable()
            : context.Instruments.Where(x => ids.Contains(x.Id));
        List<InstrumentStockSharpModelDB> data = await q.Include(x => x.Board).ThenInclude(x => x.Exchange).ToListAsync(cancellationToken: cancellationToken);

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
    public async Task<TResponseModel<List<BoardStockSharpModel>>> GetBoardsAsync(int[] ids = null, CancellationToken cancellationToken = default)
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
    public async Task<TPaginationResponseModel<InstrumentTradeStockSharpViewModel>> InstrumentsSelectAsync(InstrumentsRequestModel req, CancellationToken cancellationToken = default)
    {
        if (req.PageSize < 10)
            req.PageSize = 10;

        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<InstrumentStockSharpModelDB> q = context
            .Instruments
            .Where(x => req.FavoriteFilter == null || x.IsFavorite == req.FavoriteFilter)
            .AsQueryable();

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

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> InstrumentFavoriteToggleAsync(InstrumentTradeStockSharpViewModel req, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        InstrumentStockSharpModelDB instrumentDb = await context.Instruments.FirstOrDefaultAsync(x => x.IdRemote == req.IdRemote, cancellationToken: cancellationToken);
        if (instrumentDb is null)
            return ResponseBaseModel.CreateError("Инструмент не найден");

        instrumentDb.IsFavorite = !instrumentDb.IsFavorite;
        context.Update(instrumentDb);
        await context.SaveChangesAsync(cancellationToken: cancellationToken);

        return ResponseBaseModel.CreateSuccess("Запрос выполнен");
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