////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using StockSharp.BusinessEntities;
using StockSharp.Algo;
using Newtonsoft.Json;
using SharedLib;

namespace StockSharpDriver;

/// <inheritdoc/>
public class ConnectionStockSharpWorker(
    IFlushStockSharpService dataRepo,
    ILogger<ConnectionStockSharpWorker> _logger,
    IEventsStockSharpService eventTrans,
    ConnectionLink conLink) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RegisterEvents();

        conLink.ConnectNotify += ConnectLink;
        conLink.DisconnectNotify += DisconnectLink;

        while (!stoppingToken.IsCancellationRequested)
        {
            // _logger.LogDebug($"`tic-tac`");
            await Task.Delay(200, stoppingToken);
        }

        conLink.ConnectNotify -= ConnectLink;
        conLink.DisconnectNotify -= DisconnectLink;

        _logger.LogInformation($"call - {nameof(Connector.CancelOrders)}!");
        conLink.Connector.CancelOrders();

        foreach (Subscription sub in conLink.Connector.Subscriptions)
        {
            conLink.Connector.UnSubscribe(sub);
            _logger.LogInformation($"{nameof(Connector.UnSubscribe)} > {sub.GetType().FullName}");
        }

        await conLink.Connector.DisconnectAsync(stoppingToken);
    }

    private void DisconnectLink()
    {
        UnregisterEvents();
    }

    private void ConnectLink()
    {
        RegisterEvents();
    }

    /// <inheritdoc/>
    public bool IsConnected => conLink.Connector.ConnectionState == Ecng.ComponentModel.ConnectionStates.Connected;

    void ValuesChangedHandle(Security instrument, IEnumerable<KeyValuePair<StockSharp.Messages.Level1Fields, object>> dataPayload, DateTimeOffset dtOffsetMaster, DateTimeOffset dtOffsetSlave)
    {
        _logger.LogInformation($"Call > `{nameof(ValuesChangedHandle)}` [{dtOffsetMaster}]/[{dtOffsetSlave}]: {JsonConvert.SerializeObject(instrument)}\n\n{JsonConvert.SerializeObject(dataPayload)}");
        ConnectorValuesChangedEventPayloadModel valueChangeEvent = new()
        {
            OffsetSlave = dtOffsetSlave,
            OffsetMaster = dtOffsetMaster,
            DataPayload = [.. dataPayload.Select(x => new KeyValuePair<Level1FieldsStockSharpEnum, object>((Level1FieldsStockSharpEnum)Enum.Parse(typeof(Level1FieldsStockSharpEnum), Enum.GetName(x.Key)!), x.Value))],
            Instrument = new InstrumentTradeStockSharpModel().Bind(instrument),
        };
        //
        eventTrans.ValuesChangedEvent(valueChangeEvent);
    }

    void SecurityReceivedHandle(Subscription subscription, Security sec)
    {
        _logger.LogTrace($"Call > `{nameof(SecurityReceivedHandle)}`: {JsonConvert.SerializeObject(sec)}");
        InstrumentTradeStockSharpModel instrument = new InstrumentTradeStockSharpModel().Bind(sec);
        InstrumentTradeStockSharpViewModel dbRes = dataRepo.SaveInstrument(instrument).Result.Response;
        eventTrans.InstrumentReceived(dbRes);
    }

    void PortfolioReceivedHandle(Subscription subscription, Portfolio port)
    {
        _logger.LogInformation($"Call > `{nameof(PortfolioReceivedHandle)}`: {JsonConvert.SerializeObject(port)}");
        PortfolioStockSharpModel portfolio = new PortfolioStockSharpModel().Bind(port);
        TResponseModel<PortfolioStockSharpViewModel> echoData = dataRepo.SavePortfolio(portfolio).Result;
        eventTrans.PortfolioReceived(echoData.Response);
    }

    void BoardReceivedHandle(Subscription subscription, ExchangeBoard boardExchange)
    {
        _logger.LogWarning($"Call > `{nameof(BoardReceivedHandle)}`: {JsonConvert.SerializeObject(boardExchange)}");
        BoardStockSharpModel board = new BoardStockSharpModel().Bind(boardExchange);
        dataRepo.SaveBoard(board);
        eventTrans.BoardReceived(board);
    }

    void OrderReceivedHandle(Subscription subscription, Order orderSource)
    {
        _logger.LogWarning($"Call > `{nameof(OrderReceivedHandle)}`: {JsonConvert.SerializeObject(orderSource)}");
        OrderStockSharpModel order = new OrderStockSharpModel().Bind(orderSource);
        TResponseModel<OrderStockSharpViewModel> dbRes = dataRepo.SaveOrder(order).Result;
        if (dbRes.Response is null)
            _logger.LogError("result is null: TResponseModel<OrderStockSharpViewModel> dbRes = dataRepo.SaveOrder(order).Result;");
        else
            eventTrans.OrderReceived(dbRes.Response);
    }

    void OwnTradeReceivedHandle(Subscription subscription, MyTrade tr)
    {
        _logger.LogWarning($"Call > `{nameof(OwnTradeReceivedHandle)}`: {JsonConvert.SerializeObject(tr)}");
        MyTradeStockSharpModel myTrade = new MyTradeStockSharpModel().Bind(tr);
        TResponseModel<MyTradeStockSharpViewModel> dbRes = dataRepo.SaveTrade(myTrade).Result;

        if (dbRes.Response is null)
            _logger.LogError("result is null: TResponseModel<MyTradeStockSharpViewModel> dbRes = dataRepo.SaveTrade(myTrade).Result;");
        else
            eventTrans.OwnTradeReceived(dbRes.Response);
    }

    void PositionReceivedHandle(Subscription subscription, Position pos)
    {
        _logger.LogWarning($"Call > `{nameof(PositionReceivedHandle)}`: {JsonConvert.SerializeObject(pos)}");
        PositionStockSharpModel position = new PositionStockSharpModel().Bind(pos);
        eventTrans.PositionReceived(position);
    }

    #region Exception`s
    void LookupSecuritiesResultHandle(StockSharp.Messages.SecurityLookupMessage slm, IEnumerable<Security> securities, Exception ex)
    {
        _logger.LogError(ex, $"Call > `{nameof(LookupSecuritiesResultHandle)}`: {JsonConvert.SerializeObject(slm)}");

        //foreach (Security sec in securities)
        //    dataRepo.SaveInstrument(new InstrumentTradeStockSharpModel().Bind(sec));
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
        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState)),
        }).Wait();
    }
    void ConnectionErrorHandle(Exception ex)
    {
        _logger.LogError(ex, $"Call > `{nameof(ConnectionErrorHandle)}`");
        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState))
        }).Wait();
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
        _logger.LogWarning($"Call > `{nameof(DisconnectedExHandle)}`");
        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState))
        }).Wait();
    }
    void DisconnectedHandle()
    {
        _logger.LogWarning($"Call > `{nameof(DisconnectedHandle)}`");
        UnregisterEvents();

        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState))
        }).Wait();
    }
    void DataTypeReceivedHandle(Subscription subscription, StockSharp.Messages.DataType argDt)
    {
        _logger.LogWarning($"Call > `{nameof(DataTypeReceivedHandle)}`: {JsonConvert.SerializeObject(argDt)}");
    }
    void ConnectionRestoredHandle(StockSharp.Messages.IMessageAdapter sender)
    {
        _logger.LogWarning($"Call > `{nameof(ConnectionRestoredHandle)}`: {JsonConvert.SerializeObject(sender)}");
        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState))
        }).Wait();
    }
    void ConnectionLostHandle(StockSharp.Messages.IMessageAdapter sender)
    {
        _logger.LogWarning($"Call > `{nameof(ConnectionLostHandle)}`: {JsonConvert.SerializeObject(sender)}");
        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState))
        }).Wait();
    }
    void ConnectedExHandle(StockSharp.Messages.IMessageAdapter sender)
    {
        _logger.LogWarning($"Call > `{nameof(ConnectedExHandle)}`: {JsonConvert.SerializeObject(new { sender.Name, sender.Categories })}");
        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState))
        }).Wait();
    }
    void ConnectedHandle()
    {
        _logger.LogWarning($"Call > `{nameof(ConnectedHandle)}`");
        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState))
        }).Wait();
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

    void UnregisterEvents()
    {
        conLink.Connector.Connected -= ConnectedHandle;
        conLink.Connector.ConnectedEx -= ConnectedExHandle;
        conLink.Connector.Disconnected -= DisconnectedHandle;
        conLink.Connector.BoardReceived -= BoardReceivedHandle;
        conLink.Connector.CandleReceived -= CandleReceivedHandle;
        conLink.Connector.ConnectionLost -= ConnectionLostHandle;
        conLink.Connector.ConnectionError -= ConnectionErrorHandle;
        conLink.Connector.DataTypeReceived -= DataTypeReceivedHandle;
        conLink.Connector.ConnectionErrorEx -= ConnectionErrorExHandle;
        conLink.Connector.ConnectionRestored -= ConnectionRestoredHandle;
        conLink.Connector.CurrentTimeChanged -= CurrentTimeChangedHandle;
        conLink.Connector.ChangePasswordResult -= ChangePasswordResultHandle;
        conLink.Connector.DisconnectedEx -= DisconnectedExHandle;
        conLink.Connector.Disposed -= DisposedHandle;
        conLink.Connector.Error -= ErrorHandle;
        conLink.Connector.Level1Received -= Level1ReceivedHandle;
        conLink.Connector.Log -= LogHandle;
        conLink.Connector.LookupPortfoliosResult -= LookupPortfoliosResultHandle;
        conLink.Connector.LookupSecuritiesResult -= LookupSecuritiesResultHandle;
        conLink.Connector.MassOrderCanceled -= MassOrderCanceledHandle;
        conLink.Connector.MassOrderCanceled2 -= MassOrderCanceled2Handle;
        conLink.Connector.MassOrderCancelFailed -= MassOrderCancelFailedHandle;
        conLink.Connector.MassOrderCancelFailed2 -= MassOrderCancelFailed2Handle;
        conLink.Connector.NewMessage -= NewMessageHandle;
        conLink.Connector.NewsReceived -= NewsReceivedHandle;
        conLink.Connector.OrderBookReceived -= OrderBookReceivedHandle;
        conLink.Connector.OrderCancelFailReceived -= OrderCancelFailReceivedHandle;
        conLink.Connector.OrderEditFailReceived -= OrderEditFailReceivedHandle;
        conLink.Connector.OrderLogReceived -= OrderLogReceivedHandle;
        conLink.Connector.OrderReceived -= OrderReceivedHandle;
        conLink.Connector.OrderRegisterFailReceived -= OrderRegisterFailReceivedHandle;
        conLink.Connector.OwnTradeReceived -= OwnTradeReceivedHandle;
        conLink.Connector.ParentRemoved -= ParentRemovedHandle;
        conLink.Connector.PortfolioReceived -= PortfolioReceivedHandle;
        conLink.Connector.PositionReceived -= PositionReceivedHandle;
        conLink.Connector.SecurityReceived -= SecurityReceivedHandle;
        conLink.Connector.SubscriptionFailed -= SubscriptionFailedHandle;
        conLink.Connector.SubscriptionOnline -= SubscriptionOnlineHandle;
        conLink.Connector.SubscriptionReceived -= SubscriptionReceivedHandle;
        conLink.Connector.SubscriptionStarted -= SubscriptionStartedHandle;
        conLink.Connector.SubscriptionStopped -= SubscriptionStoppedHandle;
        conLink.Connector.TickTradeReceived -= TickTradeReceivedHandle;
        conLink.Connector.ValuesChanged -= ValuesChangedHandle;
    }

    void RegisterEvents()
    {
        conLink.Connector.Connected += ConnectedHandle;
        conLink.Connector.ConnectedEx += ConnectedExHandle;
        conLink.Connector.Disconnected += DisconnectedHandle;
        conLink.Connector.DisconnectedEx += DisconnectedExHandle;
        conLink.Connector.BoardReceived += BoardReceivedHandle;
        conLink.Connector.CandleReceived += CandleReceivedHandle;
        conLink.Connector.ConnectionLost += ConnectionLostHandle;
        conLink.Connector.ConnectionError += ConnectionErrorHandle;
        conLink.Connector.DataTypeReceived += DataTypeReceivedHandle;
        conLink.Connector.ConnectionErrorEx += ConnectionErrorExHandle;
        conLink.Connector.ConnectionRestored += ConnectionRestoredHandle;
        conLink.Connector.CurrentTimeChanged += CurrentTimeChangedHandle;
        conLink.Connector.ChangePasswordResult += ChangePasswordResultHandle;
        conLink.Connector.Disposed += DisposedHandle;
        conLink.Connector.Error += ErrorHandle;
        conLink.Connector.Level1Received += Level1ReceivedHandle;
        conLink.Connector.Log += LogHandle;
        conLink.Connector.LookupPortfoliosResult += LookupPortfoliosResultHandle;
        conLink.Connector.LookupSecuritiesResult += LookupSecuritiesResultHandle;
        conLink.Connector.MassOrderCanceled += MassOrderCanceledHandle;
        conLink.Connector.MassOrderCanceled2 += MassOrderCanceled2Handle;
        conLink.Connector.MassOrderCancelFailed += MassOrderCancelFailedHandle;
        conLink.Connector.MassOrderCancelFailed2 += MassOrderCancelFailed2Handle;
        conLink.Connector.NewMessage += NewMessageHandle;
        conLink.Connector.NewsReceived += NewsReceivedHandle;
        conLink.Connector.OrderBookReceived += OrderBookReceivedHandle;
        conLink.Connector.OrderCancelFailReceived += OrderCancelFailReceivedHandle;
        conLink.Connector.OrderEditFailReceived += OrderEditFailReceivedHandle;
        conLink.Connector.OrderLogReceived += OrderLogReceivedHandle;
        conLink.Connector.OrderReceived += OrderReceivedHandle;
        conLink.Connector.OrderRegisterFailReceived += OrderRegisterFailReceivedHandle;
        conLink.Connector.OwnTradeReceived += OwnTradeReceivedHandle;
        conLink.Connector.ParentRemoved += ParentRemovedHandle;
        conLink.Connector.PortfolioReceived += PortfolioReceivedHandle;
        conLink.Connector.PositionReceived += PositionReceivedHandle;
        conLink.Connector.SecurityReceived += SecurityReceivedHandle;
        conLink.Connector.SubscriptionFailed += SubscriptionFailedHandle;
        conLink.Connector.SubscriptionOnline += SubscriptionOnlineHandle;
        conLink.Connector.SubscriptionReceived += SubscriptionReceivedHandle;
        conLink.Connector.SubscriptionStarted += SubscriptionStartedHandle;
        conLink.Connector.SubscriptionStopped += SubscriptionStoppedHandle;
        conLink.Connector.TickTradeReceived += TickTradeReceivedHandle;
        conLink.Connector.ValuesChanged += ValuesChangedHandle;
    }
}