////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;
using SharedLib;
using DbcLib;

namespace StockSharpDriver;

/// <summary>
/// FlushStockSharpService
/// </summary>
public class FlushStockSharpService(IDbContextFactory<StockSharpAppContext> toolsDbFactory, ILogger<FlushStockSharpService> loggerRepo) : IFlushStockSharpService
{
    /// <inheritdoc/>
    public Task<TResponseModel<InstrumentTradeStockSharpViewModel>> SaveInstrument(InstrumentTradeStockSharpModel req)
    {
        using StockSharpAppContext context = toolsDbFactory.CreateDbContext();
        BoardStockSharpModelDB board = (BoardStockSharpModelDB)SaveBoard(req.Board).Result.Response;

        InstrumentStockSharpModelDB instrumentDb = context.Instruments
            .FirstOrDefault(x => x.Name == req.Name && x.Code == req.Code && x.BoardId == board.Id);

        if (instrumentDb is null)
        {
            instrumentDb = new InstrumentStockSharpModelDB().Bind(req);
            instrumentDb.CreatedAtUTC = DateTime.UtcNow;
            instrumentDb.LastUpdatedAtUTC = DateTime.UtcNow;
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
        instrumentDb.Board = board;
        return Task.FromResult(new TResponseModel<InstrumentTradeStockSharpViewModel>() { Response = instrumentDb });
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<PortfolioStockSharpViewModel>> SavePortfolio(PortfolioStockSharpModel req)
    {
        using StockSharpAppContext context = toolsDbFactory.CreateDbContext();
        BoardStockSharpModelDB board = req.Board is null ? null : (BoardStockSharpModelDB)SaveBoard(req.Board).Result.Response;

        IQueryable<PortfolioTradeModelDB> q = context.Portfolios
            .Where(x => x.Name == req.Name && x.DepoName == req.DepoName && x.Currency == req.Currency);

        PortfolioTradeModelDB portDb = board == null
            ? q.FirstOrDefault(x => x.BoardId == null)
            : q.FirstOrDefault(x => x.BoardId == board.Id);

        if (portDb is null)
        {
            portDb = new PortfolioTradeModelDB().Bind(req);
            portDb.CreatedAtUTC = DateTime.UtcNow;
            portDb.LastUpdatedAtUTC = DateTime.UtcNow;
            portDb.BoardId = board?.Id;
            portDb.Board = null;

            context.Add(portDb);
        }
        else
        {
            portDb.SetUpdate(req);
            context.Update(portDb);
        }
        await context.SaveChangesAsync();
        portDb.Board = board;
        return new TResponseModel<PortfolioStockSharpViewModel>() { Response = portDb };
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<MyTradeStockSharpViewModel>> SaveTrade(MyTradeStockSharpModel myTrade, InstrumentTradeStockSharpViewModel instrument)
    {
        TResponseModel<MyTradeStockSharpViewModel> res = new();

        if (myTrade.Order is null)
        {
            loggerRepo.LogError("myTrade.Order is null");
            res.AddError("myTrade.Order is null");
            return res;
        }
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync();
        MyTradeStockSharpModelDB myTradeDb = new MyTradeStockSharpModelDB().Bind(myTrade);
        myTradeDb.LastUpdatedAtUTC = DateTime.UtcNow;

        myTradeDb.OrderId = SaveOrder(myTrade.Order, instrument).Result.Response.IdPK;
        await context.MyTrades.AddAsync(myTradeDb);
        await context.SaveChangesAsync();
        res.Response = myTradeDb;
        return res;
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<OrderStockSharpViewModel>> SaveOrder(OrderStockSharpModel req, InstrumentTradeStockSharpViewModel instrument)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync();
        OrderStockSharpModelDB orderDb = await context.Orders.FirstOrDefaultAsync(x => x.TransactionId == req.TransactionId);

        PortfolioTradeModelDB portfolioDb = null;
        if (!string.IsNullOrWhiteSpace(req.Portfolio.Name))
        {
            portfolioDb = context.Portfolios
                .FirstOrDefault(x =>
                x.Name == req.Portfolio.Name &&
                x.DepoName == req.Portfolio.DepoName &&
                x.ClientCode == req.Portfolio.ClientCode &&
                x.Currency == req.Portfolio.Currency);

            portfolioDb ??= (PortfolioTradeModelDB)SavePortfolio(req.Portfolio).Result.Response;
        }
        else
            portfolioDb = (PortfolioTradeModelDB)SavePortfolio(req.Portfolio).Result.Response;

        if (orderDb is null)
        {
            orderDb = OrderStockSharpModelDB.Build(req, instrument.Id);
            orderDb.CreatedAtUTC = DateTime.UtcNow;
            orderDb.LastUpdatedAtUTC = DateTime.UtcNow;
            orderDb.InstrumentId = instrument.Id;
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

        orderDb.Instrument = (InstrumentStockSharpModelDB)instrument;
        orderDb.Portfolio = portfolioDb;

        return new TResponseModel<OrderStockSharpViewModel>() { Response = orderDb };
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<BoardStockSharpViewModel>> SaveBoard(BoardStockSharpModel req)
    {
        using StockSharpAppContext context = toolsDbFactory.CreateDbContext();
        ExchangeStockSharpModelDB exchange = (ExchangeStockSharpModelDB)(await SaveExchange(req.Exchange)).Response;

        BoardStockSharpModelDB boardDb = context.Boards
            .FirstOrDefault(x => x.Code == req.Code && x.ExchangeId == exchange.Id);
        if (boardDb is null)
        {
            boardDb = new BoardStockSharpModelDB().Bind(req);
            boardDb.LastUpdatedAtUTC = DateTime.UtcNow;
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

        boardDb.Exchange = exchange;

        return new TResponseModel<BoardStockSharpViewModel>() { Response = boardDb };
    }

    /// <inheritdoc/>
    public Task<TResponseModel<ExchangeStockSharpViewModel>> SaveExchange(ExchangeStockSharpModel req)
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
        return Task.FromResult(new TResponseModel<ExchangeStockSharpViewModel>() { Response = exchangeDb });
    }
}