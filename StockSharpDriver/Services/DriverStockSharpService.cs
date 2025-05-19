////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using StockSharp.BusinessEntities;
using StockSharp.Fix.Quik.Lua;
using StockSharp.Messages;
using StockSharp.Algo;
using System.Security;
using Ecng.Common;
using System.Net;
using SharedLib;
using System.Collections.Concurrent;

namespace StockSharpDriver;

/// <summary>
/// DriverStockSharpService 
/// </summary>
public class DriverStockSharpService(
    IManageStockSharpService manageRepo,
    ILogger<DriverStockSharpService> _logger,
    Connector connector) : IDriverStockSharpService
{
    private decimal lowLimit = 0.19m;
    private decimal highLimit = 0.25m;
    private readonly decimal
        lowYieldLimit = 4m,
        highYieldLimit = 5m;

    private readonly ConcurrentDictionary<string, List<Security>> BondList = [];

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> Connect(CancellationToken? cancellationToken = default)
    {
        if (BondList.Any())
            return ResponseBaseModel.CreateError($"BondList is not empty!");

        TPaginationRequestStandardModel<AdaptersRequestModel> reqAs = new()
        {
            Payload = new()
            {
                OnlineOnly = true
            },
            PageNum = 0,
            PageSize = int.MaxValue,
        };
        TPaginationResponseModel<FixMessageAdapterModelDB> adapters;
        try
        {
            adapters = await manageRepo.AdaptersSelectAsync(reqAs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $" {nameof(manageRepo.AdaptersSelectAsync)}");
            return ResponseBaseModel.CreateError(ex);
        }

        if (adapters.Response is null || adapters.Response.Count == 0)
            return ResponseBaseModel.CreateError("adapters - is empty");

        #region event +
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
        #endregion

        ResponseBaseModel res = new();
        adapters.Response.ForEach(x =>
        {
            try
            {
                IPEndPoint _cep = GlobalToolsStandard.CreateIPEndPoint(x.Address);
                SecureString secure = new();
                if (x.Password is not null)
                    foreach (char c in x.Password)
                        secure.AppendChar(c);

                switch (Enum.GetName(typeof(AdaptersTypesNames), x.AdapterTypeName))
                {
                    case nameof(LuaFixMarketDataMessageAdapter):
                        LuaFixMarketDataMessageAdapter luaFixMarketDataMessageAdapter = new(connector.TransactionIdGenerator)
                        {
                            Address = x.Address.To<EndPoint>(), //"localhost:5001".To<EndPoint>(),
                            Login = x.Login,
                            Password = secure,
                            IsDemo = true,
                        };
                        connector.Adapter.InnerAdapters.Add(luaFixMarketDataMessageAdapter);
                        break;
                    case nameof(LuaFixTransactionMessageAdapter):
                        LuaFixTransactionMessageAdapter luaFixTransactionMessageAdapter = new(connector.TransactionIdGenerator)
                        {
                            Address = x.Address.To<EndPoint>(),
                            Login = x.Login,
                            Password = secure,
                            IsDemo = true,
                        };
                        connector.Adapter.InnerAdapters.Add(luaFixTransactionMessageAdapter);
                        break;
                    default:
                        _logger.LogError($"error detect adapter [{x.AdapterTypeName}]");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка добавления адаптера");
                res.Messages.InjectException(ex);
            }
        });

        if (!connector.CanConnect)
            return ResponseBaseModel.CreateError("can`t connect");

        await connector.ConnectAsync(cancellationToken ?? CancellationToken.None);
        res.AddInfo($"connection: {connector.ConnectionState}");
        return res;
    }

    /// <inheritdoc/>
    public Task<ResponseBaseModel> Disconnect(CancellationToken? cancellationToken = default)
    {
        connector.CancelOrders();
        foreach (Subscription sub in connector.Subscriptions)
        {
            connector.UnSubscribe(sub);
            _logger.LogInformation($"{nameof(Connector.UnSubscribe)} > {sub.GetType().FullName}");
        }
        BondList.Clear();

        connector.Disconnect();

        return Task.FromResult(ResponseBaseModel.CreateInfo("connection closed"));
    }

    /// <inheritdoc/>
    public Task<AboutConnectResponseModel> AboutConnection(CancellationToken? cancellationToken = null)
    {
        AboutConnectResponseModel res = new()
        {
            CanConnect = connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(connector.ConnectionState)),
        };
        return Task.FromResult(res);
    }

    /// <inheritdoc/>
    public Task<ResponseBaseModel> OrderRegisterAsync(CreateOrderRequestModel req, CancellationToken cancellationToken = default)
    {
        ExchangeBoard board = req.Instrument.Board is null
        ? null
            : connector.ExchangeBoards.FirstOrDefault(x => x.Code == req.Instrument.Board.Code && (x.Exchange.Name == req.Instrument.Board.Exchange.Name || x.Exchange.CountryCode.ToString() == req.Instrument.Board.Exchange.CountryCode.ToString()));
        Security currentSec = connector.Securities.FirstOrDefault(x => x.Name == req.Instrument.Name && x.Code == req.Instrument.Code && x.Board.Code == board.Code && x.Board.Exchange.Name == board.Exchange.Name && x.Board.Exchange.CountryCode == board.Exchange.CountryCode);
        if (currentSec is null)
            return Task.FromResult(ResponseBaseModel.CreateError($"Инструмент не найден: {req.Instrument}"));

        Portfolio selectedPortfolio = connector.Portfolios.FirstOrDefault(x => x.ClientCode == req.Portfolio.ClientCode && x.Name == req.Portfolio.Name);
        if (selectedPortfolio is null)
            return Task.FromResult(ResponseBaseModel.CreateError($"Портфель не найден: {req.Portfolio}"));

        Order order = new()
        {
            // устанавливается тип заявки, в данном примере лимитный
            Type = (OrderTypes)Enum.Parse(typeof(OrderTypes), Enum.GetName(req.OrderType)),
            // устанавливается портфель для исполнения заявки
            Portfolio = selectedPortfolio,
            // устанавливается объём заявки
            Volume = req.Volume,
            // устанавливается цена заявки
            Price = req.Price,
            // устанавливается инструмент
            Security = currentSec,
            // устанавливается направление заявки, в данном примере покупка
            Side = (Sides)Enum.Parse(typeof(Sides), Enum.GetName(req.Side))
        };

        connector.RegisterOrder(order);
        return Task.FromResult(ResponseBaseModel.CreateInfo("Заявка отправлена на регистрацию"));
    }


    void ValuesChangedHandle(Security instrument, IEnumerable<KeyValuePair<StockSharp.Messages.Level1Fields, object>> dataPayload, DateTimeOffset dtOffsetMaster, DateTimeOffset dtOffsetSlave)
    {
        //_logger.LogInformation($"Call > `{nameof(ValuesChangedHandle)}` [{dtOffsetMaster}]/[{dtOffsetSlave}]: {JsonConvert.SerializeObject(instrument)}\n\n{JsonConvert.SerializeObject(dataPayload)}");
        //ConnectorValuesChangedEventPayloadModel req = new()
        //{
        //    OffsetSlave = dtOffsetSlave,
        //    OffsetMaster = dtOffsetMaster,
        //    DataPayload = [.. dataPayload.Select(x => new KeyValuePair<Level1FieldsStockSharpEnum, object>((Level1FieldsStockSharpEnum)Enum.Parse(typeof(Level1FieldsStockSharpEnum), Enum.GetName(x.Key)!), x.Value))],
        //    Instrument = new InstrumentTradeStockSharpModel().Bind(instrument),
        //};
        //dataRepo.SaveInstrument(req.Instrument);
        //eventTrans.ValuesChangedEvent(req);
    }

    void SecurityReceivedHandle(Subscription subscription, Security security)
    {
        //_logger.LogTrace($"Call > `{nameof(SecurityReceivedHandle)}`");
        //InstrumentTradeStockSharpModel req = new InstrumentTradeStockSharpModel().Bind(sec);
        //dataRepo.SaveInstrument(req);
        //eventTrans.InstrumentReceived(req);
        
        //    BondList.Add(security);
    }

    void PortfolioReceivedHandle(Subscription subscription, Portfolio port)
    {
        //_logger.LogInformation($"Call > `{nameof(PortfolioReceivedHandle)}`: {JsonConvert.SerializeObject(port)}");
        //PortfolioStockSharpModel req = new PortfolioStockSharpModel().Bind(port);
        //dataRepo.SavePortfolio(req);
        //eventTrans.PortfolioReceived(req);
    }

    void BoardReceivedHandle(Subscription subscription, ExchangeBoard boardExchange)
    {
        //_logger.LogWarning($"Call > `{nameof(BoardReceivedHandle)}`: {JsonConvert.SerializeObject(boardExchange)}");
        //BoardStockSharpModel req = new BoardStockSharpModel().Bind(boardExchange);
        //dataRepo.SaveBoard(req);
        //eventTrans.BoardReceived(req);
    }

    void OrderReceivedHandle(Subscription subscription, Order oreder)
    {
        //_logger.LogWarning($"Call > `{nameof(OrderReceivedHandle)}`: {JsonConvert.SerializeObject(oreder)}");
        //OrderStockSharpModel req = new OrderStockSharpModel().Bind(oreder);
        //dataRepo.SaveOrder(req);
        //eventTrans.OrderReceived(req);
    }

    void PositionReceivedHandle(Subscription subscription, Position pos)
    {
        //_logger.LogWarning($"Call > `{nameof(PositionReceivedHandle)}`: {JsonConvert.SerializeObject(pos)}");
    }
    void OwnTradeReceivedHandle(Subscription subscription, MyTrade tr)
    {
        //_logger.LogWarning($"Call > `{nameof(OwnTradeReceivedHandle)}`: {JsonConvert.SerializeObject(tr)}");
    }

    #region todo
    void OrderBookReceivedHandle(Subscription subscription, IOrderBookMessage orderBM)
    {
        //_logger.LogWarning($"Call > `{nameof(OrderBookReceivedHandle)}`: {JsonConvert.SerializeObject(orderBM)}");
        /*
         Security sec = connector.Securities.FirstOrDefault(s => s.ToSecurityId() == depth.SecurityId);

        if ((!BondList.IsNull()) && (BondList.Contains(sec)))
        {
            if (OderBookList.ContainsKey(sec))
                OderBookList[sec] = depth;
            else
                OderBookList.Add(sec, depth);
        }
         */
    }
    void OrderLogReceivedHandle(Subscription subscription, IOrderLogMessage order)
    {
        //_logger.LogWarning($"Call > `{nameof(OrderLogReceivedHandle)}`: {JsonConvert.SerializeObject(order)}");
    }
    void OrderRegisterFailReceivedHandle(Subscription subscription, OrderFail orderF)
    {
        //_logger.LogWarning($"Call > `{nameof(OrderRegisterFailReceivedHandle)}`: {JsonConvert.SerializeObject(orderF)}");
    }
    void OrderCancelFailReceivedHandle(Subscription subscription, OrderFail orderF)
    {
        //_logger.LogWarning($"Call > `{nameof(OrderCancelFailReceivedHandle)}`: {JsonConvert.SerializeObject(orderF)}");
    }
    void OrderEditFailReceivedHandle(Subscription subscription, OrderFail orderF)
    {
        //_logger.LogWarning($"Call > `{nameof(OrderEditFailReceivedHandle)}`: {JsonConvert.SerializeObject(orderF)}");
    }

    void TickTradeReceivedHandle(Subscription subscription, ITickTradeMessage msg)
    {
        //_logger.LogWarning($"Call > `{nameof(TickTradeReceivedHandle)}`: {JsonConvert.SerializeObject(msg)}");
    }
    void SubscriptionStartedHandle(Subscription subscription)
    {
        //_logger.LogWarning($"Call > `{nameof(SubscriptionStartedHandle)}`");
    }
    void SubscriptionOnlineHandle(Subscription subscription)
    {
        //_logger.LogWarning($"Call > `{nameof(SubscriptionOnlineHandle)}`");
    }
    void ParentRemovedHandle(Ecng.Logging.ILogSource sender)
    {
        //_logger.LogWarning($"Call > `{nameof(ParentRemovedHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }

    void NewsReceivedHandle(Subscription subscription, News sender)
    {
        //_logger.LogWarning($"Call > `{nameof(NewsReceivedHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    void MassOrderCanceled2Handle(long arg, DateTimeOffset dt)
    {
        //_logger.LogWarning($"Call > `{nameof(MassOrderCanceled2Handle)}` [{nameof(arg)}:{arg}]: {dt}");
    }
    void MassOrderCanceledHandle(long sender)
    {
        //_logger.LogWarning($"Call > `{nameof(MassOrderCanceledHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    void Level1ReceivedHandle(Subscription subscription, Level1ChangeMessage levelCh)
    {
        //_logger.LogWarning($"Call > `{nameof(Level1ReceivedHandle)}`: {JsonConvert.SerializeObject(levelCh)}");
    }
    void DisposedHandle()
    {
        //_logger.LogWarning($"Call > `{nameof(DisposedHandle)}`");
    }
    void DisconnectedExHandle(IMessageAdapter sender)
    {
        //_logger.LogWarning($"Call > `{nameof(DisconnectedExHandle)}`: {JsonConvert.SerializeObject(sender)}");

        #region event -
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
        #endregion

    }
    void DisconnectedHandle()
    {
        _logger.LogWarning($"Call > `{nameof(DisconnectedHandle)}`");
    }
    void DataTypeReceivedHandle(Subscription subscription, DataType argDt)
    {
        //_logger.LogWarning($"Call > `{nameof(DataTypeReceivedHandle)}`: {JsonConvert.SerializeObject(argDt)}");
    }
    void ConnectionRestoredHandle(IMessageAdapter sender)
    {
        //_logger.LogWarning($"Call > `{nameof(ConnectionRestoredHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    void ConnectionLostHandle(IMessageAdapter sender)
    {
        //_logger.LogWarning($"Call > `{nameof(ConnectionLostHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    void ConnectedExHandle(IMessageAdapter sender)
    {
        //_logger.LogWarning($"Call > `{nameof(ConnectedExHandle)}`: {JsonConvert.SerializeObject(new { sender.Name, sender.Categories })}");
    }
    void ConnectedHandle()
    {
        //_logger.LogWarning($"Call > `{nameof(ConnectedHandle)}`");
    }
    void CandleReceivedHandle(Subscription subscription, ICandleMessage candleMessage)
    {
        //_logger.LogWarning($"Call > `{nameof(CandleReceivedHandle)}`: {JsonConvert.SerializeObject(candleMessage)}");
    }
    void LogHandle(Ecng.Logging.LogMessage senderLog)
    {
        //_logger.LogTrace($"Call > `{nameof(LogHandle)}`: {senderLog}");
    }
    void CurrentTimeChangedHandle(TimeSpan sender)
    {
        //_logger.LogTrace($"Call > `{nameof(CurrentTimeChangedHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    void NewMessageHandle(Message msg)
    {
        //_logger.LogTrace($"Call > `{nameof(NewMessageHandle)}`: {JsonConvert.SerializeObject(msg)}");
    }
    void SubscriptionReceivedHandle(Subscription subscription, object sender)
    {
        //_logger.LogTrace($"Call > `{nameof(SubscriptionReceivedHandle)}`: {JsonConvert.SerializeObject(sender)}");
    }
    #endregion

    #region Exception`s
    void LookupSecuritiesResultHandle(SecurityLookupMessage slm, IEnumerable<Security> securities, Exception ex)
    {
        // _logger.LogError(ex, $"Call > `{nameof(LookupSecuritiesResultHandle)}`: {JsonConvert.SerializeObject(slm)}");

        // foreach (Security sec in securities)
        //    dataRepo.SaveInstrument(new InstrumentTradeStockSharpModel().Bind(sec));
    }

    void LookupPortfoliosResultHandle(PortfolioLookupMessage portfolioLM, IEnumerable<Portfolio> portfolios, Exception ex)
    {
        // _logger.LogError(ex, $"Call > `{nameof(LookupPortfoliosResultHandle)}`: {JsonConvert.SerializeObject(portfolioLM)}");

        //foreach (Portfolio port in portfolios)
        //    dataRepo.SavePortfolio(new PortfolioStockSharpModel().Bind(port));
    }

    void SubscriptionFailedHandle(Subscription subscription, Exception ex, bool arg)
    {
        // _logger.LogError(ex, $"Call > `{nameof(SubscriptionFailedHandle)}`: [{nameof(arg)}:{arg}]");
    }
    void SubscriptionStoppedHandle(Subscription subscription, Exception ex)
    {
        // _logger.LogError(ex, $"Call > `{nameof(SubscriptionStoppedHandle)}`");
    }
    void MassOrderCancelFailed2Handle(long arg, Exception ex, DateTimeOffset dt)
    {
        //_logger.LogError(ex, $"Call > `{nameof(MassOrderCancelFailed2Handle)}` [{nameof(arg)}:{arg}]: {dt}");
    }
    void MassOrderCancelFailedHandle(long arg, Exception ex)
    {
        //_logger.LogError(ex, $"Call > `{nameof(MassOrderCancelFailedHandle)}` [{nameof(arg)}:{arg}]");
    }
    void ConnectionErrorExHandle(IMessageAdapter sender, Exception ex)
    {
        //_logger.LogError(ex, $"Call > `{nameof(ConnectionErrorExHandle)}`");
    }
    void ConnectionErrorHandle(Exception ex)
    {
        // _logger.LogError(ex, $"Call > `{nameof(ConnectionErrorHandle)}`");
    }
    void ErrorHandle(Exception ex)
    {
        // _logger.LogError(ex, $"Call > `{nameof(ErrorHandle)}`");
    }
    void ChangePasswordResultHandle(long arg, Exception ex)
    {
        // _logger.LogError(ex, $"Call > `{nameof(ChangePasswordResultHandle)}`: {arg}");
    }
    #endregion

    /// <inheritdoc/>
    public Task<ResponseBaseModel> PingAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ResponseBaseModel.CreateSuccess($"Ok - {nameof(DriverStockSharpService)}"));
    }
}