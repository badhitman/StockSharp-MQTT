////////////////////////////////////////////////
// � https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using StockSharp.BusinessEntities;
using StockSharp.Algo;
using Newtonsoft.Json;
using SharedLib;

namespace StockSharpDriver;

/// <inheritdoc/>
public class ConnectionStockSharpWorker(
    //StockSharpClientConfigModel conf,
    IFlushStockSharpService dataRepo,
    ILogger<ConnectionStockSharpWorker> _logger,
    IEventsStockSharpService eventTrans,
    Connector Connector) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        #region events +
        Connector.Connected += ConnectedHandle;
        Connector.ConnectedEx += ConnectedExHandle;
        Connector.Disconnected += DisconnectedHandle;
        Connector.BoardReceived += BoardReceivedHandle;
        Connector.CandleReceived += CandleReceivedHandle;
        Connector.ConnectionLost += ConnectionLostHandle;
        Connector.ConnectionError += ConnectionErrorHandle;
        Connector.DataTypeReceived += DataTypeReceivedHandle;
        Connector.ConnectionErrorEx += ConnectionErrorExHandle;
        Connector.ConnectionRestored += ConnectionRestoredHandle;
        Connector.CurrentTimeChanged += CurrentTimeChangedHandle;
        Connector.ChangePasswordResult += ChangePasswordResultHandle;
        Connector.DisconnectedEx += DisconnectedExHandle;
        Connector.Disposed += DisposedHandle;
        Connector.Error += ErrorHandle;
        Connector.Level1Received += Level1ReceivedHandle;
        Connector.Log += LogHandle;
        Connector.LookupPortfoliosResult += LookupPortfoliosResultHandle;
        Connector.LookupSecuritiesResult += LookupSecuritiesResultHandle;
        Connector.MassOrderCanceled += MassOrderCanceledHandle;
        Connector.MassOrderCanceled2 += MassOrderCanceled2Handle;
        Connector.MassOrderCancelFailed += MassOrderCancelFailedHandle;
        Connector.MassOrderCancelFailed2 += MassOrderCancelFailed2Handle;
        Connector.NewMessage += NewMessageHandle;
        Connector.NewsReceived += NewsReceivedHandle;
        Connector.OrderBookReceived += OrderBookReceivedHandle;
        Connector.OrderCancelFailReceived += OrderCancelFailReceivedHandle;
        Connector.OrderEditFailReceived += OrderEditFailReceivedHandle;
        Connector.OrderLogReceived += OrderLogReceivedHandle;
        Connector.OrderReceived += OrderReceivedHandle;
        Connector.OrderRegisterFailReceived += OrderRegisterFailReceivedHandle;
        Connector.OwnTradeReceived += OwnTradeReceivedHandle;
        Connector.ParentRemoved += ParentRemovedHandle;
        Connector.PortfolioReceived += PortfolioReceivedHandle;
        Connector.PositionReceived += PositionReceivedHandle;
        Connector.SecurityReceived += SecurityReceivedHandle;
        Connector.SubscriptionFailed += SubscriptionFailedHandle;
        Connector.SubscriptionOnline += SubscriptionOnlineHandle;
        Connector.SubscriptionReceived += SubscriptionReceivedHandle;
        Connector.SubscriptionStarted += SubscriptionStartedHandle;
        Connector.SubscriptionStopped += SubscriptionStoppedHandle;
        Connector.TickTradeReceived += TickTradeReceivedHandle;
        Connector.ValuesChanged += ValuesChangedHandle;
        #endregion

        while (!stoppingToken.IsCancellationRequested)
        {
            // _logger.LogDebug($"`tic-tac`");
            await Task.Delay(200, stoppingToken);
        }

        _logger.LogInformation($"call - {nameof(Connector.CancelOrders)}!");
        Connector.CancelOrders();

        foreach (Subscription sub in Connector.Subscriptions)
        {
            Connector.UnSubscribe(sub);
            _logger.LogInformation($"{nameof(Connector.UnSubscribe)} > {sub.GetType().FullName}");
        }

        await Connector.DisconnectAsync(stoppingToken);
    }


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
        dataRepo.SaveInstrument(instrument);
        eventTrans.InstrumentReceived(instrument);
    }

    void PortfolioReceivedHandle(Subscription subscription, Portfolio port)
    {
        _logger.LogInformation($"Call > `{nameof(PortfolioReceivedHandle)}`: {JsonConvert.SerializeObject(port)}");
        PortfolioStockSharpModel portfolio = new PortfolioStockSharpModel().Bind(port);
        dataRepo.SavePortfolio(portfolio);
        eventTrans.PortfolioReceived(portfolio);
    }

    void BoardReceivedHandle(Subscription subscription, ExchangeBoard boardExchange)
    {
        _logger.LogWarning($"Call > `{nameof(BoardReceivedHandle)}`: {JsonConvert.SerializeObject(boardExchange)}");
        BoardStockSharpModel board = new BoardStockSharpModel().Bind(boardExchange);
        dataRepo.SaveBoard(board);
        eventTrans.BoardReceived(board);
    }

    void OrderReceivedHandle(Subscription subscription, Order oreder)
    {
        _logger.LogWarning($"Call > `{nameof(OrderReceivedHandle)}`: {JsonConvert.SerializeObject(oreder)}");
        OrderStockSharpModel order = new OrderStockSharpModel().Bind(oreder);
        dataRepo.SaveOrder(order);
        eventTrans.OrderReceived(order);
    }

    void OwnTradeReceivedHandle(Subscription subscription, MyTrade tr)
    {
        _logger.LogWarning($"Call > `{nameof(OwnTradeReceivedHandle)}`: {JsonConvert.SerializeObject(tr)}");
        MyTradeStockSharpModel myTrade = new MyTradeStockSharpModel().Bind(tr);
        dataRepo.SaveTrade(myTrade);
        eventTrans.OwnTradeReceived(myTrade);
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


        #region events -
        Connector.Connected -= ConnectedHandle;
        Connector.ConnectedEx -= ConnectedExHandle;
        Connector.Disconnected -= DisconnectedHandle;
        Connector.BoardReceived -= BoardReceivedHandle;
        Connector.CandleReceived -= CandleReceivedHandle;
        Connector.ConnectionLost -= ConnectionLostHandle;
        Connector.ConnectionError -= ConnectionErrorHandle;
        Connector.DataTypeReceived -= DataTypeReceivedHandle;
        Connector.ConnectionErrorEx -= ConnectionErrorExHandle;
        Connector.ConnectionRestored -= ConnectionRestoredHandle;
        Connector.CurrentTimeChanged -= CurrentTimeChangedHandle;
        Connector.ChangePasswordResult -= ChangePasswordResultHandle;
        Connector.DisconnectedEx -= DisconnectedExHandle;
        Connector.Disposed -= DisposedHandle;
        Connector.Error -= ErrorHandle;
        Connector.Level1Received -= Level1ReceivedHandle;
        Connector.Log -= LogHandle;
        Connector.LookupPortfoliosResult -= LookupPortfoliosResultHandle;
        Connector.LookupSecuritiesResult -= LookupSecuritiesResultHandle;
        Connector.MassOrderCanceled -= MassOrderCanceledHandle;
        Connector.MassOrderCanceled2 -= MassOrderCanceled2Handle;
        Connector.MassOrderCancelFailed -= MassOrderCancelFailedHandle;
        Connector.MassOrderCancelFailed2 -= MassOrderCancelFailed2Handle;
        Connector.NewMessage -= NewMessageHandle;
        Connector.NewsReceived -= NewsReceivedHandle;
        Connector.OrderBookReceived -= OrderBookReceivedHandle;
        Connector.OrderCancelFailReceived -= OrderCancelFailReceivedHandle;
        Connector.OrderEditFailReceived -= OrderEditFailReceivedHandle;
        Connector.OrderLogReceived -= OrderLogReceivedHandle;
        Connector.OrderReceived -= OrderReceivedHandle;
        Connector.OrderRegisterFailReceived -= OrderRegisterFailReceivedHandle;
        Connector.OwnTradeReceived -= OwnTradeReceivedHandle;
        Connector.ParentRemoved -= ParentRemovedHandle;
        Connector.PortfolioReceived -= PortfolioReceivedHandle;
        Connector.PositionReceived -= PositionReceivedHandle;
        Connector.SecurityReceived -= SecurityReceivedHandle;
        Connector.SubscriptionFailed -= SubscriptionFailedHandle;
        Connector.SubscriptionOnline -= SubscriptionOnlineHandle;
        Connector.SubscriptionReceived -= SubscriptionReceivedHandle;
        Connector.SubscriptionStarted -= SubscriptionStartedHandle;
        Connector.SubscriptionStopped -= SubscriptionStoppedHandle;
        Connector.TickTradeReceived -= TickTradeReceivedHandle;
        Connector.ValuesChanged -= ValuesChangedHandle;
        #endregion
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
        _logger.LogWarning($"Call > `{nameof(ConnectedExHandle)}`: {JsonConvert.SerializeObject(new { sender.Name, sender.Categories })}");
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
}