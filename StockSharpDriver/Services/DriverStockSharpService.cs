////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Ecng.Collections;
using Ecng.Common;
using Ecng.Logging;
using Newtonsoft.Json;
using SharedLib;
using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Fix.Quik.Lua;
using StockSharp.Messages;
using System.Net;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;

namespace StockSharpDriver;

/// <summary>
/// DriverStockSharpService 
/// </summary>
public class DriverStockSharpService(
                ILogger<DriverStockSharpService> _logger,
                IManageStockSharpService manageRepo,
                IDataStockSharpService dataRepo,
                IParametersStorage storageRepo,
                IEventsStockSharp eventTrans,
                ConnectionLink conLink) : IDriverStockSharpService
{
    #region prop`s
    Curve? CurveCurrent;
    Portfolio? PortfolioCurrent;
    List<BoardStockSharpViewModel>? BoardsCurrent;

    Subscription? SecurityCriteriaCodeFilterSubscription;
    readonly List<SecurityLookupMessage> SecuritiesCriteriaCodesFilterLookups = [];

    readonly List<DashboardTradeStockSharpModel> StrategyTrades = [];
    readonly List<Security> AllSecurities = [];
    readonly List<MyTrade> myTrades = [];
    readonly List<SBond> SBondList = [];
    readonly List<long?> TradesList = [];
    readonly List<Order> AllOrders = [];
    readonly List<Subscription> MarketDepthSubscriptions = [];
    readonly List<SecurityPosition>
        SBondPositionsList = [],
        SBondSizePositionsList = [],
        SBondSmallPositionsList = [];

    readonly Dictionary<string, Order>
        _ordersForQuoteBuyReregister = [],
        _ordersForQuoteSellReregister = [];

    readonly Dictionary<Security, IOrderBookMessage> OderBookList = [];
    readonly FileSystemWatcher fileWatcher = new();

    #region LastConnectedAt
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
    #endregion

    string?
        ProgramDataPath,
        ClientCodeStockSharp,
        SecurityCriteriaCodeFilter,
        BoardCriteriaCodeFilter;

    decimal
       quoteSmallStrategyBidVolume = 2000,
       quoteSmallStrategyOfferVolume = 2000,
       quoteStrategyVolume = 1000,
       quoteSizeStrategyVolume = 2000;

    decimal
        lowLimit = 0.19m,
        highLimit = 0.25m;

    readonly decimal
       lowYieldLimit = 4m,
       highYieldLimit = 5m;

    bool StrategyStarted => BoardsCurrent is not null && BoardsCurrent.Count != 0 && StrategyTrades is not null && StrategyTrades.Count != 0;

    List<Security> SecuritiesBonds(bool ofStrategy)
    {
        List<Security> res = [];

        lock (AllSecurities)
        {
            lock (StrategyTrades)
            {
                if (ofStrategy && StrategyTrades.Count == 0)
                    return res;

                foreach (Security security in AllSecurities)
                {
                    BoardStockSharpModel _bo = new BoardStockSharpModel().Bind(security.Board);
                    if (ofStrategy)
                    {
                        if (StrategyTrades.Any(x => x.Code == security.Code) && (BoardsCurrent is null || BoardsCurrent.Count == 0 || BoardsCurrent.Any(x => x.Equals(_bo))))
                            res.Add(security);
                    }
                    else
                    {
                        if (BoardsCurrent is null || BoardsCurrent.Count == 0 || BoardsCurrent.Any(x => x.Equals(_bo)))
                            res.Add(security);
                    }
                }
            }
        }

        return res;
    }
    #endregion

    /// <inheritdoc/>
    public async Task<ResponseSimpleModel> InitialLoad(InitialLoadRequestModel req, CancellationToken cancellationToken = default)
    {
        ResponseSimpleModel res = new();

        if (conLink.Connector.ConnectionState != Ecng.ComponentModel.ConnectionStates.Connected)
        {
            res.AddError($"Connection: {Enum.GetName(conLink.Connector.ConnectionState)}");
            return res;
        }

        if (StrategyStarted)
        {
            res.AddError($"{nameof(StrategyStarted)}! Stop strategy for initial load");
            return res;
        }

        TResponseModel<List<InstrumentTradeStockSharpViewModel>> resInstruments = await dataRepo.ReadTradeInstrumentsAsync(cancellationToken);

        if (resInstruments.Response is null || resInstruments.Response.Count == 0)
        {
            res.AddError($"The instruments are not configured.");
            return res;
        }

        List<DashboardTradeStockSharpModel> dataParse = await ReadDashboard([.. resInstruments.Response.Select(x => x.Id)], cancellationToken);

        List<Task> tasks = [
                  Task.Run(async () => quoteStrategyVolume = await storageRepo.ReadAsync<decimal>(GlobalStaticCloudStorageMetadata.QuoteStrategyVolume, 1000, cancellationToken)),
                  Task.Run(async () => quoteSizeStrategyVolume = await storageRepo.ReadAsync<decimal>(GlobalStaticCloudStorageMetadata.QuoteSizeStrategyVolume, 2000, cancellationToken)),
                  Task.Run(async () => quoteSmallStrategyBidVolume = await storageRepo.ReadAsync<decimal>(GlobalStaticCloudStorageMetadata.QuoteSmallStrategyBidVolume, 2000, cancellationToken)),
                  Task.Run(async () => quoteSmallStrategyBidVolume = await storageRepo.ReadAsync<decimal>(GlobalStaticCloudStorageMetadata.QuoteSmallStrategyBidVolume, 2000, cancellationToken)),
                  Task.Run(async () => {
                    int[]? _boardsFilter = await storageRepo.ReadAsync<int[]>(GlobalStaticCloudStorageMetadata.BoardsDashboard, null, cancellationToken);
                    if(_boardsFilter is not null && _boardsFilter.Length != 0)
                    {
                        TResponseModel<List<BoardStockSharpViewModel>> boardDb = await dataRepo.GetBoardsAsync(_boardsFilter);
                        BoardsCurrent = boardDb.Response;
                    }
                  }, cancellationToken),
        ];

        if (!StrategyStarted || string.IsNullOrWhiteSpace(ProgramDataPath))
            tasks.Add(Task.Run(async () => ProgramDataPath = await storageRepo.ReadAsync<string>(GlobalStaticCloudStorageMetadata.ProgramDataPathStockSharp, null, cancellationToken)));

        await Task.WhenAll(tasks);

        if (string.IsNullOrWhiteSpace(ProgramDataPath))
        {
            res.AddError($"set: {nameof(ProgramDataPath)} for load Curve data!");
            return res;
        }

        if (!Directory.Exists(ProgramDataPath))
        {
            res.AddError($"Directory ({nameof(ProgramDataPath)}): {ProgramDataPath} not Exists");
            return res;
        }

        if (BoardsCurrent is null || BoardsCurrent.Count == 0)
        {
            res.AddError($"Board is null");
            return res;
        }

        SBond? SBnd;
        DateTime curDate;
        decimal BndPrice = 0;

        List<Security> currBonds = SecuritiesBonds(false);
        if (!currBonds.Any())
        {
            res.AddError($"!{nameof(SecuritiesBonds)}().Any()");
            return res;
        }
        DateTime _gnvd = MyHelper.GetNextWorkingDay(DateTime.Today, 1, Path.Combine(ProgramDataPath, "RedArrowData.db"));
        CurveCurrent = new Curve(_gnvd);
        res.Response = CurveCurrent.GetCurveFromDb(Path.Combine(ProgramDataPath, "RedArrowData.db"), conLink.Connector, BoardsCurrent, req.BigPriceDifferences, ref eventTrans);
        if (!string.IsNullOrWhiteSpace(res.Response))
            return res;

        if (CurveCurrent.BondList.Count == 0)
        {
            res.AddError("OfzCurve.Length == 0");
            return res;
        }

        curDate = MyHelper.GetNextWorkingDay(DateTime.Today, 1, ProgramDataPath + "RedArrowData.db");
        List<Task> tasksMaster = [], tasksSlave = [];
        currBonds.ForEach(security =>
        {
            InstrumentTradeStockSharpModel _sec = new InstrumentTradeStockSharpModel().Bind(security);
            InstrumentTradeStockSharpViewModel _instrument = resInstruments.Response.First(x => x.IdRemote == _sec.IdRemote);
            DashboardTradeStockSharpModel tradeDashboard = dataParse.FirstOrDefault(x => x.Id.Equals(_instrument.Id)) ?? new() { Id = _instrument.Id };

            SBnd = CurveCurrent.GetNode(_sec);

            if (SBnd is not null)
                BndPrice = SBnd.ModelPrice;

            tradeDashboard.BasePrice = BndPrice;
            tradeDashboard.SmallBidVolume = (long)quoteSmallStrategyBidVolume;
            tradeDashboard.SmallOfferVolume = (long)quoteSmallStrategyOfferVolume;
            tradeDashboard.WorkingVolume = (long)quoteStrategyVolume;
            tradeDashboard.SmallOffset = 0;
            tradeDashboard.Offset = 0;

            SBnd = SBondList.FirstOrDefault(s => s.UnderlyingSecurity.Code == security.Code);

            if (SBnd is not null)
            {
                decimal yield = SBnd.GetYieldForPrice(curDate, BndPrice / 100);
                if (yield > 0) //Regular bonds
                {
                    tradeDashboard.LowLimit = (int)((BndPrice / 100 - SBnd.GetPriceFromYield(curDate, yield + lowYieldLimit / 10000, true)) * 10000);

                    if (tradeDashboard.LowLimit < 9)
                        tradeDashboard.LowLimit = 9;
                    if (tradeDashboard.LowLimit > lowLimit * 100)
                        tradeDashboard.LowLimit = (int)(lowLimit * 100);

                    tradeDashboard.HightLimit = (int)((BndPrice / 100 - SBnd.GetPriceFromYield(curDate, yield + highYieldLimit / 10000, true)) * 10000);

                    if (tradeDashboard.HightLimit < 11)
                        tradeDashboard.HightLimit = 11;
                    if (tradeDashboard.HightLimit > highLimit * 100)
                        tradeDashboard.HightLimit = (int)(highLimit * 100);

                    if ((SBnd.Maturity - curDate).Days < 400)
                        tradeDashboard.WorkingVolume = quoteStrategyVolume;
                    else if ((SBnd.Maturity - curDate).Days < 1100)
                        tradeDashboard.WorkingVolume = quoteStrategyVolume;
                    else if ((SBnd.Maturity - curDate).Days < 1500)
                        tradeDashboard.WorkingVolume = quoteStrategyVolume;
                    else
                        tradeDashboard.WorkingVolume = quoteStrategyVolume;
                }
                else
                {
                    if (_instrument.Markers!.Any(x => x.MarkerDescriptor == (int)MarkersInstrumentStockSharpEnum.Illiquid))
                    {
                        if ((SBnd.Maturity - curDate).Days < 300)
                        {
                            tradeDashboard.WorkingVolume = 1000;
                            tradeDashboard.LowLimit = (int)(lowLimit * 100);
                            tradeDashboard.HightLimit = (int)(highLimit * 100);
                        }
                        else
                        {
                            tradeDashboard.WorkingVolume = 1000;
                            tradeDashboard.LowLimit = (int)(lowLimit * 2 * 100);
                            tradeDashboard.HightLimit = (int)(highLimit * 2 * 100);
                        }
                    }
                    else
                    {
                        if ((SBnd.Maturity - curDate).Days < 500)
                        {
                            tradeDashboard.WorkingVolume = 2000;
                            tradeDashboard.LowLimit = (int)(lowLimit / 2 * 100);
                            tradeDashboard.HightLimit = (int)(highLimit / 2 * 100);
                        }
                        else if ((SBnd.Maturity - curDate).Days < 2000)
                        {
                            tradeDashboard.WorkingVolume = 2000;
                            tradeDashboard.LowLimit = (int)(lowLimit / 1.5m * 100);
                            tradeDashboard.HightLimit = (int)(highLimit / 1.5m * 100);
                        }
                        else
                        {
                            tradeDashboard.WorkingVolume = 2000;
                            tradeDashboard.LowLimit = (int)(lowLimit / 1.5m * 100);
                            tradeDashboard.HightLimit = (int)(highLimit / 1.5m * 100);
                        }
                    }
                }
            }
            else
            {
                tradeDashboard.LowLimit = (int)(lowLimit * 100);
                tradeDashboard.HightLimit = (int)(highLimit * 100);
            }

            tasksMaster.Add(Task.Run(async () => { await storageRepo.SaveAsync(tradeDashboard, GlobalStaticCloudStorageMetadata.TradeInstrumentStrategyStockSharp(tradeDashboard.Id), true); }));
            tasksSlave.Add(Task.Run(async () => { await eventTrans.DashboardTradeUpdate(tradeDashboard); }));
        });

        if (tasksMaster.Count != 0)
        {
            res.AddInfo($"Updated items (strategies): {tasksMaster.Count}");
            await Task.WhenAll(tasksMaster);
            await Task.WhenAll(tasksSlave);
        }
        await eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState)!)
        }, cancellationToken);
        res.AddInfo("Data loaded!!!");
        return res;
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

        TResponseModel<List<InstrumentTradeStockSharpViewModel>> resInstruments = await dataRepo.ReadTradeInstrumentsAsync(cancellationToken);

        if (resInstruments.Response is null || resInstruments.Response.Count == 0)
            return ResponseBaseModel.CreateError($"The instruments are not configured.");

        List<DashboardTradeStockSharpModel> dataParse = await ReadDashboard([.. resInstruments.Response.Select(x => x.Id)], cancellationToken);

        if (dataParse.Count == 0)
            return ResponseBaseModel.CreateError("Dashboard - not set");

        ResponseBaseModel res = new();
        List<Task> tasksMaster = [], tasksSlave = [];
        List<Security> sbs = SecuritiesBonds(true);
        sbs.ForEach(security =>
        {
            InstrumentTradeStockSharpModel _sec = new InstrumentTradeStockSharpModel().Bind(security);
            InstrumentTradeStockSharpViewModel _instrument = resInstruments.Response.First(x => x.IdRemote == _sec.IdRemote);
            DashboardTradeStockSharpModel tradeDashboard = dataParse.FirstOrDefault(x => x.Id.Equals(_instrument.Id)) ?? new() { Id = _instrument.Id };

            tradeDashboard.LowLimit = Calculation(tradeDashboard.LowLimit, req.Operator, req.Operand);
            tradeDashboard.HightLimit = Calculation(tradeDashboard.HightLimit, req.Operator, req.Operand);

            tasksMaster.Add(Task.Run(async () => { await storageRepo.SaveAsync(tradeDashboard, GlobalStaticCloudStorageMetadata.TradeInstrumentStrategyStockSharp(tradeDashboard.Id), true); }));
            tasksSlave.Add(Task.Run(async () => { await eventTrans.DashboardTradeUpdate(tradeDashboard); }));
        });

        if (tasksMaster.Count != 0)
        {
            res.AddInfo($"Updated items (strategies): {tasksMaster.Count}");
            await Task.WhenAll(tasksMaster);
            await Task.WhenAll(tasksSlave);
        }

        lowLimit = Calculation(lowLimit, req.Operator, req.Operand);
        highLimit = Calculation(highLimit, req.Operator, req.Operand);

        await eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState)!)
        }, cancellationToken);

        return ResponseBaseModel.CreateInfo($"ok - `{nameof(LimitsStrategiesUpdate)}`");
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> StartStrategy(StrategyStartRequestModel req, CancellationToken cancellationToken = default)
    {
        AboutConnectResponseModel _ac = await AboutConnection(cancellationToken);
        if (_ac.ConnectionState != ConnectionStatesEnum.Connected)
            return ResponseBaseModel.CreateError($"{nameof(_ac.ConnectionState)}: {_ac.ConnectionState} ({_ac.ConnectionState?.DescriptionInfo()})");

        await ClearStrategy(cancellationToken);

        ProgramDataPath = await storageRepo.ReadAsync<string>(GlobalStaticCloudStorageMetadata.ProgramDataPathStockSharp, null, cancellationToken);
        if (string.IsNullOrWhiteSpace(ProgramDataPath))
            return ResponseBaseModel.CreateError($"{nameof(ProgramDataPath)} - not set");

        if (!Directory.Exists(ProgramDataPath))
            return ResponseBaseModel.CreateError($"Directory [{nameof(ProgramDataPath)}] - not exists");

        if (BoardsCurrent is null || BoardsCurrent.Count == 0)
            return ResponseBaseModel.CreateError("Board - not set");

        if (req.SelectedPortfolio is null)
            return ResponseBaseModel.CreateError("Portfolio - not set");

        PortfolioCurrent = conLink.Connector.Portfolios.FirstOrDefault(x => x.ClientCode == req.SelectedPortfolio.ClientCode);

        if (PortfolioCurrent is null)
        {
            await ClearStrategy(cancellationToken);
            return ResponseBaseModel.CreateError($"Portfolio #{req.SelectedPortfolio.ClientCode} - not found");
        }
        TResponseModel<List<InstrumentTradeStockSharpViewModel>> resInstruments = await dataRepo.ReadTradeInstrumentsAsync(cancellationToken);

        if (resInstruments.Response is null || resInstruments.Response.Count == 0)
            return ResponseBaseModel.CreateError($"The instruments are not configured.");

        List<DashboardTradeStockSharpModel> dataParse = await ReadDashboard([.. resInstruments.Response.Select(x => x.Id)], cancellationToken);

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

        List<Security> bl = SecuritiesBonds(true);
        if (!bl.Any())
            return ResponseBaseModel.CreateError("BondList - not any");
        ResponseBaseModel response = new();

        void securityHandleAction(Security security)
        {
            DashboardTradeStockSharpModel[] tryFindStrategy = [.. StrategyTrades.Where(x => x.Code == security.Code)];
            string msg;

            if (tryFindStrategy.Length == 0)
            {
                msg = $"strategy #{security.Code} not found in BondList ({bl.Count} items)";
                response.AddError(msg);
                _logger.LogError(msg);
                return;
            }
            if (tryFindStrategy.Length != 1)
            {
                msg = $"strategy #{security.Code} DOUBLE`s ({tryFindStrategy.Length}) found in BondList ({bl.Count} items)";
                response.AddError(msg);
                _logger.LogError(msg);
                return;
            }
            DashboardTradeStockSharpModel currentStrategy = tryFindStrategy[0];

            InstrumentTradeStockSharpViewModel[] tryFindInstrument = [.. resInstruments.Response.Where(x => x.Id == currentStrategy.Id)];
            if (tryFindInstrument.Length == 0)
            {
                msg = $"instrument #{currentStrategy.Id} not found in BondList ({bl.Count} items)";
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
            InstrumentTradeStockSharpModel _sec = new InstrumentTradeStockSharpModel().Bind(security);
            SBondPositionsList.Add(new SecurityPosition(_sec, "Quote", currentStrategy.LowLimit / 100, currentStrategy.HightLimit / 100, currentStrategy.ValueOperation, currentStrategy.ValueOperation, currentStrategy.Offset / 100));

            if (currentStrategy.IsSmall)
                SBondSmallPositionsList.Add(new SecurityPosition(_sec, "Small", (decimal)0.0301, (currentStrategy.LowLimit - (decimal)0.1) / 100, currentStrategy.SmallBidVolume, currentStrategy.SmallOfferVolume, currentStrategy.SmallOffset / 100));

            if (tryFindInstrument[0].Markers!.Any(x => x.MarkerDescriptor == (int)MarkersInstrumentStockSharpEnum.Illiquid))
                SBondSizePositionsList.Add(new SecurityPosition(_sec, "Size", (currentStrategy.HightLimit + (decimal)0.1) / 100, (currentStrategy.LowLimit + currentStrategy.HightLimit) / 100, quoteSizeStrategyVolume, quoteSizeStrategyVolume, 0m));
        }
        bl.ForEach(securityHandleAction);

        if (!response.Success())
        {
            await ClearStrategy(cancellationToken);
            return response;
        }

        lock (MarketDepthSubscriptions)
        {
            MarketDepthSubscriptions.Clear();
            bl.ForEach(RegisterMarketDepth);
        }
        void RegisterMarketDepth(Security security)
        {
            Subscription depthSubscription = new(DataType.MarketDepth, security);
            conLink.Connector.Subscribe(depthSubscription);
            MarketDepthSubscriptions.Add(depthSubscription);
        }
        conLink.Connector.OrderBookReceived += MarketDepthOrderBookHandle;

        if (CurveCurrent is null || CurveCurrent.BondList.Count == 0)
        {
            await ClearStrategy(cancellationToken);
            return ResponseBaseModel.CreateError("OfzCurve.Length == 0");
        }

        lock (_ordersForQuoteBuyReregister)
            _ordersForQuoteBuyReregister.Clear();
        lock (_ordersForQuoteSellReregister)
            _ordersForQuoteSellReregister.Clear();

        fileWatcher.Path = ProgramDataPath;
        fileWatcher.NotifyFilter = NotifyFilters.LastWrite;
        fileWatcher.Filter = "RedArrowData.db";
        fileWatcher.Changed += OnDatabaseChanged;
        fileWatcher.EnableRaisingEvents = true;

        return ResponseBaseModel.CreateInfo("Ok");
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> StopStrategy(StrategyStopRequestModel req, CancellationToken cancellationToken = default)
    {
        await ClearStrategy(cancellationToken);
        fileWatcher.Changed -= OnDatabaseChanged;
        return ResponseBaseModel.CreateInfo("Ok");
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> ResetStrategy(ResetStrategyRequestModel req, CancellationToken cancellationToken = default)
    {
        List<Security> currentSecurities = SecuritiesBonds(true);
        TResponseModel<List<InstrumentTradeStockSharpViewModel>> readInstrument = await dataRepo.GetInstrumentsAsync([req.InstrumentId], cancellationToken);

        InstrumentTradeStockSharpViewModel? currInstrument = readInstrument.Response?.FirstOrDefault(x => x.Id == req.InstrumentId);
        if (currInstrument is null)
            return ResponseBaseModel.CreateError("current instrument not found");

        SBondPositionsList.RemoveAll(sp => sp.Sec.IdRemote == currInstrument.IdRemote);
        SBondSizePositionsList.RemoveAll(sp => sp.Sec.IdRemote == currInstrument.IdRemote);
        SBondSmallPositionsList.RemoveAll(sp => sp.Sec.IdRemote == currInstrument.IdRemote);

        Security currentSecurity = currentSecurities.First(sec => sec.Id == currInstrument.IdRemote);
        string msg;
        if (currentSecurity is null)
        {
            msg = $"Security - not found for instrument: {JsonConvert.SerializeObject(currInstrument, Formatting.Indented)}";
            _logger.LogError(msg);
            return ResponseBaseModel.CreateError(msg);
        }

        TResponseModel<List<InstrumentTradeStockSharpViewModel>> resInstruments = await dataRepo.ReadTradeInstrumentsAsync(cancellationToken);

        if (resInstruments.Response is null || resInstruments.Response.Count == 0)
            return ResponseBaseModel.CreateError($"The instruments are not configured.");

        List<DashboardTradeStockSharpModel> dataParse = await ReadDashboard([.. resInstruments.Response.Select(x => x.Id)], cancellationToken);

        if (dataParse.Count == 0)
            return ResponseBaseModel.CreateError("Dashboard - not set");

        int _fx = dataParse.FindIndex(x => x.Id == req.InstrumentId);
        if (_fx < 0)
            return ResponseBaseModel.CreateError($"Instrument not set strategy: {currInstrument}");

        DashboardTradeStockSharpModel currentStrategy = dataParse[_fx];

        InstrumentTradeStockSharpModel _sec = new InstrumentTradeStockSharpModel().Bind(currentSecurity);

        decimal
            WorkVol = currentStrategy.WorkingVolume,
            SmallBidVol = currentStrategy.SmallBidVolume,
            SmallOfferVol = currentStrategy.SmallOfferVolume,
            LowLimit = currentStrategy.LowLimit,
            Highlimit = currentStrategy.HightLimit,
            SmallOffset = currentStrategy.SmallOffset,
            Offset = currentStrategy.Offset;

        bool IsSmall = currentStrategy.IsSmall;
        SBondPositionsList.Add(new SecurityPosition(_sec, "Quote", (decimal)LowLimit / 100, (decimal)Highlimit / 100, (decimal)WorkVol, (decimal)WorkVol, (decimal)Offset / 100));

        if (IsSmall)
            SBondSmallPositionsList.Add(new SecurityPosition(_sec, "Small", (decimal)(0.0301), (decimal)(Highlimit - (decimal)0.1) / 100, (decimal)SmallBidVol, (decimal)SmallOfferVol, (decimal)SmallOffset / 100));

        //int[] _dsc = [(int)MarkersInstrumentStockSharpEnum.IsNew];

        SBondSizePositionsList.Add(new SecurityPosition(_sec, "Size", (decimal)(Highlimit + (decimal)0.1) / 100, (decimal)(LowLimit + Highlimit) / 100, quoteSizeStrategyVolume, quoteSizeStrategyVolume, 0m));

        Subscription sub = conLink.Connector.FindSubscriptions(currentSecurity, DataType.MarketDepth).Where(s => s.SubscriptionMessage.To == null && s.State.IsActive()).First();
        OrderBookReceivedConnectorMan(sub, OderBookList[currentSecurity]);

        return ResponseBaseModel.CreateInfo($"`{nameof(ResetStrategy)}` - done");
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> ResetAllStrategies(ResetStrategyRequestBaseModel req, CancellationToken cancellationToken = default)
    {
        quoteSizeStrategyVolume = req.Size;
        quoteStrategyVolume = req.Volume;

        DeleteAllQuotesByStrategy("Size");
        DeleteAllQuotesByStrategy("Small");
        DeleteAllQuotesByStrategy("Quote");

        lock (SBondPositionsList)
            SBondPositionsList.Clear();
        lock (SBondSizePositionsList)
            SBondSizePositionsList.Clear();
        lock (SBondSmallPositionsList)
            SBondSmallPositionsList.Clear();

        List<Security> currentSecurities = SecuritiesBonds(true);

        if (!currentSecurities.Any())
            return ResponseBaseModel.CreateError("BondList - not any");

        TResponseModel<List<InstrumentTradeStockSharpViewModel>> resInstruments = await dataRepo.ReadTradeInstrumentsAsync(cancellationToken);

        if (resInstruments.Response is null || resInstruments.Response.Count == 0)
            return ResponseBaseModel.CreateError($"The instruments are not configured.");

        List<DashboardTradeStockSharpModel> dataParse = await ReadDashboard([.. resInstruments.Response.Select(x => x.Id)], cancellationToken);

        if (dataParse.Count == 0)
            return ResponseBaseModel.CreateError("Dashboard - not set");

        string msg;
        foreach (InstrumentTradeStockSharpViewModel instrument in resInstruments.Response)
        {
            int _fx = dataParse.FindIndex(x => x.Id == instrument.Id);
            if (_fx < 0)
                return ResponseBaseModel.CreateError($"Instrument not set: {instrument}");

            if (dataParse[_fx].ValueOperation < 1)
                return ResponseBaseModel.CreateError($"Value for instrument '{instrument}' incorrect");

            if (dataParse[_fx].BasePrice < 1)
                return ResponseBaseModel.CreateError($"Price for instrument '{instrument}' incorrect");

            DashboardTradeStockSharpModel currentStrategy = dataParse[_fx];

            Security? currentSecurity = currentSecurities.FirstOrDefault(x =>
            x.Code == currentStrategy.Code &&
            (int?)x.Currency == currentStrategy.Currency &&
            x.Board.Code == currentStrategy.Board?.Code);

            if (currentSecurity is null)
            {
                msg = $"Security - not found for strategy: {JsonConvert.SerializeObject(currentStrategy, Formatting.Indented)}";
                _logger.LogError(msg);
                return ResponseBaseModel.CreateError(msg);
            }
            InstrumentTradeStockSharpModel _sec = new InstrumentTradeStockSharpModel().Bind(currentSecurity);
            decimal WorkVol = currentStrategy.WorkingVolume;
            decimal SmallBidVol = currentStrategy.SmallBidVolume;
            decimal SmallOfferVol = currentStrategy.SmallOfferVolume;
            decimal LowLimit = currentStrategy.LowLimit;
            decimal Highlimit = currentStrategy.HightLimit;
            decimal SmallOffset = currentStrategy.SmallOffset;
            decimal Offset = currentStrategy.Offset;
            bool IsSmall = currentStrategy.IsSmall;

            SBondPositionsList.Add(new SecurityPosition(_sec, "Quote", LowLimit / 100,
             Highlimit / 100, WorkVol, WorkVol, Offset / 100));

            if (IsSmall)
                SBondSmallPositionsList.Add(new SecurityPosition(_sec, "Small", (decimal)(0.0301), (LowLimit - (decimal)0.1) / 100, SmallBidVol, SmallOfferVol, SmallOffset / 100));

            if (!instrument.Markers!.Any(x => x.MarkerDescriptor == (int)MarkersInstrumentStockSharpEnum.Illiquid))
                SBondSizePositionsList.Add(new SecurityPosition(_sec, "Size", (Highlimit + (decimal)0.1) / 100, (LowLimit + Highlimit) / 100, quoteSizeStrategyVolume, quoteSizeStrategyVolume, 0m));

            Subscription? sub = conLink.Connector.FindSubscriptions(currentSecurity, DataType.MarketDepth).Where(s => s.SubscriptionMessage.To == null && s.State.IsActive()).FirstOrDefault();

            if (sub is not null)
                OrderBookReceivedConnectorMan(sub, OderBookList[currentSecurity]);
            else
            {
                _logger.LogError($"sub is not null");
                return ResponseBaseModel.CreateError("sub is not null");
            }
        }
        return ResponseBaseModel.CreateInfo($"done: reset for {resInstruments.Response.Count} instruments");
    }


    /// <inheritdoc/>
    public Task<ResponseBaseModel> ShiftCurve(ShiftCurveRequestModel req, CancellationToken cancellationToken = default)
    {
        if (CurveCurrent is null)
            return Task.FromResult(ResponseBaseModel.CreateWarning("OfzCurve is null"));

        _logger.LogWarning($"Curve changed: {req.YieldChange}");

        CurveCurrent.BondList.ForEach(bnd =>
        {
            SBond? SBnd = SBondList.FirstOrDefault(s => s.UnderlyingSecurity.Code == bnd.MicexCode);

            if (SBnd is not null)
            {
                decimal yield = SBnd.GetYieldForPrice(CurveCurrent.CurveDate, bnd.ModelPrice / 100);
                if (yield > 0)
                {
                    bnd.ModelPrice = Math.Round(100 * SBnd.GetPriceFromYield(CurveCurrent.CurveDate, yield + req.YieldChange / 10000, true), 2);
                }
            }
        });
        return Task.FromResult(ResponseBaseModel.CreateSuccess($"Ok - {nameof(ShiftCurve)} changed: {req.YieldChange}"));
    }


    /// <inheritdoc/>
    public async Task<List<DashboardTradeStockSharpModel>> ReadDashboard(int[] instrumentsIds, CancellationToken cancellationToken = default)
    {
        FindStorageBaseModel _findParametersQuery = new()
        {
            ApplicationName = GlobalStaticConstantsTransmission.TransmissionQueues.TradeInstrumentStrategyStockSharpReceive,
            PropertyName = $"{GlobalStaticConstantsRoutes.Routes.TRADE_CONTROLLER_NAME}-{GlobalStaticConstantsRoutes.Routes.STRATEGY_CONTROLLER_NAME}",
            OwnersPrimaryKeys = instrumentsIds,
        };

        FundedParametersModel<DashboardTradeStockSharpModel>[] findStorageRows = await storageRepo.FindAsync<DashboardTradeStockSharpModel>(_findParametersQuery, cancellationToken);

        if (findStorageRows.Length == 0)
            return [];

        IQueryable<IGrouping<int?, FundedParametersModel<DashboardTradeStockSharpModel>>> _q = findStorageRows.Where(x => x.PrefixPropertyName == GlobalStaticConstantsRoutes.Routes.BROKER_CONTROLLER_NAME)
            .GroupBy(x => x.OwnerPrimaryKey)
            .Where(x => x.Key.HasValue)
            .AsQueryable();

        return [.. _q.Select(x => x.OrderByDescending(x => x.CreatedAt).First().Payload)];
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> Connect(ConnectRequestModel req, CancellationToken cancellationToken = default)
    {
        BoardsCurrent?.Clear();
        if (SecuritiesBonds(false).Any())
            return ResponseBaseModel.CreateError($"BondList is not empty!");

        TPaginationRequestStandardModel<AdaptersRequestModel> adReq = new()
        {
            PageSize = int.MaxValue,
            Payload = new() { OnlineOnly = true, }
        };

        TPaginationResponseModel<FixMessageAdapterModelDB> adRes = await manageRepo.AdaptersSelectAsync(adReq, cancellationToken);
        if (adRes.Response is null || adRes.Response.Count == 0)
            return ResponseBaseModel.CreateError("adapters - is empty");

        lock (AllSecurities)
        {
            AllSecurities.Clear();
        }

        LastConnectedAt = DateTime.UtcNow;

        List<FixMessageAdapterModelDB> adapters = adRes.Response;

        ResponseBaseModel res = new();
        adapters.ForEach(x =>
        {
            if (string.IsNullOrWhiteSpace(x.Address))
            {
                res.AddError($"set address for adapter#{x.Id}");
                return;
            }

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
                            IsDemo = x.IsDemo,
                        };
                        conLink.Connector.Adapter.InnerAdapters.Add(luaFixMarketDataMessageAdapter);
                        break;
                    case nameof(LuaFixTransactionMessageAdapter):
                        LuaFixTransactionMessageAdapter luaFixTransactionMessageAdapter = new(conLink.Connector.TransactionIdGenerator)
                        {
                            Address = x.Address.To<EndPoint>(),
                            Login = x.Login,
                            Password = secure,
                            IsDemo = x.IsDemo,
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
                _logger.LogError(ex, $"Ошибка инициализации адаптера ~{JsonConvert.SerializeObject(x)}");
                res.Messages.InjectException(ex);
            }
        });

        if (!conLink.Connector.CanConnect)
        {
            res.AddError("can`t connect");
            return res;
        }

        await Task.WhenAll([
                Task.Run(async () => { SecurityCriteriaCodeFilter = await storageRepo.ReadAsync<string>(GlobalStaticCloudStorageMetadata.SecuritiesCriteriaCodeFilterStockSharp,token: cancellationToken); }, cancellationToken),
                Task.Run(async () => { BoardCriteriaCodeFilter = await storageRepo.ReadAsync<string>(GlobalStaticCloudStorageMetadata.BoardCriteriaCodeFilterStockSharp, token:cancellationToken); }, cancellationToken),
                Task.Run(async () => { ClientCodeStockSharp = await storageRepo.ReadAsync<string>(GlobalStaticCloudStorageMetadata.ClientCodeBrokerStockSharp, token: cancellationToken); }, cancellationToken)
            ]);

        RegisterEvents();
        if (!string.IsNullOrWhiteSpace(SecurityCriteriaCodeFilter))
            conLink.Connector.SubscriptionsOnConnect.RemoveRange(conLink.Connector.SubscriptionsOnConnect.Where(x => x.DataType == DataType.Securities));

        await conLink.Connector.ConnectAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(SecurityCriteriaCodeFilter))
        {
            lock (SecuritiesCriteriaCodesFilterLookups)
            {
                SecuritiesCriteriaCodesFilterLookups.Clear();
                foreach (string _sc in Regex.Split(SecurityCriteriaCodeFilter, @"\s+").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct())
                {
                    SecuritiesCriteriaCodesFilterLookups.Add(new()
                    {
                        SecurityId = new SecurityId
                        {
                            SecurityCode = _sc.Trim(),
                            BoardCode = BoardCriteriaCodeFilter
                        },
                        TransactionId = conLink.Connector.TransactionIdGenerator.GetNextId()
                    });
                    SecurityCriteriaCodeFilterSubscription = new(SecuritiesCriteriaCodesFilterLookups.Last());
                    conLink.Connector.Subscribe(SecurityCriteriaCodeFilterSubscription);
                }
            }
        }

        res.AddInfo($"connection: {conLink.Connector.ConnectionState}");
        return res;
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> Disconnect(CancellationToken cancellationToken = default)
    {
        ClientCodeStockSharp = null;
        SecurityCriteriaCodeFilter = null;

        await ClearStrategy(cancellationToken);
        conLink.Connector.CancelOrders();
        foreach (Subscription sub in conLink.Connector.Subscriptions)
        {
            conLink.Connector.UnSubscribe(sub);
            _logger.LogInformation($"{nameof(Connector.UnSubscribe)} > {sub.GetType().FullName}");
        }

        lock (SecuritiesCriteriaCodesFilterLookups)
            SecuritiesCriteriaCodesFilterLookups.Clear();

        UnregisterEvents();
        conLink.Connector.Disconnect();

        lock (AllSecurities)
            AllSecurities.Clear();
        return ResponseBaseModel.CreateInfo("connection closed");
    }

    /// <inheritdoc/>
    public Task<AboutConnectResponseModel> AboutConnection(CancellationToken cancellationToken = default)
    {
        DateTime _lc = LastConnectedAt;
        AboutConnectResponseModel res = new()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState)!),
            LastConnectedAt = (_lc == DateTime.MinValue || _lc == default) ? null : _lc,
            StrategyStarted = StrategyStarted,
            LowLimit = lowLimit,
            HighLimit = highLimit,
            SecurityCriteriaCodeFilter = SecurityCriteriaCodeFilter,
            ClientCode = ClientCodeStockSharp,
            ProgramPath = ProgramDataPath,
            Curve = CurveCurrent,
        };

        return Task.FromResult(res);
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> OrderRegisterAsync(CreateOrderRequestModel req, CancellationToken cancellationToken = default)
    {
        if (req.PortfolioId <= 0)
            return ResponseBaseModel.CreateError("Portfolio not set");
        if (req.InstrumentId <= 0)
            return ResponseBaseModel.CreateError("Instrument not set");

        TResponseModel<List<InstrumentTradeStockSharpViewModel>> resInstrument = default!;
        TResponseModel<List<InstrumentTradeStockSharpViewModel>> resStrategies = default!;
        TResponseModel<List<PortfolioStockSharpViewModel>> resPortfolio = default!;

        await Task.WhenAll([
            Task.Run(async () => resStrategies = await dataRepo.ReadTradeInstrumentsAsync(cancellationToken)),
            Task.Run(async () =>  resInstrument = await dataRepo.GetInstrumentsAsync([req.InstrumentId],cancellationToken)),
            Task.Run(async () =>  resPortfolio = await dataRepo.GetPortfoliosAsync([req.PortfolioId],cancellationToken))
        ]);

        if (resPortfolio.Response is null || resPortfolio.Response.Count == 0)
            return ResponseBaseModel.CreateError($"The portfolio #{req.PortfolioId} are not found.");

        if (resInstrument.Response is null || resInstrument.Response.Count == 0)
            return ResponseBaseModel.CreateError($"The instrument #{req.InstrumentId} are not found.");

        if (resStrategies.Response is null || resStrategies.Response.Count == 0)
            return ResponseBaseModel.CreateError($"The instruments are not configured.");

        InstrumentTradeStockSharpViewModel instrumentDb = resInstrument.Response[0];
        PortfolioStockSharpViewModel portfolioDb = resPortfolio.Response[0];

        Security? currentSec = conLink.Connector.Securities.FirstOrDefault(x => x.Code == instrumentDb.Code && x.Board.Code == instrumentDb.Board!.Code && (int?)x.Board.Exchange.CountryCode == instrumentDb.Board.Exchange?.CountryCode);
        if (currentSec is null)
            return ResponseBaseModel.CreateError($"Инструмент не найден (aka Security): {instrumentDb}");

        Portfolio? selectedPortfolio = conLink.Connector.Portfolios.FirstOrDefault(x => x.ClientCode == portfolioDb.ClientCode && x.Name == portfolioDb.Name);
        if (selectedPortfolio is null)
            return ResponseBaseModel.CreateError($"Портфель не найден: {portfolioDb}");

        Order order = new()
        {
            Type = (OrderTypes)Enum.Parse(typeof(OrderTypes), Enum.GetName(req.OrderType)!),
            Portfolio = selectedPortfolio,
            Volume = req.Volume,
            Price = req.Price,
            Security = currentSec,
            Side = (Sides)Enum.Parse(typeof(Sides), Enum.GetName(req.Side)!),
            IsManual = req.IsManual,
            IsMarketMaker = instrumentDb.Markers!.Any(x => x.MarkerDescriptor == (int)MarkersInstrumentStockSharpEnum.IsMarketMaker),
            IsSystem = instrumentDb.Markers!.Any(x => x.MarkerDescriptor == (int)MarkersInstrumentStockSharpEnum.IsSystem),
            Comment = req.Comment,
            ClientCode = ClientCodeStockSharp
        };

        try
        {
            conLink.Connector.RegisterOrder(order);
        }
        catch (Exception ex)
        {
            return ResponseBaseModel.CreateError(ex);
        }

        return ResponseBaseModel.CreateInfo("Заявка отправлена на регистрацию");
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> Terminate(CancellationToken cancellationToken = default)
    {
        UnregisterEvents();
        conLink.Connector.Dispose();
        conLink.Connector = new();
        RegisterEvents();

        await eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState)!)
        }, cancellationToken);

        return ResponseBaseModel.CreateSuccess("Connection terminated");
    }

    async void DeleteAllQuotesByStrategy(string strategy)
    {
        IEnumerable<Order> orders = AllOrders.Where(s => s.State == OrderStates.Active);

        if (string.IsNullOrEmpty(strategy))
        {
            foreach (Order order in orders)
            {
                conLink.Connector.CancelOrder(order);
                await eventTrans.ToastClientShow(new()
                {
                    HeadTitle = $"`{nameof(DeleteAllQuotesByStrategy)}` without strategy",
                    MessageText = string.Format("Order cancelled: ins ={0}, price = {1}, volume = {2}", order.Security, order.Price, order.Volume),
                    TypeMessage = MessagesTypesEnum.Warning
                });
            }
        }
        else
        {
            foreach (Order order in orders)
            {
                if ((!string.IsNullOrEmpty(order.Comment)) && order.Comment.ContainsIgnoreCase(strategy))
                    conLink.Connector.CancelOrder(order);

                await eventTrans.ToastClientShow(new()
                {
                    HeadTitle = $"`{nameof(DeleteAllQuotesByStrategy)}` with strategy '{strategy}'",
                    MessageText = string.Format("Order cancelled: ins ={0}, price = {1}, volume = {2}", order.Security, order.Price, order.Volume),
                    TypeMessage = MessagesTypesEnum.Warning
                });
            }
        }
    }

    void OnDatabaseChanged(object source, FileSystemEventArgs a)
    {
        string msg = $"call > {nameof(OnDatabaseChanged)}: {a.Name}", headTitle = $"{fileWatcher.GetType().Name}.{nameof(fileWatcher.Changed)}";
        _logger.LogWarning(msg);
        eventTrans.ToastClientShow(new()
        {
            HeadTitle = headTitle,
            TypeMessage = MessagesTypesEnum.Info,
            MessageText = msg
        });

        if (CurveCurrent is null)
        {
            msg = $"Curve is null";
            _logger.LogError(msg);
            eventTrans.ToastClientShow(new()
            {
                HeadTitle = headTitle,
                TypeMessage = MessagesTypesEnum.Error,
                MessageText = msg
            });
            return;
        }
        if (string.IsNullOrWhiteSpace(ProgramDataPath))
        {
            msg = $"string.IsNullOrWhiteSpace(ProgramDataPath)";
            _logger.LogError(msg);
            eventTrans.ToastClientShow(new()
            {
                HeadTitle = headTitle,
                TypeMessage = MessagesTypesEnum.Error,
                MessageText = msg
            });
            return;
        }
        if (!Directory.Exists(ProgramDataPath))
        {
            msg = $"!Directory.Exists('{ProgramDataPath}')";
            _logger.LogError(msg);
            eventTrans.ToastClientShow(new()
            {
                HeadTitle = headTitle,
                TypeMessage = MessagesTypesEnum.Error,
                MessageText = msg
            });
            return;
        }
        if (BoardsCurrent is null || BoardsCurrent.Count == 0)
        {
            msg = $"Boards is null || Boards.Count == 0";
            _logger.LogError(msg);
            eventTrans.ToastClientShow(new()
            {
                HeadTitle = headTitle,
                TypeMessage = MessagesTypesEnum.Error,
                MessageText = msg
            });
            return;
        }

        try
        {
            string? _res = CurveCurrent.GetCurveFromDb(Path.Combine(ProgramDataPath, "RedArrowData.db"), conLink.Connector, BoardsCurrent, null, ref eventTrans);
            if (!string.IsNullOrWhiteSpace(_res))
            {
                msg = $"Curve.GetCurveFromDb is null";
                _logger.LogError(msg);
                eventTrans.ToastClientShow(new()
                {
                    HeadTitle = headTitle,
                    TypeMessage = MessagesTypesEnum.Error,
                    MessageText = msg
                });
                return;
            }
            if (CurveCurrent.BondList.Count == 0)
            {
                msg = $"Curve.BondList.Count == 0";
                _logger.LogError(msg);
                eventTrans.ToastClientShow(new()
                {
                    HeadTitle = headTitle,
                    TypeMessage = MessagesTypesEnum.Error,
                    MessageText = msg
                });
                return;
            }
            List<Security> secS = SecuritiesBonds(false);
            secS.ForEach(security =>
                {
                    if (OderBookList.ContainsKey(security))
                    {
                        Subscription? sub = conLink.Connector
                        .FindSubscriptions(security, DataType.MarketDepth)
                        .FirstOrDefault(s => s.SubscriptionMessage.To == null && s.State.IsActive());

                        if (sub is null)
                        {
                            msg = $"Active subscription [{nameof(DataType.MarketDepth)}] not found for security '{security}'";
                            _logger.LogError(msg);
                            eventTrans.ToastClientShow(new()
                            {
                                HeadTitle = headTitle,
                                TypeMessage = MessagesTypesEnum.Error,
                                MessageText = msg
                            });
                            return;
                        }

                        OrderBookReceivedHandle(sub, OderBookList[security]);
                    }
                });

            conLink.Connector.AddWarningLog("Curve changed");
        }
        catch (Exception ex)
        {
            msg = $"Error of OnDatabaseChanged for Curve";
            _logger.LogError(ex, msg);
            eventTrans.ToastClientShow(new()
            {
                HeadTitle = headTitle,
                TypeMessage = MessagesTypesEnum.Error,
                MessageText = $"{msg}: {ex.Message}"
            });
        }
    }

    async Task ClearStrategy(CancellationToken cancellationToken = default)
    {
        lock (MarketDepthSubscriptions)
        {
            MarketDepthSubscriptions.ForEach(conLink.Connector.UnSubscribe);
            MarketDepthSubscriptions.Clear();
        }

        if (SecurityCriteriaCodeFilterSubscription is not null)
            conLink.Connector.UnSubscribe(SecurityCriteriaCodeFilterSubscription);
        SecurityCriteriaCodeFilterSubscription = null;

        conLink.Connector.OrderBookReceived -= MarketDepthOrderBookHandle;

        BoardsCurrent?.Clear();
        PortfolioCurrent = null;

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

        lowLimit = 0.19m;
        highLimit = 0.25m;

        await eventTrans.UpdateConnectionHandle(new UpdateConnectionHandleModel()
        {
            CanConnect = conLink.Connector.CanConnect,
            ConnectionState = (ConnectionStatesEnum)Enum.Parse(typeof(ConnectionStatesEnum), Enum.GetName(conLink.Connector.ConnectionState)!)
        }, cancellationToken);
    }

    #region events
    /// <inheritdoc/>
    async void OrderBookReceivedConnectorMan(Subscription subscription, IOrderBookMessage depth)
    {
        if (CurveCurrent is null)
        {
            _logger.LogError("Curve is null");
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = $"err [{nameof(OrderBookReceivedConnectorMan)}]",
                MessageText = "Curve is null",
                TypeMessage = MessagesTypesEnum.Error
            });
            return;
        }

        TPaginationResponseModel<InstrumentTradeStockSharpViewModel> resInstruments = dataRepo.InstrumentsSelectAsync(new()
        {
            PageNum = 0,
            PageSize = int.MaxValue,
        }).Result;
        string _msg;
        if (resInstruments.Response is null || resInstruments.Response.Count == 0)
        {
            _msg = $"The instruments are not configured.";
            _logger.LogError(_msg);
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = nameof(OrderBookReceivedConnectorMan),
                MessageText = _msg,
                TypeMessage = MessagesTypesEnum.Error
            });
            return;
        }
        List<DashboardTradeStockSharpModel> dataParse = ReadDashboard([.. resInstruments.Response.Select(x => x.Id)]).Result;

        if (dataParse.Count == 0)
        {
            _msg = "Dashboard - not set";

            _logger.LogError(_msg);
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = nameof(OrderBookReceivedConnectorMan),
                MessageText = _msg,
                TypeMessage = MessagesTypesEnum.Error
            });
            return;
        }

        Order tmpOrder;
        Order newOrder;
        IOrderBookMessage tmpDepth;
        decimal price;

        Security sec = conLink.Connector.Securities.First(s => s.ToSecurityId() == depth.SecurityId);
        InstrumentTradeStockSharpModel _sec = new InstrumentTradeStockSharpModel().Bind(sec);
        SecurityPosition SbPos = SBondPositionsList.First(sp => sp.Sec.Equals(sec));

        InstrumentTradeStockSharpViewModel? currentInstrument = resInstruments.Response
            .FirstOrDefault(x => x.Code == sec.Code && x.Currency == (int?)sec.Currency && x.Board?.Code == sec.Board.Code);

        if (currentInstrument is null)
        {
            _msg = $"Instrument not found - {JsonConvert.SerializeObject(sec, Formatting.Indented)}";
            _logger.LogError(_msg);
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = nameof(OrderBookReceivedConnectorMan),
                MessageText = _msg,
                TypeMessage = MessagesTypesEnum.Error
            });
            return;
        }

        DashboardTradeStockSharpModel? currentStrategy = dataParse.FirstOrDefault(x => x.Id == currentInstrument.Id);
        if (currentStrategy is null)
        {
            _msg = $"Strategy not found - {JsonConvert.SerializeObject(currentInstrument, Formatting.Indented)}";
            _logger.LogError(_msg);
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = nameof(OrderBookReceivedConnectorMan),
                MessageText = _msg,
                TypeMessage = MessagesTypesEnum.Error
            });
            return;
        }
        SBond? secCurceNode = CurveCurrent.GetNode(_sec);
        if (secCurceNode is null)
        {
            _msg = $"Curve.GetNode - is null";
            _logger.LogError(_msg);
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = nameof(OrderBookReceivedConnectorMan),
                MessageText = _msg,
                TypeMessage = MessagesTypesEnum.Error
            });
            return;
        }

        bool isMarketMaker = currentInstrument.Markers?.Any(x => x.MarkerDescriptor == (int)MarkersInstrumentStockSharpEnum.IsMarketMaker) == true;
        if (SbPos is not null && sec is not null)
        {
            if (!_ordersForQuoteBuyReregister.ContainsKey(sec.Code) && depth.Bids is not null && !AllOrders.Any(s => ((s.State == OrderStates.Pending) && (s.Comment is not null) && (s.Comment.ContainsIgnoreCase("Quote")) && (s.Security.Code == sec.Code) && (s.Side == Sides.Buy))))
            {
                IEnumerable<Order> Orders = AllOrders.Where(s => (s.State == OrderStates.Active) && (s.Security.Code == sec.Code) && (s.Comment is not null) && (s.Comment.ContainsIgnoreCase("Quote")) && (s.Side == Sides.Buy));

                if (Orders.IsEmpty()) //if there is no orders in stakan
                {
                    price = MyHelper.GetBestConditionPrice(sec, depth, secCurceNode.ModelPrice + SbPos.Offset, -SbPos.LowLimit, -SbPos.HighLimit, 2.101m * SbPos.BidVolume);
                    if (price > 0)
                    {
                        Order ord = new()
                        {
                            Security = sec,
                            Portfolio = PortfolioCurrent,
                            Price = price,
                            Side = Sides.Buy,
                            Comment = "Quote",
                            IsMarketMaker = isMarketMaker,
                            Volume = SbPos.BidVolume,
                            ClientCode = ClientCodeStockSharp,
                        };
                        conLink.Connector.RegisterOrder(ord);
                        await eventTrans.ToastClientShow(new()
                        {
                            HeadTitle = $"`{nameof(OrderBookReceivedConnectorMan)}` there is no orders in MarketDepth",
                            MessageText = string.Format("Order buy registered new: ins ={0}, price = {1}, volume = {2}", sec, price, SbPos.BidVolume),
                            TypeMessage = MessagesTypesEnum.Warning
                        });
                    }
                }
                else
                {
                    tmpOrder = Orders.First();
                    tmpDepth = (IOrderBookMessage)depth.Clone();

                    int Len = tmpDepth.Bids.Length;
                    for (int i = 0; i < Len; i++)
                    {
                        if ((tmpDepth.Bids[i].Price == tmpOrder.Price) && (tmpDepth.Bids[i].Volume >= tmpOrder.Balance))
                        {
                            tmpDepth.Bids[i].Volume = tmpDepth.Bids[i].Volume - tmpOrder.Balance;
                            break;
                        }
                    }

                    price = MyHelper.GetBestConditionPrice(sec, tmpDepth, secCurceNode.ModelPrice + SbPos.Offset, -SbPos.LowLimit, -SbPos.HighLimit, 2.101m * SbPos.BidVolume);

                    if ((price > 0) && ((price != tmpOrder.Price) || (tmpOrder.Balance != SbPos.BidVolume)))
                    {
                        newOrder = new Order()
                        {
                            Security = sec,
                            Portfolio = PortfolioCurrent,
                            Price = price,
                            Side = Sides.Buy,
                            Comment = "Quote",
                            IsMarketMaker = isMarketMaker,
                            Volume = SbPos.BidVolume,
                            ClientCode = ClientCodeStockSharp,
                        };
                        _ordersForQuoteBuyReregister.Add(tmpOrder.Security.Code, newOrder);
                        await eventTrans.ToastClientShow(new()
                        {
                            HeadTitle = $"`{nameof(OrderBookReceivedConnectorMan)}` with orders (x {Orders.Count()}) in MarketDepth",
                            MessageText = string.Format("Order buy cancelled for reregister: ins ={0}, price = {1}, volume = {2}", sec, tmpOrder.Price, tmpOrder.Volume),
                            TypeMessage = MessagesTypesEnum.Warning
                        });
                        conLink.Connector.CancelOrder(tmpOrder);
                    }

                    Orders.Skip(1).ForEach(s =>
                    {
                        eventTrans.ToastClientShow(new()
                        {
                            HeadTitle = "Skip order",
                            MessageText = string.Format("Order buy duplication!"),
                            TypeMessage = MessagesTypesEnum.Warning
                        });
                        if (s.Id != tmpOrder.Id)
                        {
                            eventTrans.ToastClientShow(new()
                            {
                                HeadTitle = "Warning",
                                MessageText = string.Format("Duplicate buy order cancelled: ins ={0}, price = {1}, volume = {2}", s.Security, s.Price, s.Volume),
                                TypeMessage = MessagesTypesEnum.Warning
                            });

                            conLink.Connector.CancelOrder(s);
                        }
                    });
                }
            }

            //only for sell orders
            if (!_ordersForQuoteSellReregister.ContainsKey(sec.Code) && depth.Asks is not null && !AllOrders.Any(s => ((s.State == OrderStates.Pending) && (s.Comment is not null) && (s.Comment.ContainsIgnoreCase("Quote")) && (s.Security.Code == sec.Code) && (s.Side == Sides.Sell))))
            {
                IEnumerable<Order> Orders = AllOrders.Where(s => (s.State == OrderStates.Active) && (s.Security.Code == sec.Code) && (s.Comment is not null) && (s.Comment.ContainsIgnoreCase("Quote")) && (s.Side == Sides.Sell));

                if (Orders.IsEmpty()) //if there is no orders in stakan
                {
                    price = MyHelper.GetBestConditionPrice(sec, depth, secCurceNode.ModelPrice + SbPos.Offset, SbPos.LowLimit, SbPos.HighLimit, 2.101m * SbPos.OfferVolume);
                    if (price > 0)
                    {
                        Order ord = new()
                        {
                            Security = sec,
                            Portfolio = PortfolioCurrent,
                            Price = price,
                            Side = Sides.Sell,
                            Comment = "Quote",
                            IsMarketMaker = isMarketMaker,
                            Volume = SbPos.OfferVolume,
                            ClientCode = ClientCodeStockSharp,
                        };

                        conLink.Connector.RegisterOrder(ord);
                        await eventTrans.ToastClientShow(new()
                        {
                            HeadTitle = "Warning",
                            MessageText = string.Format("Order sell registered new: ins ={0}, price = {1}, volume = {2}", sec, price, SbPos.OfferVolume),
                            TypeMessage = MessagesTypesEnum.Warning
                        });
                    }
                }
                else
                {
                    tmpOrder = Orders.First();
                    tmpDepth = (IOrderBookMessage)depth.Clone();

                    int Len = tmpDepth.Asks.Length;
                    for (int i = 0; i < Len; i++)
                    {
                        if ((tmpDepth.Asks[i].Price == tmpOrder.Price) && (tmpDepth.Asks[i].Volume >= tmpOrder.Balance))
                        {
                            tmpDepth.Asks[i].Volume = tmpDepth.Asks[i].Volume - tmpOrder.Balance;
                            break;
                        }
                    }

                    price = MyHelper.GetBestConditionPrice(sec, tmpDepth, secCurceNode.ModelPrice + SbPos.Offset, SbPos.LowLimit, SbPos.HighLimit, 2.101m * SbPos.OfferVolume);

                    if ((price > 0) && ((price != tmpOrder.Price) || (tmpOrder.Balance != SbPos.OfferVolume)))
                    {
                        newOrder = new Order()
                        {
                            Security = sec,
                            Portfolio = PortfolioCurrent,
                            Price = price,
                            Side = Sides.Sell,
                            Comment = "Quote",
                            IsMarketMaker = isMarketMaker,
                            Volume = SbPos.OfferVolume,
                            ClientCode = ClientCodeStockSharp,
                        };
                        _ordersForQuoteSellReregister.Add(tmpOrder.Security.Code, newOrder);
                        await eventTrans.ToastClientShow(new()
                        {
                            HeadTitle = "Warning",
                            MessageText = string.Format("Order sell cancelled for reregister: ins ={0}, price = {1}, volume = {2}", sec, tmpOrder.Price, tmpOrder.Volume),
                            TypeMessage = MessagesTypesEnum.Warning
                        });
                        conLink.Connector.CancelOrder(tmpOrder);
                    }

                    Orders.Skip(1).ForEach(s =>
                    {
                        eventTrans.ToastClientShow(new()
                        {
                            HeadTitle = "Warning",
                            MessageText = string.Format("Order sell duplication!"),
                            TypeMessage = MessagesTypesEnum.Warning
                        });
                        if (s.Id != tmpOrder.Id)
                        {
                            eventTrans.ToastClientShow(new()
                            {
                                HeadTitle = "Warning",
                                MessageText = string.Format("Duplicate sell order cancelled: ins ={0}, price = {1}, volume = {2}", s.Security, s.Price, s.Volume),
                                TypeMessage = MessagesTypesEnum.Warning
                            });
                            conLink.Connector.CancelOrder(s);
                        }
                    });
                }
            }
        }
    }

    async void MarketDepthOrderBookHandle(Subscription subscription, IOrderBookMessage depth)
    {
        _logger.LogInformation($"Call `{nameof(MarketDepthOrderBookHandle)}` > Стакан: {depth.SecurityId}, Время: {depth.ServerTime}; | Покупки (Bids): {depth.Bids.Length}, Продажи (Asks): {depth.Asks.Length}");

        lock (MarketDepthSubscriptions)
            if (!MarketDepthSubscriptions.Any(x => x.SecurityId == subscription.SecurityId))
                return;

        await eventTrans.ToastClientShow(new()
        {
            HeadTitle = nameof(MarketDepthOrderBookHandle),
            TypeMessage = MessagesTypesEnum.Info,
            MessageText = $"Стакан: {depth.SecurityId}, Время: {depth.ServerTime}; | Покупки (Bids): {depth.Bids.Length}, Продажи (Asks): {depth.Asks.Length}"
        });

        Security sec = conLink.Connector.Securities.First(s => s.ToSecurityId() == depth.SecurityId);
        List<Security> BondList = SecuritiesBonds(true);
        if (BondList.Contains(sec))
        {
            if (OderBookList.ContainsKey(sec))
                OderBookList[sec] = depth;
            else
                OderBookList.Add(sec, depth);
        }
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

    async void OrderReceivedHandle(Subscription subscription, Order order)
    {
        await eventTrans.ToastClientShow(new()
        {
            HeadTitle = nameof(conLink.Connector.OrderReceived),
            TypeMessage = MessagesTypesEnum.Success,
            MessageText = $"{nameof(order.Security)}:{order.Security.Id}; {nameof(order.Price)}:{order.Price}; {nameof(order.Volume)}:{order.Volume};"
        });

        lock (AllOrders)
        {
            int _i = AllOrders.FindIndex(x => x.Id.Equals(order.Id));

            if (_i == -1)
                AllOrders.Add(order);
            else
                AllOrders[_i] = order;
        }
    }

    async void OwnTradeReceivedHandle(Subscription subscription, MyTrade tr)
    {
        lock (myTrades)
            myTrades.Add(tr);

        await eventTrans.ToastClientShow(new()
        {
            HeadTitle = nameof(conLink.Connector.OwnTradeReceived),
            TypeMessage = MessagesTypesEnum.Success,
            MessageText = tr.ToString()
        });
    }

    async void OrderBookReceivedHandle(Subscription subscription, IOrderBookMessage orderBM)
    {
        await eventTrans.ToastClientShow(new()
        {
            HeadTitle = nameof(conLink.Connector.OrderBookReceived),
            TypeMessage = MessagesTypesEnum.Info,
            MessageText = orderBM.SecurityId.ToString()
        });
        Security sec = conLink.Connector.Securities.First(s => s.ToSecurityId() == orderBM.SecurityId);
        lock (OderBookList)
        {
            if (SecuritiesBonds(true).Contains(sec))
            {
                if (OderBookList.ContainsKey(sec))
                    OderBookList[sec] = orderBM;
                else
                    OderBookList.Add(sec, orderBM);
            }
        }
        OnProcessOutOfRangeCheck(subscription, orderBM);
    }

    async void OnProcessOutOfRangeCheck(Subscription subscription, IOrderBookMessage depth)
    {
        decimal ofrVolume;

        Security sec = conLink.Connector.Securities.First(s => s.ToSecurityId() == depth.SecurityId);
        InstrumentTradeStockSharpModel _sec = new InstrumentTradeStockSharpModel().Bind(sec);
        //..................................
        // For bonds
        //...................................

        SecurityPosition? SbPos = SBondPositionsList.FirstOrDefault(sp => sp.Sec.Equals(sec));

        if (SbPos is null)
            return;

        if (SbPos.LowLimit > SbPos.HighLimit)
            return;

        IEnumerable<Order> Orders = [.. AllOrders
            .Where(s => (s.State == OrderStates.Active) && (s.Security.Code == sec.Code) && (s.Comment is not null) && s.Comment.ContainsIgnoreCase("Ofr"))];

        if (!Orders.Any() && (Orders.Count() > 4))
            return;

        QuoteChange? bBid = depth.GetBestBid();
        QuoteChange? bAsk = depth.GetBestAsk();

        if (bBid is null || bAsk is null)
            return;
        string headTitle = $"err [{nameof(OnProcessOutOfRangeCheck)}]", msg;
        if (CurveCurrent is null)
        {
            msg = "Curve is null";
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = headTitle,
                MessageText = msg,
                TypeMessage = MessagesTypesEnum.Error
            });
            _logger.LogError(msg);
            return;
        }

        SBond? _secNode = CurveCurrent.GetNode(_sec);

        if (_secNode is null)
        {
            msg = "CurveBondNode is null";
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = headTitle,
                MessageText = msg,
                TypeMessage = MessagesTypesEnum.Error
            });
            _logger.LogError(msg);
            return;
        }

        TResponseModel<List<InstrumentTradeStockSharpViewModel>> resInstruments = dataRepo.ReadTradeInstrumentsAsync().Result;

        if (resInstruments.Response is null || resInstruments.Response.Count == 0)
        {
            msg = "The instruments are not configured.";
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = headTitle,
                MessageText = msg,
                TypeMessage = MessagesTypesEnum.Error
            });
            _logger.LogError(msg);
            return;
        }
        InstrumentTradeStockSharpViewModel? currInstrument = resInstruments.Response.FirstOrDefault(x => x.IdRemote == sec.Id);
        if (currInstrument is null)
        {
            msg = $"Security [{sec}] not configured in trade strategy";
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = headTitle,
                MessageText = msg,
                TypeMessage = MessagesTypesEnum.Error
            });
            _logger.LogError(msg);
            return;
        }

        List<DashboardTradeStockSharpModel> dataParse = ReadDashboard([currInstrument.Id]).Result;

        if (dataParse.Count == 0)
        {
            msg = "Dashboard - not set";
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = headTitle,
                MessageText = msg,
                TypeMessage = MessagesTypesEnum.Error
            });
            _logger.LogError(msg);
            return;
        }
        bool? _IsMarketMaker = currInstrument.Markers?.Any(x => x.MarkerDescriptor == (int)MarkersInstrumentStockSharpEnum.IsMarketMaker);

        if (bBid.Value.Price > _secNode.ModelPrice + SbPos.LowLimit + SbPos.HighLimit)
        {
            ofrVolume = 20000;
            if (bBid.Value.Price > _secNode.ModelPrice + 2 * SbPos.HighLimit)
                ofrVolume = 30000;
            if (bBid.Value.Price > _secNode.ModelPrice + 3 * SbPos.HighLimit)
                ofrVolume = 50000;

            if (bBid.Value.Volume < ofrVolume)
                ofrVolume = bBid.Value.Volume;

            DeleteAllQuotesByStrategy("Quote");
            DeleteAllQuotesByStrategy("Size");

            Order ord = new()
            {
                Security = sec,
                Portfolio = PortfolioCurrent,
                Price = bBid.Value.Price,
                Side = Sides.Sell,
                Comment = "OfRStrategy",
                IsMarketMaker = _IsMarketMaker,
                Volume = ofrVolume,
                ClientCode = ClientCodeStockSharp,
            };

            conLink.Connector.RegisterOrder(ord);
            conLink.Connector.AddWarningLog(string.Format("Order sell registered for OfRStrategy: ins ={0}, price = {1}, volume = {2}", sec, ord.Price, ord.Volume));
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = nameof(OnProcessOutOfRangeCheck),
                MessageText = $"OfR Detected! {sec.Id}",
                TypeMessage = MessagesTypesEnum.Success
            });
        }
        else if (bAsk.Value.Price < _secNode.ModelPrice - SbPos.LowLimit - SbPos.HighLimit)
        {
            ofrVolume = 20000;
            if (bAsk.Value.Price < _secNode.ModelPrice - 2 * SbPos.HighLimit)
                ofrVolume = 30000;
            if (bAsk.Value.Price < _secNode.ModelPrice - 3 * SbPos.HighLimit)
                ofrVolume = 50000;

            if (bAsk.Value.Volume < ofrVolume)
                ofrVolume = bAsk.Value.Volume;

            DeleteAllQuotesByStrategy("Quote");
            DeleteAllQuotesByStrategy("Size");

            Order ord = new()
            {
                Security = sec,
                Portfolio = PortfolioCurrent,
                Price = bAsk.Value.Price,
                Side = Sides.Buy,
                Comment = "OfRStrategy",
                IsMarketMaker = _IsMarketMaker,
                Volume = ofrVolume,
                ClientCode = ClientCodeStockSharp,
            };

            conLink.Connector.RegisterOrder(ord);
            conLink.Connector.AddWarningLog("Order buy registered for OfRStrategy: ins ={0}, price = {1}, volume = {2}", sec, ord.Price, ord.Volume);
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = nameof(OnProcessOutOfRangeCheck),
                MessageText = string.Format("Order buy registered for OfRStrategy: ins ={0}, price = {1}, volume = {2}", sec, ord.Price, ord.Volume),
                TypeMessage = MessagesTypesEnum.Success
            });
        }
    }

    async void LookupSecuritiesResultHandle(SecurityLookupMessage slm, IEnumerable<Security> securities, Exception ex)
    {
        string _msg;
        if (ex is not null)
        {
            _msg = $"Call > `{nameof(conLink.Connector.LookupSecuritiesResult)}`";
            await eventTrans.ToastClientShow(new()
            {
                HeadTitle = nameof(conLink.Connector.LookupSecuritiesResult),
                TypeMessage = MessagesTypesEnum.Error,
                MessageText = $"{_msg}\n{ex.Message}"
            });
            _logger.LogError(ex, _msg);
        }
        else
        {
            _msg = $"Call > `{nameof(conLink.Connector.LookupSecuritiesResult)}`";
            _logger.LogDebug(_msg);
            // eventTrans.ToastClientShow(new() { HeadTitle = nameof(conLink.Connector.LookupSecuritiesResult), TypeMessage = MessagesTypesEnum.Info, MessageText = _msg });
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


    void UnregisterEvents()
    {
        conLink.Unsubscribe();

        conLink.Connector.LookupSecuritiesResult -= LookupSecuritiesResultHandle;
        conLink.Connector.OrderBookReceived -= OrderBookReceivedHandle;
        conLink.Connector.OrderReceived -= OrderReceivedHandle;
        conLink.Connector.OwnTradeReceived -= OwnTradeReceivedHandle;
        conLink.Connector.SecurityReceived -= SecurityReceivedHandle;
    }

    void RegisterEvents()
    {
        conLink.Subscribe();

        conLink.Connector.LookupSecuritiesResult += LookupSecuritiesResultHandle;
        conLink.Connector.OrderBookReceived += OrderBookReceivedHandle;
        conLink.Connector.OrderReceived += OrderReceivedHandle;
        conLink.Connector.OwnTradeReceived += OwnTradeReceivedHandle;
        conLink.Connector.SecurityReceived += SecurityReceivedHandle;
    }
    #endregion
}