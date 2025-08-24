////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedLib;
using DbcLib;

namespace StockSharpDriver;

/// <summary>
/// FlushStockSharpService
/// </summary>
public class FlushStockSharpService(IDbContextFactory<StockSharpAppContext> toolsDbFactory, ILogger<FlushStockSharpService> logger) : IFlushStockSharpService
{
    /// <inheritdoc/>
    public Task<TResponseModel<InstrumentTradeStockSharpViewModel>> SaveInstrument(InstrumentTradeStockSharpModel req)
    {
        using StockSharpAppContext context = toolsDbFactory.CreateDbContext();
        BoardStockSharpModelDB board = (BoardStockSharpModelDB)SaveBoard(req.Board).Result.Response;

        InstrumentStockSharpModelDB instrumentDb = context.Instruments
            .FirstOrDefault(x => x.Code == req.Code && x.BoardId == board.Id);

        if (instrumentDb is null)
        {
            instrumentDb = new InstrumentStockSharpModelDB().Bind(req);
            instrumentDb.CreatedAtUTC = DateTime.UtcNow;
            instrumentDb.LastUpdatedAtUTC = instrumentDb.CreatedAtUTC;
            instrumentDb.BoardId = board.Id;
            instrumentDb.Board = null;

            context.Add(instrumentDb);
            logger.LogInformation($"New instrument (save): {JsonConvert.SerializeObject(instrumentDb)}");
        }
        else
        {
            instrumentDb.SetUpdate(req);
            context.Update(instrumentDb);
            logger.LogDebug($"Actuality/update instrument: {instrumentDb.IdRemote}");
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
            logger.LogInformation($"New portfolio (save): {JsonConvert.SerializeObject(portDb)}");
        }
        else
        {
            portDb.SetUpdate(req);
            context.Update(portDb);
            logger.LogDebug($"Actuality/update portfolio: {portDb}");
        }
        await context.SaveChangesAsync();
        portDb.Board = board;
        return new TResponseModel<PortfolioStockSharpViewModel>() { Response = portDb };
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<OrderStockSharpViewModel>> SaveOrder(OrderStockSharpModel req, InstrumentTradeStockSharpViewModel instrument)
    {
        using StockSharpAppContext context = await toolsDbFactory.CreateDbContextAsync();
        OrderStockSharpModelDB orderDb = await context.Orders.FirstOrDefaultAsync(x => x.TransactionId == req.TransactionId);

        PortfolioTradeModelDB? portfolioDb = null;
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
            logger.LogInformation($"New order (save): {JsonConvert.SerializeObject(orderDb)}");
        }
        else
        {
            orderDb.SetUpdate(req);
            context.Update(orderDb);
            logger.LogInformation($"Update order: {JsonConvert.SerializeObject(orderDb)}");
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

        BoardStockSharpModelDB? boardDb = context.Boards
            .FirstOrDefault(x => x.Code == req.Code && x.ExchangeId == exchange.Id);
        if (boardDb is null)
        {
            boardDb = new BoardStockSharpModelDB().Bind(req);
            boardDb.CreatedAtUTC = DateTime.UtcNow;
            boardDb.LastUpdatedAtUTC = boardDb.CreatedAtUTC;
            boardDb.ExchangeId = exchange.Id;
            boardDb.Exchange = null;

            context.Add(boardDb);
            logger.LogInformation($"New board (save): {JsonConvert.SerializeObject(boardDb)}");
        }
        else
        {
            boardDb.SetUpdate(req);
            context.Update(boardDb);
            logger.LogDebug($"Actuality/update board: {boardDb}");
        }
        context.SaveChanges();

        boardDb.Exchange = exchange;

        return new TResponseModel<BoardStockSharpViewModel>() { Response = boardDb };
    }

    /// <inheritdoc/>
    public Task<TResponseModel<ExchangeStockSharpViewModel>> SaveExchange(ExchangeStockSharpModel req)
    {
        using StockSharpAppContext context = toolsDbFactory.CreateDbContext();
        ExchangeStockSharpModelDB? exchangeDb = context.Exchanges
            .FirstOrDefault(x => x.Name == req.Name && x.CountryCode == req.CountryCode);
        if (exchangeDb is null)
        {
            exchangeDb = new ExchangeStockSharpModelDB().Bind(req);
            context.Add(exchangeDb);
            logger.LogInformation($"New exchange (save): {JsonConvert.SerializeObject(exchangeDb)}");
        }
        else
        {
            exchangeDb.SetUpdate(req);
            context.Update(exchangeDb);
            logger.LogDebug($"Actuality/update exchange: {exchangeDb}");
        }
        context.SaveChanges();
        return Task.FromResult(new TResponseModel<ExchangeStockSharpViewModel>() { Response = exchangeDb });
    }
}