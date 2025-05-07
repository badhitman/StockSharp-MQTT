////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
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
    public Task<TResponseModel<int>> SaveBoard(BoardStockSharpModel req)
    {
        using StockSharpAppContext context = toolsDbFactory.CreateDbContext();
        ExchangeStockSharpModelDB exchange = null;
        if (!string.IsNullOrWhiteSpace(req.Exchange.Name))
        {
            SaveExchange(req.Exchange);
            exchange = context.Exchanges.First(x => x.Name == req.Exchange.Name && x.CountryCode == req.Exchange.CountryCode);
        }
        bool withOutExchange = exchange is null;
        BoardStockSharpModelDB boardDb = context.Boards
            .FirstOrDefault(x => x.Code == req.Code && (withOutExchange || x.ExchangeId == exchange.Id));
        if (boardDb is null)
        {
            boardDb = new BoardStockSharpModelDB().Bind(req);

            boardDb.ExchangeId = exchange.Id;
            boardDb.Exchange = null;

            context.Add(boardDb);
        }
        else
        {
            boardDb.SetUpdate(req);
            context.Update(boardDb);
        }
        context.SaveChanges();
        return Task.FromResult(new TResponseModel<int>() { Response = boardDb.Id });
    }

    /// <inheritdoc/>
    public Task<TResponseModel<int>> SaveExchange(ExchangeStockSharpModel req)
    {
        using StockSharpAppContext context = toolsDbFactory.CreateDbContext();
        ExchangeStockSharpModelDB exchangeDb = context.Exchanges
            .FirstOrDefault(x => x.Name == req.Name && x.CountryCode == req.CountryCode);
        if (exchangeDb is null)
        {
            exchangeDb = new ExchangeStockSharpModelDB().Bind(req);
            context.Add(exchangeDb);
        }
        else
        {
            exchangeDb.SetUpdate(req);
            context.Update(exchangeDb);
        }
        context.SaveChanges();
        return Task.FromResult(new TResponseModel<int>() { Response = exchangeDb.Id });
    }

    /// <inheritdoc/>
    public Task<TResponseModel<int>> SaveInstrument(InstrumentTradeStockSharpModel req)
    {
        using StockSharpAppContext context = toolsDbFactory.CreateDbContext();
        BoardStockSharpModelDB board = null;
        if (!string.IsNullOrWhiteSpace(req.Board.Code))
            board = context.Boards.First(x => x.Id == SaveBoard(req.Board).Result.Response);

        InstrumentStockSharpModelDB instrumentDb = context.Instruments
            .FirstOrDefault(x => x.Name == req.Name && x.Code == req.Code && x.BoardId == board.Id);

        if (instrumentDb is null)
        {
            instrumentDb = new InstrumentStockSharpModelDB().Bind(req);
            instrumentDb.CreatedAtUTC = DateTime.UtcNow;
            instrumentDb.BoardId = board.Id;
            instrumentDb.Board = null;

            context.Add(instrumentDb);
        }
        else
        {
            instrumentDb.SetUpdate(req);
            context.Update(instrumentDb);
        }
        context.SaveChanges();
        return Task.FromResult(new TResponseModel<int>() { Response = instrumentDb.Id });
    }

    /// <inheritdoc/>
    public Task<TResponseModel<int>> SavePortfolio(PortfolioStockSharpModel req)
    {
        using StockSharpAppContext context = toolsDbFactory.CreateDbContext();
        BoardStockSharpModelDB board = null;
        if (!string.IsNullOrWhiteSpace(req.Board?.Code))
            board = context.Boards.First(x => x.Id == SaveBoard(req.Board).Result.Response);

        IQueryable<PortfolioTradeModelDB> q = context.Portfolios
            .Where(x => x.Name == req.Name && x.DepoName == req.DepoName && x.Currency == req.Currency);

        PortfolioTradeModelDB portDb = board == null
            ? q.FirstOrDefault(x => x.BoardId == null)
            : q.FirstOrDefault(x => x.BoardId == board.Id);

        if (portDb is null)
        {
            portDb = new PortfolioTradeModelDB().Bind(req);
            portDb.CreatedAtUTC = DateTime.UtcNow;
            portDb.BoardId = board?.Id;
            portDb.Board = null;

            context.Add(portDb);
        }
        else
        {
            portDb.SetUpdate(req);
            context.Update(portDb);
        }
        context.SaveChanges();
        return Task.FromResult(new TResponseModel<int>() { Response = portDb.Id });
    }

    public Task<TResponseModel<int>> SaveOrder(OrderStockSharpModel req)
    {
        using StockSharpAppContext context = toolsDbFactory.CreateDbContext();
        OrderStockSharpModelDB orderDb = context.Orders.FirstOrDefault(x => x.TransactionId == req.TransactionId);

        InstrumentStockSharpModelDB instrumentDb = null;
        if (!string.IsNullOrWhiteSpace(req.Instrument.Name))
            instrumentDb = context.Instruments.First(x => x.Id == SaveInstrument(req.Instrument).Result.Response);

        PortfolioTradeModelDB portfolioDb = null;
        if (!string.IsNullOrWhiteSpace(req.Portfolio.Name))
        {
            portfolioDb = context.Portfolios
                .FirstOrDefault(x =>
                x.Name == req.Portfolio.Name &&
                x.DepoName == req.Portfolio.DepoName &&
                x.ClientCode == req.Portfolio.ClientCode &&
                x.Currency == req.Portfolio.Currency);

            portfolioDb ??= context.Portfolios.First(x => x.Id == SavePortfolio(req.Portfolio).Result.Response);
        }

        if (orderDb is null)
        {
            orderDb = new OrderStockSharpModelDB().Bind(req);
            orderDb.CreatedAtUTC = DateTime.UtcNow;
            orderDb.InstrumentId = instrumentDb.Id;
            orderDb.Instrument = null;

            orderDb.PortfolioId = portfolioDb.Id;
            orderDb.Portfolio = null;

            context.Add(orderDb);
        }
        else
        {
            orderDb.SetUpdate(req);
            context.Update(orderDb);
        }
        context.SaveChanges();
        return Task.FromResult(new TResponseModel<int>() { Response = orderDb.IdPK });
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<List<InstrumentTradeStockSharpModel>>> GetInstrumentsAsync(int[] ids = null, CancellationToken cancellationToken = default)
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
    public async Task<TPaginationResponseModel<InstrumentTradeStockSharpViewModel>> InstrumentsSelectAsync(TPaginationRequestStandardModel<InstrumentsRequestModel> req, CancellationToken cancellationToken = default)
    {
        if (req.PageSize < 10)
            req.PageSize = 10;

        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<InstrumentStockSharpModelDB> q = context.Instruments.Where(x => req.Payload.FavoriteFilter == null || x.IsFavorite == req.Payload.FavoriteFilter).AsQueryable();

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
    public async Task<TResponseModel<List<PortfolioStockSharpModel>>> GetPortfoliosAsync(int[] ids = null, CancellationToken cancellationToken = default)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<PortfolioTradeModelDB> q = ids is null || ids.Length == 0
            ? context.Portfolios.AsQueryable()
            : context.Portfolios.Where(x => ids.Contains(x.Id));
        List<PortfolioTradeModelDB> data = await q.Include(x => x.Board).ToListAsync(cancellationToken: cancellationToken);

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