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
public class FlushStockSharpService(IDbContextFactory<StockSharpAppContext> toolsDbFactory) : IFlushStockSharpService
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

    /// <inheritdoc/>
    public async Task<TResponseModel<int>> SaveOrder(OrderStockSharpModel req)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync();
        OrderStockSharpModelDB orderDb = await context.Orders.FirstOrDefaultAsync(x => x.TransactionId == req.TransactionId);

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

            portfolioDb ??= await context.Portfolios.FirstAsync(x => x.Id == SavePortfolio(req.Portfolio).Result.Response);
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
        return new TResponseModel<int>() { Response = orderDb.IdPK };
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<int>> SaveTrade(MyTradeStockSharpModel myTrade)
    {
        TResponseModel<int> res = new();
        if (myTrade.Order is null)
        {
            res.AddError("myTrade.Order is null");
            return res;
        }
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync();
        MyTradeStockSharpModelDB myTradeDb = new MyTradeStockSharpModelDB().Bind(myTrade);
        myTradeDb.OrderId = SaveOrder(myTrade.Order).Result.Response;
        await context.MyTrades.AddAsync(myTradeDb);
        await context.SaveChangesAsync();
        res.Response = myTradeDb.Id;
        return res;
    }
}