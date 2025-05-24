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

namespace StockSharpDriver;

/// <summary>
/// DriverStockSharpService 
/// </summary>
public class DriverStockSharpService(
    ILogger<DriverStockSharpService> _logger,
    IManageStockSharpService manageRepo,
    ConnectionLink conLink) : IDriverStockSharpService
{
    readonly List<SecurityPosition> SBondPositionsList = [];
    readonly List<SecurityPosition> SBondSizePositionsList = [];
    readonly List<SecurityPosition> SBondSmallPositionsList = [];
    static readonly List<long?> list = [];
    readonly List<long?> TradesList = list;

    readonly FileSystemWatcher fileWatcher = new();

    readonly object _lockLastConnectedAt = new();
    DateTime _lastConnectedAt = DateTime.MinValue;
    DateTime LastConnectedAt
    {
        get
        {
            lock (_lockLastConnectedAt)
                return _lastConnectedAt;
        }
        set
        {
            lock (_lockLastConnectedAt)
                _lastConnectedAt = value;
        }
    }

    Curve OfzCurve;

    decimal quoteSmallStrategyBidVolume = 2000;
    decimal quoteSmallStrategyOfferVolume = 2000;
    decimal quoteStrategyVolume = 1000;
    decimal skipVolume = 2500;
    decimal quoteSizeStrategyVolume = 2000;

    decimal bondPositionTraded;
    decimal bondSizePositionTraded;
    decimal bondSmallPositionTraded;
    decimal bondOutOfRangePositionTraded;

    Dictionary<string, Order> _ordersForQuoteBuyReregister;
    Dictionary<string, Order> _ordersForQuoteSellReregister;

    readonly Dictionary<Security, IOrderBookMessage> OderBookList = [];

    decimal lowLimit = 0.19m;
    decimal highLimit = 0.25m;
    readonly decimal
       lowYieldLimit = 4m,
       highYieldLimit = 5m;

    readonly List<Security> BondList = [];
    readonly List<SBond> SBondList = [];

    PortfolioStockSharpModel Portfolio;
    List<InstrumentTradeStockSharpViewModel> Instruments;
    List<FixMessageAdapterModelDB> Adapters;
    List<BoardStockSharpModel> BoardsFilter;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> Connect(ConnectRequestModel req, CancellationToken? cancellationToken = default)
    {
        if (BondList.Any())
            return ResponseBaseModel.CreateError($"BondList is not empty!");

        if (req.Instruments is null || req.Instruments.Count == 0)
            return ResponseBaseModel.CreateError("Instruments - is empty");

        if (req.Portfolio is null)
            return ResponseBaseModel.CreateError("Portfolio not set");

        TPaginationRequestStandardModel<AdaptersRequestModel> adReq = new()
        {
            PageSize = int.MaxValue,
            Payload = new()
            {
                OnlineOnly = true,
            }
        };

        TPaginationResponseModel<FixMessageAdapterModelDB> adRes = await manageRepo.AdaptersSelectAsync(adReq);
        if (adRes.Response is null || adRes.Response.Count == 0)
            return ResponseBaseModel.CreateError("adapters - is empty");

        List<FixMessageAdapterModelDB> adapters = adRes.Response;

        Portfolio = req.Portfolio;
        Instruments = req.Instruments;
        BoardsFilter = req.BoardsFilter;
        LastConnectedAt = DateTime.UtcNow;

        /*
         SecurityLookupWindow wnd = new()
        {
            ShowAllOption = connector.Adapter.IsSupportSecuritiesLookupAll(),
            Criteria = new Security { Type = SecurityTypes.Stock, Code = "SU" }
        };

        if (!wnd.ShowModal(this))
            return;

        connector.LookupSecurities(wnd.CriteriaMessage);

        //SecurityEditor.SecurityProvider = QTrader;
        PortfolioEditor.Portfolios = new PortfolioDataSource(connector);

        //Подписаться на событие появления новых портфелей
        connector.PortfolioReceived += (Sub, portfolios) =>
        {
            if (portfolios.Name == PortName)
            {
                MyPortf = portfolios;
            }
        };

        

        connector.OrderBookReceived += TraderOnMarketDepthReceived;
        
         */

        #region event +
        conLink.Connector.Connected += ConnectedHandle;
        conLink.Connector.ConnectedEx += ConnectedExHandle;
        conLink.Connector.Disconnected += DisconnectedHandle;
        conLink.Connector.BoardReceived += BoardReceivedHandle;
        conLink.Connector.CandleReceived += CandleReceivedHandle;
        conLink.Connector.ConnectionLost += ConnectionLostHandle;
        conLink.Connector.ConnectionError += ConnectionErrorHandle;
        conLink.Connector.DataTypeReceived += DataTypeReceivedHandle;
        conLink.Connector.ConnectionErrorEx += ConnectionErrorExHandle;
        conLink.Connector.ConnectionRestored += ConnectionRestoredHandle;
        conLink.Connector.CurrentTimeChanged += CurrentTimeChangedHandle;
        conLink.Connector.ChangePasswordResult += ChangePasswordResultHandle;
        conLink.Connector.DisconnectedEx += DisconnectedExHandle;
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
        #endregion

        ResponseBaseModel res = new();
        adapters.ForEach(x =>
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
                        LuaFixMarketDataMessageAdapter luaFixMarketDataMessageAdapter = new(conLink.Connector.TransactionIdGenerator)
                        {
                            Address = x.Address.To<EndPoint>(), //"localhost:5001".To<EndPoint>(),
                            Login = x.Login,
                            Password = secure,
                            IsDemo = true,
                        };
                        conLink.Connector.Adapter.InnerAdapters.Add(luaFixMarketDataMessageAdapter);
                        break;
                    case nameof(LuaFixTransactionMessageAdapter):
                        LuaFixTransactionMessageAdapter luaFixTransactionMessageAdapter = new(conLink.Connector.TransactionIdGenerator)
                        {
                            Address = x.Address.To<EndPoint>(),
                            Login = x.Login,
                            Password = secure,
                            IsDemo = true,
                        };
                        conLink.Connector.Adapter.InnerAdapters.Add(luaFixTransactionMessageAdapter);
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

        if (!conLink.Connector.CanConnect)
            return ResponseBaseModel.CreateError("can`t connect");

        await conLink.Connector.ConnectAsync(cancellationToken ?? CancellationToken.None);
        res.AddInfo($"connection: {conLink.Connector.ConnectionState}");
        return res;
    }

    /// <inheritdoc/>
    public Task<ResponseBaseModel> Disconnect(CancellationToken? cancellationToken = default)
    {
        conLink.Connector.CancelOrders();
        foreach (Subscription sub in conLink.Connector.Subscriptions)
        {
            conLink.Connector.UnSubscribe(sub);
            _logger.LogInformation($"{nameof(Connector.UnSubscribe)} > {sub.GetType().FullName}");
        }
        BondList.Clear();
        conLink.Connector.Disconnect();
        Portfolio = null;
        return Task.FromResult(ResponseBaseModel.CreateInfo("connection closed"));
    }

    /// <inheritdoc/>
    public Task<AboutConnectResponseModel> AboutConnection(CancellationToken? cancellationToken = null)
    {
        DateTime _lc = LastConnectedAt;
        AboutConnectResponseModel res = new()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState)),
            LastConnectedAt = _lc == DateTime.MinValue ? null : _lc,
        };
        return Task.FromResult(res);
    }

    /// <inheritdoc/>
    public Task<ResponseBaseModel> OrderRegisterAsync(CreateOrderRequestModel req, CancellationToken cancellationToken = default)
    {
        ExchangeBoard board = req.Instrument.Board is null
        ? null
            : conLink.Connector.ExchangeBoards.FirstOrDefault(x => x.Code == req.Instrument.Board.Code && (x.Exchange.Name == req.Instrument.Board.Exchange.Name || x.Exchange.CountryCode.ToString() == req.Instrument.Board.Exchange.CountryCode.ToString()));
        Security currentSec = conLink.Connector.Securities.FirstOrDefault(x => x.Name == req.Instrument.Name && x.Code == req.Instrument.Code && x.Board.Code == board.Code && x.Board.Exchange.Name == board.Exchange.Name && x.Board.Exchange.CountryCode == board.Exchange.CountryCode);
        if (currentSec is null)
            return Task.FromResult(ResponseBaseModel.CreateError($"Инструмент не найден: {req.Instrument}"));

        Portfolio selectedPortfolio = conLink.Connector.Portfolios.FirstOrDefault(x => x.ClientCode == req.Portfolio.ClientCode && x.Name == req.Portfolio.Name);
        if (selectedPortfolio is null)
            return Task.FromResult(ResponseBaseModel.CreateError($"Портфель не найден: {req.Portfolio}"));

        Order order = new()
        {
            Type = (OrderTypes)Enum.Parse(typeof(OrderTypes), Enum.GetName(req.OrderType)),
            Portfolio = selectedPortfolio,
            Volume = req.Volume,
            Price = req.Price,
            Security = currentSec,
            Side = (Sides)Enum.Parse(typeof(Sides), Enum.GetName(req.Side))
        };

        conLink.Connector.RegisterOrder(order);
        return Task.FromResult(ResponseBaseModel.CreateInfo("Заявка отправлена на регистрацию"));
    }


    void ValuesChangedHandle(Security instrument, IEnumerable<KeyValuePair<Level1Fields, object>> dataPayload, DateTimeOffset dtOffsetMaster, DateTimeOffset dtOffsetSlave)
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
        if (Instruments.Any(x => x.Code == security.Code) && (BoardsFilter is null || BoardsFilter.Count == 0 || BoardsFilter.Contains(new BoardStockSharpModel().Bind(security.Board))))
            BondList.Add(security);
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

    #region transit

    void SendRequest(string secCode, decimal price, decimal volume, Sides direction)
    {
        /*if (System.Windows.MessageBox.Show(
            "Do you really want to " + (direction == Sides.Buy ? "BUY" : "SELL") + " " + volume.ToString("#") +
            " " + secCode + " bonds @" + " " + price.ToString("#.##"), "Confirmation", MessageBoxButton.YesNo) ==
            MessageBoxResult.Yes)
        {
            Order _order = new()
            {
                Security = conLink.Connector.Securities.FirstOrDefault(s => (s.Code == secCode) && (s.Board == ExchangeBoard.MicexTqob)),
                Portfolio = conLink.Connector.Portfolios.FirstOrDefault(p => p.Name == PortName),
                Side = direction,
                Price = price,
                Volume = volume,
                Comment = "Manual",
                IsMarketMaker = OfzCodes.Contains(secCode),
                ClientCode = ClCode,
            };

            conLink.Connector.RegisterOrder(_order);
        }*/
    }


    #endregion

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