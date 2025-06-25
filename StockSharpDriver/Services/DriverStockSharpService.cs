////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Ecng.Collections;
using Ecng.Common;
using Microsoft.Extensions.Caching.Memory;
using SharedLib;
using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Fix.Quik.Lua;
using StockSharp.Messages;
using System.Net;
using System.Security;

namespace StockSharpDriver;

/// <summary>
/// DriverStockSharpService 
/// </summary>
public class DriverStockSharpService(
    ILogger<DriverStockSharpService> _logger,
    IManageStockSharpService manageRepo,
    IDataStockSharpService DataRepo,
    IParametersStorage storageRepo,
    IMemoryCache memoryCache,
    ConnectionLink conLink) : IDriverStockSharpService
{
    SecurityLookupMessage SecurityCriteriaCodeFilterLookup;
    Subscription SecurityCriteriaCodeFilterSubscription;

    readonly List<SecurityPosition>
        SBondPositionsList = [],
        SBondSizePositionsList = [],
        SBondSmallPositionsList = [];

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

    string SecurityCriteriaCodeFilterStockSharp;
    List<Subscription> DepthSubscriptions = [];

    decimal
        quoteSmallStrategyBidVolume = 2000,
        quoteSmallStrategyOfferVolume = 2000,
        quoteStrategyVolume = 1000,
        skipVolume = 2500,
        quoteSizeStrategyVolume = 2000;

    decimal bondPositionTraded,
        bondSizePositionTraded,
        bondSmallPositionTraded,
        bondOutOfRangePositionTraded;

    readonly List<Order> AllOrders = [];

    Dictionary<string, Order>
        _ordersForQuoteBuyReregister,
        _ordersForQuoteSellReregister;

    readonly Dictionary<Security, IOrderBookMessage> OderBookList = [];

    decimal
        lowLimit = 0.19m,
        highLimit = 0.25m;

    readonly decimal
       lowYieldLimit = 4m,
       highYieldLimit = 5m;

    BoardStockSharpModel Board;
    Portfolio SelectedPortfolio;

    readonly List<MyTrade> myTrades = [];

    readonly List<StrategyTradeStockSharpModel> StrategyTrades = [];
    readonly List<FixMessageAdapterModelDB> Adapters = [];

    readonly List<SBond> SBondList = [];

    readonly List<Security> AllSecurities = [];
    List<Security> SecuritiesBonds()
    {
        List<Security> res = [];

        if (StrategyTrades.Count == 0)
            return res;

        lock (AllSecurities)
        {
            lock (StrategyTrades)
            {
                foreach (Security security in AllSecurities)
                {
                    if (StrategyTrades.Any(x => x.Code == security.Code) && (Board is null || Board.Equals(new BoardStockSharpModel().Bind(security.Board))))
                        res.Add(security);
                }
            }
        }

        return res;
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> StartStrategy(StrategyStartRequestModel req, CancellationToken cancellationToken = default)
    {
        AboutConnectResponseModel _ac = await AboutConnection();
        if (_ac.ConnectionState != ConnectionStatesEnum.Connected)
            return ResponseBaseModel.CreateError($"{nameof(_ac.ConnectionState)}: {_ac.ConnectionState} ({_ac.ConnectionState.DescriptionInfo()})");

        ClearStrategy();

        if (req.Board is null)
            return ResponseBaseModel.CreateError("Board - not set");

        if (req.SelectedPortfolio is null)
            return ResponseBaseModel.CreateError("Portfolio - not set");

        Board = req.Board;
        SelectedPortfolio = conLink.Connector.Portfolios.FirstOrDefault(x => x.ClientCode == req.SelectedPortfolio.ClientCode);

        if (SelectedPortfolio is null)
        {
            ClearStrategy();
            return ResponseBaseModel.CreateError($"Portfolio #{req.SelectedPortfolio.ClientCode} - not found");
        }

        TPaginationResponseModel<InstrumentTradeStockSharpViewModel> resInstruments = await DataRepo.InstrumentsSelectAsync(new()
        {
            PageNum = 0,
            PageSize = int.MaxValue,
            FavoriteFilter = true,
        }, cancellationToken);

        if (resInstruments.Response is null || resInstruments.Response.Count == 0)
            return ResponseBaseModel.CreateError($"The instruments are not configured.");

        List<StrategyTradeStockSharpModel> dataParse = await ReadStrategies([.. resInstruments.Response.Select(x => x.Id)], cancellationToken);

        if (dataParse.Count == 0)
            return ResponseBaseModel.CreateError("Dashboard - not set");

        lock (StrategyTrades)
        {
            StrategyTrades.Clear();
            foreach (InstrumentTradeStockSharpViewModel instrument in resInstruments.Response)
            {
                int _fx = dataParse.FindIndex(x => x.Id == instrument.Id);
                if (_fx < 0)
                    return ResponseBaseModel.CreateError($"Instrument not set: {instrument}");

                if (dataParse[_fx].ValueOperation < 1)
                    return ResponseBaseModel.CreateError($"Value for instrument '{instrument}' incorrect");

                if (dataParse[_fx].BasePrice < 1)
                    return ResponseBaseModel.CreateError($"Price for instrument '{instrument}' incorrect");

                StrategyTrades.Add(dataParse[_fx]);
            }

            if (StrategyTrades is null || StrategyTrades.Count == 0)
                return ResponseBaseModel.CreateError("Instruments - is empty");
        }

        List<Security> bl = SecuritiesBonds();
        if (!bl.Any())
            return ResponseBaseModel.CreateError("BondList - not any");
        ResponseBaseModel response = new();
        bl.ForEach(securityHandleAction);
        void securityHandleAction(Security security)
        {
            StrategyTradeStockSharpModel[] tryFindStrategy = [.. StrategyTrades.Where(x => x.Code == security.Code)];
            string msg;
            int _cntBounds = SecuritiesBonds().Count;
            if (tryFindStrategy.Length == 0)
            {
                msg = $"strategy #{security.Code} not found in BondList ({_cntBounds} items)";
                response.AddError(msg);
                _logger.LogError(msg);
                return;
            }
            if (tryFindStrategy.Length != 1)
            {
                msg = $"strategy #{security.Code} DOUBLE`s ({tryFindStrategy.Length}) found in BondList ({_cntBounds} items)";
                response.AddError(msg);
                _logger.LogError(msg);
                return;
            }
            StrategyTradeStockSharpModel currentStrategy = tryFindStrategy[0];

            InstrumentTradeStockSharpViewModel[] tryFindInstrument = [.. resInstruments.Response.Where(x => x.Id == currentStrategy.Id)];
            if (tryFindInstrument.Length == 0)
            {
                msg = $"instrument #{currentStrategy.Id} not found in BondList ({_cntBounds} items)";
                response.AddError(msg);
                _logger.LogError(msg);
                return;
            }
            if (tryFindInstrument.Length != 1)
            {
                msg = $"instrument #{currentStrategy.Id} DOUBLE`s ({tryFindInstrument.Length}) found ({resInstruments.Response.Count} items)";
                response.AddError(msg);
                _logger.LogError(msg);
                return;
            }

            SBondPositionsList.Add(new SecurityPosition(security, "Quote", currentStrategy.LowLimit / 100, currentStrategy.HightLimit / 100, currentStrategy.ValueOperation, currentStrategy.ValueOperation, currentStrategy.ShiftPosition / 100));

            if (currentStrategy.IsSmall)
                SBondSmallPositionsList.Add(new SecurityPosition(security, "Small", (decimal)0.0301, (currentStrategy.LowLimit - (decimal)0.1) / 100, currentStrategy.SmallBidVolume, currentStrategy.SmallOfferVolume, currentStrategy.SmallOffset / 100));

            if (tryFindInstrument[0].Markers.Any(x => x.MarkerDescriptor == MarkersInstrumentStockSharpEnum.Illiquid))
                SBondSizePositionsList.Add(new SecurityPosition(security, "Size", (decimal)(currentStrategy.HightLimit + (decimal)0.1) / 100, (decimal)(currentStrategy.LowLimit + currentStrategy.HightLimit) / 100, quoteSizeStrategyVolume, quoteSizeStrategyVolume, 0m));
        }
        if (!response.Success())
        {
            ClearStrategy();
            return response;
        }

        lock (DepthSubscriptions)
        {
            DepthSubscriptions.Clear();
            bl.ForEach(MarketDepthRegister);
        }


        void MarketDepthRegister(Security security)
        {
            // Создаем подписку на стакан для выбранного инструмента
            Subscription depthSubscription = new(DataType.MarketDepth, security);

            // Обработка полученных стаканов
            conLink.Connector.OrderBookReceived += (sub, depth) =>
            {
                if (sub != depthSubscription)
                    return;

                // Обработка стакана
                Console.WriteLine($"Стакан: {depth.SecurityId}, Время: {depth.ServerTime}");
                Console.WriteLine($"Покупки (Bids): {depth.Bids.Length}, Продажи (Asks): {depth.Asks.Length}");
            };

            // Запуск подписки
            conLink.Connector.Subscribe(depthSubscription);

            DepthSubscriptions.Add(depthSubscription);
        }

        if (OfzCurve is null || OfzCurve.Length == 0)
            return ResponseBaseModel.CreateError("OfzCurve.Length == 0");

        _ordersForQuoteBuyReregister = [];
        _ordersForQuoteSellReregister = [];

        return ResponseBaseModel.CreateInfo("Ok");
    }

    /// <inheritdoc/>
    public Task<ResponseBaseModel> StopStrategy(StrategyStopRequestModel req, CancellationToken cancellationToken = default)
    {
        ClearStrategy();
        return Task.FromResult(ResponseBaseModel.CreateInfo("Ok"));
    }

    async Task<List<StrategyTradeStockSharpModel>> ReadStrategies(int?[] instrumentsIds, CancellationToken cancellationToken = default)
    {
        FindStorageBaseModel _findParametersQuery = new()
        {
            ApplicationName = GlobalStaticConstantsTransmission.TransmissionQueues.TradeInstrumentStrategyStockSharpReceive,
            PropertyName = GlobalStaticConstantsRoutes.Routes.DUMP_ACTION_NAME,
            OwnersPrimaryKeys = instrumentsIds
        };

        FundedParametersModel<StrategyTradeStockSharpModel>[] findStorageRows = await storageRepo.FindAsync<StrategyTradeStockSharpModel>(_findParametersQuery, cancellationToken);

        if (findStorageRows.Length == 0)
            return [];

        IQueryable<IGrouping<int?, FundedParametersModel<StrategyTradeStockSharpModel>>> _q = findStorageRows
            .GroupBy(x => x.OwnerPrimaryKey)
            .Where(x => x.Key.HasValue)
            .AsQueryable();

        return [.. _q.Select(x => x.OrderByDescending(x => x.CreatedAt).First().Payload)];
    }

    /// <inheritdoc/>
    public Task<ResponseBaseModel> ShiftCurve(ShiftCurveRequestModel req, CancellationToken cancellationToken = default)
    {
        if (OfzCurve.IsNull())
            return Task.FromResult(ResponseBaseModel.CreateWarning("OfzCurve is null"));

        _logger.LogWarning($"Curve changed: {req.YieldChange}");

        OfzCurve.BondList.ForEach(bnd =>
        {
            SBond SBnd = SBondList.FirstOrDefault(s => s.UnderlyingSecurity.Code == bnd.MicexCode);

            if (!SBnd.IsNull())
            {
                decimal yield = SBnd.GetYieldForPrice(OfzCurve.CurveDate, bnd.ModelPrice / 100);
                if (yield > 0) //Regular bonds
                {
                    bnd.ModelPrice = Math.Round(100 * SBnd.GetPriceFromYield(OfzCurve.CurveDate, yield + req.YieldChange / 10000, true), 2);
                }
            }
        });
        return Task.FromResult(ResponseBaseModel.CreateSuccess($"Ok - {nameof(ShiftCurve)} changed: {req.YieldChange}"));
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> LimitsStrategiesUpdate(LimitsStrategiesUpdateRequestModel req, CancellationToken cancellationToken = default)
    {
        static decimal Calculation(decimal L, OperatorsEnum op, decimal R)
        {
            return op switch
            {
                OperatorsEnum.Multiplication => L * R,
                OperatorsEnum.Dividing => L / R,
                OperatorsEnum.Plus => L + R,
                OperatorsEnum.Minus => L - R,
                _ => throw new NotImplementedException(),
            };
        }

        TPaginationResponseModel<InstrumentTradeStockSharpViewModel> resInstruments = await DataRepo.InstrumentsSelectAsync(new()
        {
            PageNum = 0,
            PageSize = int.MaxValue,
            FavoriteFilter = true,
        }, cancellationToken);

        if (resInstruments.Response is null || resInstruments.Response.Count == 0)
            return ResponseBaseModel.CreateError($"The instruments are not configured.");

        List<StrategyTradeStockSharpModel> dataParse = await ReadStrategies([.. resInstruments.Response.Select(x => x.Id)], cancellationToken);

        if (dataParse.Count == 0)
            return ResponseBaseModel.CreateError("Dashboard - not set");


        List<Security> sbs = SecuritiesBonds();
        sbs.ForEach(security =>
        {
            //string bndName = security.Code.Substring(2, 5);
            //DecimalUpDown decUpD = (DecimalUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "Price_" + bndName);

            //if (!decUpD.IsNull())
            //{
            //    IntegerUpDown Lowlimit = (IntegerUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "LowLimit_" + bndName);
            //    IntegerUpDown Highlimit = (IntegerUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "HighLimit_" + bndName);
            //    Lowlimit.Value = Lowlimit.Value / 2;
            //    Highlimit.Value = Highlimit.Value / 2;
            //}
        });

        lowLimit = Calculation(lowLimit, req.Operator, req.Operand);
        highLimit = Calculation(highLimit, req.Operator, req.Operand);

        return ResponseBaseModel.CreateInfo($"ok - `{nameof(LimitsStrategiesUpdate)}`");
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> Connect(ConnectRequestModel req, CancellationToken? cancellationToken = default)
    {
        Board = null;
        if (SecuritiesBonds().Any())
            return ResponseBaseModel.CreateError($"BondList is not empty!");

        TPaginationRequestStandardModel<AdaptersRequestModel> adReq = new()
        {
            PageSize = int.MaxValue,
            Payload = new() { OnlineOnly = true, }
        };

        TPaginationResponseModel<FixMessageAdapterModelDB> adRes = await manageRepo.AdaptersSelectAsync(adReq);
        if (adRes.Response is null || adRes.Response.Count == 0)
            return ResponseBaseModel.CreateError("adapters - is empty");

        lock (AllSecurities)
        {
            AllSecurities.Clear();
        }

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

        RegisterEvents();
        //
        SecurityCriteriaCodeFilterStockSharp = await storageRepo.ReadAsync<string>(GlobalStaticCloudStorageMetadata.SecuritiesCriteriaCodeFilterStockSharp);
        if (!string.IsNullOrWhiteSpace(SecurityCriteriaCodeFilterStockSharp))
        {
            SecurityCriteriaCodeFilterLookup = new()
            {
                SecurityId = new SecurityId
                {
                    SecurityCode = SecurityCriteriaCodeFilterStockSharp.Trim(),
                },
                TransactionId = conLink.Connector.TransactionIdGenerator.GetNextId()
            };
            SecurityCriteriaCodeFilterSubscription = new(SecurityCriteriaCodeFilterLookup);
            conLink.Connector.Subscribe(SecurityCriteriaCodeFilterSubscription);
        }

        List<FixMessageAdapterModelDB> adapters = adRes.Response;

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
                            Address = x.Address.To<EndPoint>(),
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
        {
            res.AddError("can`t connect");
            return res;
        }

        await conLink.Connector.ConnectAsync(cancellationToken ?? CancellationToken.None);
        res.AddInfo($"connection: {conLink.Connector.ConnectionState}");
        return res;
    }

    /// <inheritdoc/>
    public Task<ResponseBaseModel> Disconnect(CancellationToken? cancellationToken = default)
    {
        ClearStrategy();
        conLink.Connector.CancelOrders();
        foreach (Subscription sub in conLink.Connector.Subscriptions)
        {
            conLink.Connector.UnSubscribe(sub);
            _logger.LogInformation($"{nameof(Connector.UnSubscribe)} > {sub.GetType().FullName}");
        }

        SecurityCriteriaCodeFilterStockSharp = "";
        SecurityCriteriaCodeFilterLookup = null;

        UnregisterEvents();
        conLink.Connector.Disconnect();

        lock (AllSecurities)
            AllSecurities.Clear();
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
            StrategyStarted = Board is not null && StrategyTrades is not null && StrategyTrades.Count != 0,
            LowLimit = lowLimit,
            HighLimit = highLimit,
            SecurityCriteriaCodeFilterStockSharp = SecurityCriteriaCodeFilterStockSharp,
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
            Side = (Sides)Enum.Parse(typeof(Sides), Enum.GetName(req.Side)),
            IsManual = req.IsManual,
            IsMarketMaker = req.IsMarketMaker,
            IsSystem = req.IsSystem,
            Comment = req.Comment,
        };

        conLink.Connector.RegisterOrder(order);
        return Task.FromResult(ResponseBaseModel.CreateInfo("Заявка отправлена на регистрацию"));
    }

    /// <inheritdoc/>
    public Task<OrderRegisterRequestResponseModel> OrderRegisterRequestAsync(OrderRegisterRequestModel req, CancellationToken cancellationToken = default)
    {
        OrderRegisterRequestResponseModel res = new();
        if (string.IsNullOrWhiteSpace(req.ConfirmRequestToken))
        {
            req.ConfirmRequestToken = Guid.NewGuid().ToString();
            memoryCache.Set(req.ConfirmRequestToken, req, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(10)));
            res.AddWarning("Запрос требуется подтвердить!");
            return Task.FromResult(res);
        }
        else
        {
            memoryCache.TryGetValue(req.ConfirmRequestToken, out OrderRegisterRequestModel savedReq);
            if (savedReq is null)
            {
                req.ConfirmRequestToken = Guid.NewGuid().ToString();
                memoryCache.Set(req.ConfirmRequestToken, req, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(30)));
                res.AddAlert("Подтверждение просрочено. Повторите попытку! ");
                return Task.FromResult(res);
            }
            else
                memoryCache.Remove(req.ConfirmRequestToken);
        }


        Order _order = new()
        {
            Price = req.Price,
            ClientCode = req.ClientCode,
            Comment = "Manual",
            Volume = req.Volume,
            Side = req.Direction == SidesEnum.Buy ? Sides.Buy : Sides.Sell,
            //Security = conLink.Connector.Securities.FirstOrDefault(s => (s.Code == secCode) && (s.Board == ExchangeBoard.MicexTqob)),
            //Portfolio = conLink.Connector.Portfolios.FirstOrDefault(p => p.Name == PortName),
            //IsMarketMaker = OfzCodes.Contains(req.SecCode),
        };

        conLink.Connector.RegisterOrder(_order);

        throw new NotImplementedException();
    }

    #region events
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
        lock (AllSecurities)
        {
            int _fi = AllSecurities.FindIndex(x => x.Id == security.Id);

            if (_fi == -1)
                AllSecurities.Add(security);
            else
                AllSecurities[_fi] = security;
        }
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
        lock (AllOrders)
        {
            int _i = AllOrders.FindIndex(x => x.StringId.Equals(oreder.StringId));

            if (_i == -1)
                AllOrders.Add(oreder);
            else
                AllOrders[_i] = oreder;
        }
    }

    void PositionReceivedHandle(Subscription subscription, Position pos)
    {
        //_logger.LogWarning($"Call > `{nameof(PositionReceivedHandle)}`: {JsonConvert.SerializeObject(pos)}");
    }
    void OwnTradeReceivedHandle(Subscription subscription, MyTrade tr)
    {
        lock (myTrades)
            myTrades.Add(tr);

        //_logger.LogWarning($"Call > `{nameof(OwnTradeReceivedHandle)}`: {JsonConvert.SerializeObject(tr)}");
    }
    void OrderBookReceivedHandle(Subscription subscription, IOrderBookMessage orderBM)
    {
        Security sec = conLink.Connector.Securities.FirstOrDefault(s => s.ToSecurityId() == orderBM.SecurityId);
        lock (OderBookList)
        {
            if (SecuritiesBonds().Contains(sec))
            {
                if (OderBookList.ContainsKey(sec))
                    OderBookList[sec] = orderBM;
                else
                    OderBookList.Add(sec, orderBM);
            }
        }
    }

    private void OrderBookReceivedConnector2(Subscription subscription, IOrderBookMessage depth)
    {
        decimal ofrVolume;

        Security sec = conLink.Connector.Securities.FirstOrDefault(s => s.ToSecurityId() == depth.SecurityId);

        //..................................
        // For bonds
        //...................................

        SecurityPosition SbPos = SBondPositionsList.FirstOrDefault(sp => sp.Sec.Equals(sec));

        if (SbPos.IsNull())
            return;

        if (SbPos.LowLimit > SbPos.HighLimit)
            return;

        IEnumerable<Order> Orders = [.. AllOrders
            .Where(s => (s.State == OrderStates.Active) && (s.Security.Code == sec.Code) && (!s.Comment.IsNull()) && s.Comment.ContainsIgnoreCase("Ofr"))];

        if (!Orders.IsEmpty() && (Orders.Count() > 4))
            return;

        QuoteChange? bBid = depth.GetBestBid();
        QuoteChange? bAsk = depth.GetBestAsk();

        if (bBid.IsNull() || bAsk.IsNull())
            return;

        if (bBid.Value.Price > OfzCurve.GetNode(sec).ModelPrice + SbPos.LowLimit + SbPos.HighLimit)
        {
            ofrVolume = 20000;
            if (bBid.Value.Price > OfzCurve.GetNode(sec).ModelPrice + 2 * SbPos.HighLimit)
                ofrVolume = 30000;
            if (bBid.Value.Price > OfzCurve.GetNode(sec).ModelPrice + 3 * SbPos.HighLimit)
                ofrVolume = 50000;

            if (bBid.Value.Volume < ofrVolume)
                ofrVolume = bBid.Value.Volume;

            DeleteAllQuotesByStrategy("Quote");
            DeleteAllQuotesByStrategy("Size");

            //Order ord = new()
            //{
            //    Security = sec,
            //    Portfolio = SelectedPortfolio,
            //    Price = bBid.Value.Price,
            //    Side = Sides.Sell,
            //    Comment = "OfRStrategy",
            //    IsMarketMaker = OfzCodes.Contains(sec.Code),
            //    Volume = ofrVolume,
            //    ClientCode = ClCode,
            //};

            //    connector.RegisterOrder(ord);
            //    connector.AddWarningLog("Order sell registered for OfRStrategy: ins ={0}, price = {1}, volume = {2}", sec, ord.Price, ord.Volume);

            //    // New logic???
            //    this.GuiAsync(() => System.Windows.MessageBox.Show(this, "OfR Detected! " + sec.Code));
            //    connector.OrderBookReceived -= OrderBookReceivedConnector2;
            //    //
        }
        else if (bAsk.Value.Price < OfzCurve.GetNode(sec).ModelPrice - SbPos.LowLimit - SbPos.HighLimit)
        {
            //    ofrVolume = 20000;
            //    if (bAsk.Value.Price < OfzCurve.GetNode(sec).ModelPrice - 2 * SbPos.HighLimit)
            //        ofrVolume = 30000;
            //    if (bAsk.Value.Price < OfzCurve.GetNode(sec).ModelPrice - 3 * SbPos.HighLimit)
            //        ofrVolume = 50000;

            //    if (bAsk.Value.Volume < ofrVolume)
            //        ofrVolume = bAsk.Value.Volume;

            //    DeleteAllQuotesByStrategy("Quote");
            //    DeleteAllQuotesByStrategy("Size");

            //    Order ord = new()
            //    {
            //        Security = sec,
            //        Portfolio = MyPortf,
            //        Price = bAsk.Value.Price,
            //        Side = Sides.Buy,
            //        Comment = "OfRStrategy",
            //        IsMarketMaker = OfzCodes.Contains(sec.Code),
            //        Volume = ofrVolume,
            //        ClientCode = ClCode,
            //    };

            //    connector.RegisterOrder(ord);
            //    connector.AddWarningLog("Order buy registered for OfRStrategy: ins ={0}, price = {1}, volume = {2}", sec, ord.Price, ord.Volume);

            //    // New logic???
            //    this.GuiAsync(() => System.Windows.MessageBox.Show(this, "OfR Detected! " + sec.Code));
            //    connector.OrderBookReceived -= OrderBookReceivedConnector2;
        }
    }

    #region todo
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
        UnregisterEvents();
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
        _logger.LogWarning($"Call > `{nameof(CandleReceivedHandle)}`");
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

    void LookupSecuritiesResultHandle(SecurityLookupMessage slm, IEnumerable<Security> securities, Exception ex)
    {
        if (ex is not null)
            _logger.LogError(ex, $"Call > `{nameof(conLink.Connector.LookupSecuritiesResult)}`");
        else
        {
            _logger.LogInformation($"Call > `{nameof(conLink.Connector.LookupSecuritiesResult)}`");

            lock (AllSecurities)
            {
                foreach (Security _sec in securities)
                {
                    int _fi = AllSecurities.FindIndex(x => x.Id == _sec.Id);

                    if (_fi == -1)
                        AllSecurities.Add(_sec);
                    else
                        AllSecurities[_fi] = _sec;
                }
            }
        }
    }

    #region Exception`s
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
    #endregion

    void DeleteAllQuotesByStrategy(string strategy)
    {
        IEnumerable<Order> orders = AllOrders.Where(s => s.State == OrderStates.Active);

        if (string.IsNullOrEmpty(strategy))
        {
            foreach (Order order in orders)
            {
                conLink.Connector.CancelOrder(order);
                //conLink.Connector.AddWarningLog("Order cancelled: ins ={0}, price = {1}, volume = {2}", order.Security, order.Price, order.Volume);
            }
        }
        else
        {
            foreach (Order order in orders)
            {
                if ((!string.IsNullOrEmpty(order.Comment)) && order.Comment.ContainsIgnoreCase(strategy))
                    conLink.Connector.CancelOrder(order);

                //connector.AddWarningLog("Order cancelled: ins ={0}, price = {1}, volume = {2}", order.Security, order.Price, order.Volume);
            }
        }
    }

    void ClearStrategy()
    {
        lock (DepthSubscriptions)
        {
            DepthSubscriptions.ForEach(conLink.Connector.UnSubscribe);
            DepthSubscriptions.Clear();
        }

        if (SecurityCriteriaCodeFilterSubscription is not null)
            conLink.Connector.UnSubscribe(SecurityCriteriaCodeFilterSubscription);
        SecurityCriteriaCodeFilterSubscription = null;

        Board = null;
        SelectedPortfolio = null;

        lock (StrategyTrades)
            StrategyTrades.Clear();

        lock (SBondPositionsList)
            SBondPositionsList.Clear();

        lock (SBondSizePositionsList)
            SBondSizePositionsList.Clear();

        lock (SBondSmallPositionsList)
            SBondSmallPositionsList.Clear();

        lock (OderBookList)
            OderBookList.Clear();

        lock (AllOrders)
            AllOrders.Clear();

        bondPositionTraded = 0;
        bondSizePositionTraded = 0;
        bondSmallPositionTraded = 0;
        bondOutOfRangePositionTraded = 0;

        lowLimit = 0.19m;
        highLimit = 0.25m;
    }

    /// <inheritdoc/>
    public Task<ResponseBaseModel> PingAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ResponseBaseModel.CreateSuccess($"Ok - {nameof(DriverStockSharpService)}"));
    }

    public Task<ResponseBaseModel> Terminate(CancellationToken? cancellationToken = null)
    {
        UnregisterEvents();
        conLink.Connector.Dispose();
        conLink.Connector = new();
        RegisterEvents();
        return Task.FromResult(ResponseBaseModel.CreateSuccess("Connection terminated"));
    }


    void UnregisterEvents()
    {
        conLink.Unsubscribe();

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
        conLink.Subscribe();

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