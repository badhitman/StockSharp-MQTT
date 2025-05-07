////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using StockSharp.Algo;
using SharedLib;
using Newtonsoft.Json;
using StockSharp.BusinessEntities;
using System.Diagnostics.Tracing;
using StockSharp.Fix.Quik.Lua;
using Ecng.Common;
using System.Net;

namespace StockSharpDriver;

/// <summary>
/// StockSharpDriverService 
/// </summary>
public class DriverStockSharpService(IStockSharpDataService dataRepo, IStockSharpEventsService eventTrans, ILogger<DriverStockSharpService> _logger, Connector connector) : IStockSharpDriverService
{
    /// <inheritdoc/>
    public async Task<ResponseBaseModel> Connect(CancellationToken? cancellationToken = default)
    {
        if (!connector.CanConnect)
            return ResponseBaseModel.CreateError("can`t connect");

        connector.Connected += ConnectedHandle;
        connector.ConnectedEx += ConnectedExHandle;
        connector.Disconnected += DisconnectedHandle;
        connector.BoardReceived += BoardReceivedHandle;
        connector.CandleReceived += CandleReceivedHandle;
        connector.ConnectionLost += ConnectionLostHandle;
        connector.ConnectionError += ConnectionErrorHandle;
        connector.DataTypeReceived += DataTypeReceivedHandle;
        connector.ConnectionErrorEx += ConnectionErrorExHandle;
        connector.ConnectionRestored += ConnectionRestoredHandle;
        connector.CurrentTimeChanged += CurrentTimeChangedHandle;
        connector.ChangePasswordResult += ChangePasswordResultHandle;
        connector.DisconnectedEx += DisconnectedExHandle;
        connector.Disposed += DisposedHandle;
        connector.Error += ErrorHandle;
        connector.Level1Received += Level1ReceivedHandle;
        connector.Log += LogHandle;
        connector.LookupPortfoliosResult += LookupPortfoliosResultHandle;
        connector.LookupSecuritiesResult += LookupSecuritiesResultHandle;
        connector.MassOrderCanceled += MassOrderCanceledHandle;
        connector.MassOrderCanceled2 += MassOrderCanceled2Handle;
        connector.MassOrderCancelFailed += MassOrderCancelFailedHandle;
        connector.MassOrderCancelFailed2 += MassOrderCancelFailed2Handle;
        connector.NewMessage += NewMessageHandle;
        connector.NewsReceived += NewsReceivedHandle;
        connector.OrderBookReceived += OrderBookReceivedHandle;
        connector.OrderCancelFailReceived += OrderCancelFailReceivedHandle;
        connector.OrderEditFailReceived += OrderEditFailReceivedHandle;
        connector.OrderLogReceived += OrderLogReceivedHandle;
        connector.OrderReceived += OrderReceivedHandle;
        connector.OrderRegisterFailReceived += OrderRegisterFailReceivedHandle;
        connector.OwnTradeReceived += OwnTradeReceivedHandle;
        connector.ParentRemoved += ParentRemovedHandle;
        connector.PortfolioReceived += PortfolioReceivedHandle;
        connector.PositionReceived += PositionReceivedHandle;
        connector.SecurityReceived += SecurityReceivedHandle;
        connector.SubscriptionFailed += SubscriptionFailedHandle;
        connector.SubscriptionOnline += SubscriptionOnlineHandle;
        connector.SubscriptionReceived += SubscriptionReceivedHandle;
        connector.SubscriptionStarted += SubscriptionStartedHandle;
        connector.SubscriptionStopped += SubscriptionStoppedHandle;
        connector.TickTradeReceived += TickTradeReceivedHandle;
        connector.ValuesChanged += ValuesChangedHandle;

        LuaFixMarketDataMessageAdapter luaFixMarketDataMessageAdapter = new(connector.TransactionIdGenerator)
        {
            Address = "localhost:5001".To<EndPoint>(),
            //Login = "quik",
            //Password = "quik".To<SecureString>(),
            IsDemo = true,
        };
        LuaFixTransactionMessageAdapter luaFixTransactionMessageAdapter = new(connector.TransactionIdGenerator)
        {
            Address = "localhost:5001".To<EndPoint>(),
            //Login = "quik",
            //Password = "quik".To<SecureString>(),
            IsDemo = true, 
        };
        connector.Adapter.InnerAdapters.Add(luaFixMarketDataMessageAdapter);
        connector.Adapter.InnerAdapters.Add(luaFixTransactionMessageAdapter);

        await connector.ConnectAsync(cancellationToken ?? CancellationToken.None);

        return ResponseBaseModel.CreateInfo("connection started");
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> Disconnect(CancellationToken? cancellationToken = default)
    {
        connector.CancelOrders();

        foreach (Subscription sub in connector.Subscriptions)
        {
            connector.UnSubscribe(sub);
            _logger.LogInformation($"{nameof(Connector.UnSubscribe)} > {sub.GetType().FullName}");
        }

        await connector.DisconnectAsync(cancellationToken ?? CancellationToken.None);

        connector.Connected -= ConnectedHandle;
        connector.ConnectedEx -= ConnectedExHandle;
        connector.Disconnected -= DisconnectedHandle;
        connector.BoardReceived -= BoardReceivedHandle;
        connector.CandleReceived -= CandleReceivedHandle;
        connector.ConnectionLost -= ConnectionLostHandle;
        connector.ConnectionError -= ConnectionErrorHandle;
        connector.DataTypeReceived -= DataTypeReceivedHandle;
        connector.ConnectionErrorEx -= ConnectionErrorExHandle;
        connector.ConnectionRestored -= ConnectionRestoredHandle;
        connector.CurrentTimeChanged -= CurrentTimeChangedHandle;
        connector.ChangePasswordResult -= ChangePasswordResultHandle;
        connector.DisconnectedEx -= DisconnectedExHandle;
        connector.Disposed -= DisposedHandle;
        connector.Error -= ErrorHandle;
        connector.Level1Received -= Level1ReceivedHandle;
        connector.Log -= LogHandle;
        connector.LookupPortfoliosResult -= LookupPortfoliosResultHandle;
        connector.LookupSecuritiesResult -= LookupSecuritiesResultHandle;
        connector.MassOrderCanceled -= MassOrderCanceledHandle;
        connector.MassOrderCanceled2 -= MassOrderCanceled2Handle;
        connector.MassOrderCancelFailed -= MassOrderCancelFailedHandle;
        connector.MassOrderCancelFailed2 -= MassOrderCancelFailed2Handle;
        connector.NewMessage -= NewMessageHandle;
        connector.NewsReceived -= NewsReceivedHandle;
        connector.OrderBookReceived -= OrderBookReceivedHandle;
        connector.OrderCancelFailReceived -= OrderCancelFailReceivedHandle;
        connector.OrderEditFailReceived -= OrderEditFailReceivedHandle;
        connector.OrderLogReceived -= OrderLogReceivedHandle;
        connector.OrderReceived -= OrderReceivedHandle;
        connector.OrderRegisterFailReceived -= OrderRegisterFailReceivedHandle;
        connector.OwnTradeReceived -= OwnTradeReceivedHandle;
        connector.ParentRemoved -= ParentRemovedHandle;
        connector.PortfolioReceived -= PortfolioReceivedHandle;
        connector.PositionReceived -= PositionReceivedHandle;
        connector.SecurityReceived -= SecurityReceivedHandle;
        connector.SubscriptionFailed -= SubscriptionFailedHandle;
        connector.SubscriptionOnline -= SubscriptionOnlineHandle;
        connector.SubscriptionReceived -= SubscriptionReceivedHandle;
        connector.SubscriptionStarted -= SubscriptionStartedHandle;
        connector.SubscriptionStopped -= SubscriptionStoppedHandle;
        connector.TickTradeReceived -= TickTradeReceivedHandle;
        connector.ValuesChanged -= ValuesChangedHandle;

        return ResponseBaseModel.CreateInfo("connection closed");
    }

    void ValuesChangedHandle(Security instrument, IEnumerable<KeyValuePair<StockSharp.Messages.Level1Fields, object>> dataPayload, DateTimeOffset dtOffsetMaster, DateTimeOffset dtOffsetSlave)
    {
        _logger.LogInformation($"Call > `{nameof(ValuesChangedHandle)}` [{dtOffsetMaster}]/[{dtOffsetSlave}]: {JsonConvert.SerializeObject(instrument)}\n\n{JsonConvert.SerializeObject(dataPayload)}");
        ConnectorValuesChangedEventPayloadModel req = new()
        {
            OffsetSlave = dtOffsetSlave,
            OffsetMaster = dtOffsetMaster,
            DataPayload = [.. dataPayload.Select(x => new KeyValuePair<Level1FieldsStockSharpEnum, object>((Level1FieldsStockSharpEnum)Enum.Parse(typeof(Level1FieldsStockSharpEnum), Enum.GetName(x.Key)!), x.Value))],
            Instrument = new InstrumentTradeStockSharpModel().Bind(instrument),
        };
        //dataRepo.SaveInstrument(req.Instrument);
        eventTrans.ValuesChangedEvent(req);
    }

    void SecurityReceivedHandle(Subscription subscription, Security sec)
    {
        _logger.LogTrace($"Call > `{nameof(SecurityReceivedHandle)}`: {JsonConvert.SerializeObject(sec)}");
        InstrumentTradeStockSharpModel req = new InstrumentTradeStockSharpModel().Bind(sec);
        dataRepo.SaveInstrument(req);
        eventTrans.InstrumentReceived(req);
    }

    void PortfolioReceivedHandle(Subscription subscription, Portfolio port)
    {
        _logger.LogInformation($"Call > `{nameof(PortfolioReceivedHandle)}`: {JsonConvert.SerializeObject(port)}");
        PortfolioStockSharpModel req = new PortfolioStockSharpModel().Bind(port);
        dataRepo.SavePortfolio(req);
        eventTrans.PortfolioReceived(req);
    }

    void BoardReceivedHandle(Subscription subscription, ExchangeBoard boardExchange)
    {
        _logger.LogWarning($"Call > `{nameof(BoardReceivedHandle)}`: {JsonConvert.SerializeObject(boardExchange)}");
        BoardStockSharpModel req = new BoardStockSharpModel().Bind(boardExchange);
        dataRepo.SaveBoard(req);
        eventTrans.BoardReceived(req);
    }

    void OrderReceivedHandle(Subscription subscription, Order oreder)
    {
        _logger.LogWarning($"Call > `{nameof(OrderReceivedHandle)}`: {JsonConvert.SerializeObject(oreder)}");
        OrderStockSharpModel req = new OrderStockSharpModel().Bind(oreder);
        dataRepo.SaveOrder(req);
        eventTrans.OrderReceived(req);
    }


    void PositionReceivedHandle(Subscription subscription, Position pos)
    {
        _logger.LogWarning($"Call > `{nameof(PositionReceivedHandle)}`: {JsonConvert.SerializeObject(pos)}");
    }
    void OwnTradeReceivedHandle(Subscription subscription, MyTrade tr)
    {
        _logger.LogWarning($"Call > `{nameof(OwnTradeReceivedHandle)}`: {JsonConvert.SerializeObject(tr)}");
    }

    #region Exception`s
    void LookupSecuritiesResultHandle(StockSharp.Messages.SecurityLookupMessage slm, IEnumerable<Security> securities, Exception ex)
    {
        _logger.LogError(ex, $"Call > `{nameof(LookupSecuritiesResultHandle)}`: {JsonConvert.SerializeObject(slm)}");

        foreach (Security sec in securities)
            dataRepo.SaveInstrument(new InstrumentTradeStockSharpModel().Bind(sec));
    }

    void LookupPortfoliosResultHandle(StockSharp.Messages.PortfolioLookupMessage portfolioLM, IEnumerable<Portfolio> portfolios, Exception ex)
    {
        _logger.LogError(ex, $"Call > `{nameof(LookupPortfoliosResultHandle)}`: {JsonConvert.SerializeObject(portfolioLM)}");

        //foreach (Portfolio port in portfolios)
        //    dataRepo.SavePortfolio(new PortfolioStockSharpModel().Bind(port));
    }

    void SubscriptionFailedHandle(Subscription subscription, Exception ex, bool arg)
    {
        _logger.LogError(ex, $"Call > `{nameof(SubscriptionFailedHandle)}`: [{nameof(arg)}:{arg}]");
    }
    void SubscriptionStoppedHandle(Subscription subscription, Exception ex)
    {
        _logger.LogError(ex, $"Call > `{nameof(SubscriptionStoppedHandle)}`");
    }
    void MassOrderCancelFailed2Handle(long arg, Exception ex, DateTimeOffset dt)
    {
        _logger.LogError(ex, $"Call > `{nameof(MassOrderCancelFailed2Handle)}` [{nameof(arg)}:{arg}]: {dt}");
    }
    void MassOrderCancelFailedHandle(long arg, Exception ex)
    {
        _logger.LogError(ex, $"Call > `{nameof(MassOrderCancelFailedHandle)}` [{nameof(arg)}:{arg}]");
    }
    void ConnectionErrorExHandle(StockSharp.Messages.IMessageAdapter sender, Exception ex)
    {
        _logger.LogError(ex, $"Call > `{nameof(ConnectionErrorExHandle)}`");
    }
    void ConnectionErrorHandle(Exception ex)
    {
        _logger.LogError(ex, $"Call > `{nameof(ConnectionErrorHandle)}`");
    }
    void ErrorHandle(Exception ex)
    {
        _logger.LogError(ex, $"Call > `{nameof(ErrorHandle)}`");
    }
    void ChangePasswordResultHandle(long arg, Exception ex)
    {
        _logger.LogError(ex, $"Call > `{nameof(ChangePasswordResultHandle)}`: {arg}");
    }
    #endregion

    #region todo
    void TickTradeReceivedHandle(Subscription subscription, StockSharp.Messages.ITickTradeMessage msg)
    {
        _logger.LogWarning($"Call > `{nameof(TickTradeReceivedHandle)}`: {JsonConvert.SerializeObject(msg)}");
    }
    void SubscriptionStartedHandle(Subscription subscription)
    {
        _logger.LogWarning($"Call > `{nameof(SubscriptionStartedHandle)}`");
    }
    void SubscriptionOnlineHandle(Subscription subscription)
    {
        _logger.LogWarning($"Call > `{nameof(SubscriptionOnlineHandle)}`");
    }
    void ParentRemovedHandle(Ecng.Logging.ILogSource sender)
    {
        _logger.LogWarning($"Call > `{nameof(ParentRemovedHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    void OrderRegisterFailReceivedHandle(Subscription subscription, OrderFail orderF)
    {
        _logger.LogWarning($"Call > `{nameof(OrderRegisterFailReceivedHandle)}`: {JsonConvert.SerializeObject(orderF)}");
    }
    void OrderLogReceivedHandle(Subscription subscription, StockSharp.Messages.IOrderLogMessage order)
    {
        _logger.LogWarning($"Call > `{nameof(OrderLogReceivedHandle)}`: {JsonConvert.SerializeObject(order)}");
    }
    void OrderEditFailReceivedHandle(Subscription subscription, OrderFail orderF)
    {
        _logger.LogWarning($"Call > `{nameof(OrderEditFailReceivedHandle)}`: {JsonConvert.SerializeObject(orderF)}");
    }
    void OrderCancelFailReceivedHandle(Subscription subscription, OrderFail orderF)
    {
        _logger.LogWarning($"Call > `{nameof(OrderCancelFailReceivedHandle)}`: {JsonConvert.SerializeObject(orderF)}");
    }
    void OrderBookReceivedHandle(Subscription subscription, StockSharp.Messages.IOrderBookMessage orderBM)
    {
        _logger.LogWarning($"Call > `{nameof(OrderBookReceivedHandle)}`: {JsonConvert.SerializeObject(orderBM)}");
    }
    void NewsReceivedHandle(Subscription subscription, News sender)
    {
        _logger.LogWarning($"Call > `{nameof(NewsReceivedHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    void MassOrderCanceled2Handle(long arg, DateTimeOffset dt)
    {
        _logger.LogWarning($"Call > `{nameof(MassOrderCanceled2Handle)}` [{nameof(arg)}:{arg}]: {dt}");
    }
    void MassOrderCanceledHandle(long sender)
    {
        _logger.LogWarning($"Call > `{nameof(MassOrderCanceledHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    void Level1ReceivedHandle(Subscription subscription, StockSharp.Messages.Level1ChangeMessage levelCh)
    {
        _logger.LogWarning($"Call > `{nameof(Level1ReceivedHandle)}`: {JsonConvert.SerializeObject(levelCh)}");
    }
    void DisposedHandle()
    {
        _logger.LogWarning($"Call > `{nameof(DisposedHandle)}`");
    }
    void DisconnectedExHandle(StockSharp.Messages.IMessageAdapter sender)
    {
        _logger.LogWarning($"Call > `{nameof(DisconnectedExHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    void DisconnectedHandle()
    {
        _logger.LogWarning($"Call > `{nameof(DisconnectedHandle)}`");
    }
    void DataTypeReceivedHandle(Subscription subscription, StockSharp.Messages.DataType argDt)
    {
        _logger.LogWarning($"Call > `{nameof(DataTypeReceivedHandle)}`: {JsonConvert.SerializeObject(argDt)}");
    }
    void ConnectionRestoredHandle(StockSharp.Messages.IMessageAdapter sender)
    {
        _logger.LogWarning($"Call > `{nameof(ConnectionRestoredHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    void ConnectionLostHandle(StockSharp.Messages.IMessageAdapter sender)
    {
        _logger.LogWarning($"Call > `{nameof(ConnectionLostHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    void ConnectedExHandle(StockSharp.Messages.IMessageAdapter sender)
    {
        _logger.LogWarning($"Call > `{nameof(ConnectedExHandle)}`: {JsonConvert.SerializeObject(new { sender.Name, sender.Categories, ((StockSharp.Fix.FixMessageAdapter)sender).Address })}");
    }
    void ConnectedHandle()
    {
        _logger.LogWarning($"Call > `{nameof(ConnectedHandle)}`");
    }
    void CandleReceivedHandle(Subscription subscription, StockSharp.Messages.ICandleMessage candleMessage)
    {
        _logger.LogWarning($"Call > `{nameof(CandleReceivedHandle)}`: {JsonConvert.SerializeObject(candleMessage)}");
    }
    void LogHandle(Ecng.Logging.LogMessage senderLog)
    {
        _logger.LogTrace($"Call > `{nameof(LogHandle)}`: {senderLog}");
    }
    void CurrentTimeChangedHandle(TimeSpan sender)
    {
        _logger.LogTrace($"Call > `{nameof(CurrentTimeChangedHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    void NewMessageHandle(StockSharp.Messages.Message msg)
    {
        _logger.LogTrace($"Call > `{nameof(NewMessageHandle)}`: {JsonConvert.SerializeObject(msg)}");
    }
    void SubscriptionReceivedHandle(Subscription subscription, object sender)
    {
        _logger.LogTrace($"Call > `{nameof(SubscriptionReceivedHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    #endregion

    /// <inheritdoc/>
    public Task<ResponseBaseModel> PingAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ResponseBaseModel.CreateSuccess($"Ok - {nameof(DriverStockSharpService)}"));
    }
}