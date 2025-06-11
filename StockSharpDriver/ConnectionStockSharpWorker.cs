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
    ConnectionLink ssLink) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        #region events +
        ssLink.Connector.Connected += ConnectedHandle;
        ssLink.Connector.ConnectedEx += ConnectedExHandle;
        ssLink.Connector.Disconnected += DisconnectedHandle;
        ssLink.Connector.DisconnectedEx += DisconnectedExHandle;
        ssLink.Connector.BoardReceived += BoardReceivedHandle;
        ssLink.Connector.CandleReceived += CandleReceivedHandle;
        ssLink.Connector.ConnectionLost += ConnectionLostHandle;
        ssLink.Connector.ConnectionError += ConnectionErrorHandle;
        ssLink.Connector.DataTypeReceived += DataTypeReceivedHandle;
        ssLink.Connector.ConnectionErrorEx += ConnectionErrorExHandle;
        ssLink.Connector.ConnectionRestored += ConnectionRestoredHandle;
        ssLink.Connector.CurrentTimeChanged += CurrentTimeChangedHandle;
        ssLink.Connector.ChangePasswordResult += ChangePasswordResultHandle;
        ssLink.Connector.Disposed += DisposedHandle;
        ssLink.Connector.Error += ErrorHandle;
        ssLink.Connector.Level1Received += Level1ReceivedHandle;
        ssLink.Connector.Log += LogHandle;
        ssLink.Connector.LookupPortfoliosResult += LookupPortfoliosResultHandle;
        ssLink.Connector.LookupSecuritiesResult += LookupSecuritiesResultHandle;
        ssLink.Connector.MassOrderCanceled += MassOrderCanceledHandle;
        ssLink.Connector.MassOrderCanceled2 += MassOrderCanceled2Handle;
        ssLink.Connector.MassOrderCancelFailed += MassOrderCancelFailedHandle;
        ssLink.Connector.MassOrderCancelFailed2 += MassOrderCancelFailed2Handle;
        ssLink.Connector.NewMessage += NewMessageHandle;
        ssLink.Connector.NewsReceived += NewsReceivedHandle;
        ssLink.Connector.OrderBookReceived += OrderBookReceivedHandle;
        ssLink.Connector.OrderCancelFailReceived += OrderCancelFailReceivedHandle;
        ssLink.Connector.OrderEditFailReceived += OrderEditFailReceivedHandle;
        ssLink.Connector.OrderLogReceived += OrderLogReceivedHandle;
        ssLink.Connector.OrderReceived += OrderReceivedHandle;
        ssLink.Connector.OrderRegisterFailReceived += OrderRegisterFailReceivedHandle;
        ssLink.Connector.OwnTradeReceived += OwnTradeReceivedHandle;
        ssLink.Connector.ParentRemoved += ParentRemovedHandle;
        ssLink.Connector.PortfolioReceived += PortfolioReceivedHandle;
        ssLink.Connector.PositionReceived += PositionReceivedHandle;
        ssLink.Connector.SecurityReceived += SecurityReceivedHandle;
        ssLink.Connector.SubscriptionFailed += SubscriptionFailedHandle;
        ssLink.Connector.SubscriptionOnline += SubscriptionOnlineHandle;
        ssLink.Connector.SubscriptionReceived += SubscriptionReceivedHandle;
        ssLink.Connector.SubscriptionStarted += SubscriptionStartedHandle;
        ssLink.Connector.SubscriptionStopped += SubscriptionStoppedHandle;
        ssLink.Connector.TickTradeReceived += TickTradeReceivedHandle;
        ssLink.Connector.ValuesChanged += ValuesChangedHandle;
        #endregion

        while (!stoppingToken.IsCancellationRequested)
        {
            // _logger.LogDebug($"`tic-tac`");
            await Task.Delay(200, stoppingToken);
        }

        _logger.LogInformation($"call - {nameof(Connector.CancelOrders)}!");
        ssLink.Connector.CancelOrders();

        foreach (Subscription sub in ssLink.Connector.Subscriptions)
        {
            ssLink.Connector.UnSubscribe(sub);
            _logger.LogInformation($"{nameof(Connector.UnSubscribe)} > {sub.GetType().FullName}");
        }

        await ssLink.Connector.DisconnectAsync(stoppingToken);
    }

    /// <inheritdoc/>
    public bool IsConnected => ssLink.Connector.ConnectionState == Ecng.ComponentModel.ConnectionStates.Connected;

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
            CanConnect = ssLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(ssLink.Connector.ConnectionState))
        }).Wait();
    }
    void ConnectionErrorHandle(Exception ex)
    {
        _logger.LogError(ex, $"Call > `{nameof(ConnectionErrorHandle)}`");
        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = ssLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(ssLink.Connector.ConnectionState))
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
            CanConnect = ssLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(ssLink.Connector.ConnectionState))
        }).Wait();
    }
    void DisconnectedHandle()
    {
        _logger.LogWarning($"Call > `{nameof(DisconnectedHandle)}`");

        #region events -
        ssLink.Connector.Connected -= ConnectedHandle;
        ssLink.Connector.ConnectedEx -= ConnectedExHandle;
        ssLink.Connector.Disconnected -= DisconnectedHandle;
        ssLink.Connector.BoardReceived -= BoardReceivedHandle;
        ssLink.Connector.CandleReceived -= CandleReceivedHandle;
        ssLink.Connector.ConnectionLost -= ConnectionLostHandle;
        ssLink.Connector.ConnectionError -= ConnectionErrorHandle;
        ssLink.Connector.DataTypeReceived -= DataTypeReceivedHandle;
        ssLink.Connector.ConnectionErrorEx -= ConnectionErrorExHandle;
        ssLink.Connector.ConnectionRestored -= ConnectionRestoredHandle;
        ssLink.Connector.CurrentTimeChanged -= CurrentTimeChangedHandle;
        ssLink.Connector.ChangePasswordResult -= ChangePasswordResultHandle;
        ssLink.Connector.DisconnectedEx -= DisconnectedExHandle;
        ssLink.Connector.Disposed -= DisposedHandle;
        ssLink.Connector.Error -= ErrorHandle;
        ssLink.Connector.Level1Received -= Level1ReceivedHandle;
        ssLink.Connector.Log -= LogHandle;
        ssLink.Connector.LookupPortfoliosResult -= LookupPortfoliosResultHandle;
        ssLink.Connector.LookupSecuritiesResult -= LookupSecuritiesResultHandle;
        ssLink.Connector.MassOrderCanceled -= MassOrderCanceledHandle;
        ssLink.Connector.MassOrderCanceled2 -= MassOrderCanceled2Handle;
        ssLink.Connector.MassOrderCancelFailed -= MassOrderCancelFailedHandle;
        ssLink.Connector.MassOrderCancelFailed2 -= MassOrderCancelFailed2Handle;
        ssLink.Connector.NewMessage -= NewMessageHandle;
        ssLink.Connector.NewsReceived -= NewsReceivedHandle;
        ssLink.Connector.OrderBookReceived -= OrderBookReceivedHandle;
        ssLink.Connector.OrderCancelFailReceived -= OrderCancelFailReceivedHandle;
        ssLink.Connector.OrderEditFailReceived -= OrderEditFailReceivedHandle;
        ssLink.Connector.OrderLogReceived -= OrderLogReceivedHandle;
        ssLink.Connector.OrderReceived -= OrderReceivedHandle;
        ssLink.Connector.OrderRegisterFailReceived -= OrderRegisterFailReceivedHandle;
        ssLink.Connector.OwnTradeReceived -= OwnTradeReceivedHandle;
        ssLink.Connector.ParentRemoved -= ParentRemovedHandle;
        ssLink.Connector.PortfolioReceived -= PortfolioReceivedHandle;
        ssLink.Connector.PositionReceived -= PositionReceivedHandle;
        ssLink.Connector.SecurityReceived -= SecurityReceivedHandle;
        ssLink.Connector.SubscriptionFailed -= SubscriptionFailedHandle;
        ssLink.Connector.SubscriptionOnline -= SubscriptionOnlineHandle;
        ssLink.Connector.SubscriptionReceived -= SubscriptionReceivedHandle;
        ssLink.Connector.SubscriptionStarted -= SubscriptionStartedHandle;
        ssLink.Connector.SubscriptionStopped -= SubscriptionStoppedHandle;
        ssLink.Connector.TickTradeReceived -= TickTradeReceivedHandle;
        ssLink.Connector.ValuesChanged -= ValuesChangedHandle;
        #endregion

        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = ssLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(ssLink.Connector.ConnectionState))
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
            CanConnect = ssLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(ssLink.Connector.ConnectionState))
        }).Wait();
    }
    void ConnectionLostHandle(StockSharp.Messages.IMessageAdapter sender)
    {
        _logger.LogWarning($"Call > `{nameof(ConnectionLostHandle)}`: {JsonConvert.SerializeObject(sender)}");
        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = ssLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(ssLink.Connector.ConnectionState))
        }).Wait();
    }
    void ConnectedExHandle(StockSharp.Messages.IMessageAdapter sender)
    {
        _logger.LogWarning($"Call > `{nameof(ConnectedExHandle)}`: {JsonConvert.SerializeObject(new { sender.Name, sender.Categories })}");
        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = ssLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(ssLink.Connector.ConnectionState))
        }).Wait();
    }
    void ConnectedHandle()
    {
        _logger.LogWarning($"Call > `{nameof(ConnectedHandle)}`");
        eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = ssLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(ssLink.Connector.ConnectionState))
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
}