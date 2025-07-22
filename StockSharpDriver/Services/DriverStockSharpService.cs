////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using StockSharp.BusinessEntities;
using StockSharp.Fix.Quik.Lua;
using StockSharp.Messages;
using Ecng.Collections;
using Newtonsoft.Json;
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
    IDataStockSharpService DataRepo,
    IParametersStorage storageRepo,
    IEventsStockSharp eventTrans,
    IMemoryCache memoryCache,
    ConnectionLink conLink) : IDriverStockSharpService
{
    #region prop`s
    List<SecurityLookupMessage> SecuritiesCriteriaCodesFilterLookup = [];
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

    string ProgramDataPath;
    string ClientCodeStockSharp;
    string SecurityCriteriaCodeFilter;
    readonly List<Subscription> DepthSubscriptions = [];

    decimal
        quoteSmallStrategyBidVolume = 2000,
        quoteSmallStrategyOfferVolume = 2000,

        quoteSizeStrategyVolume = 2000,
        quoteStrategyVolume = 1000,
        skipVolume = 2500;

    decimal bondPositionTraded,
        bondSizePositionTraded,
        bondSmallPositionTraded,
        bondOutOfRangePositionTraded;

    readonly List<Order> AllOrders = [];

    readonly Dictionary<string, Order>
        _ordersForQuoteBuyReregister = [],
        _ordersForQuoteSellReregister = [];

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
    #endregion
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
    public async Task<ResponseBaseModel> InitialLoad(InitialLoadRequestModel req, CancellationToken cancellationToken = default)
    {
        SBond SBnd;
        DateTime curDate;
        decimal BndPrice, bondDV;

        if (!SecuritiesBonds().Any())
            return ResponseBaseModel.CreateError($"!{nameof(SecuritiesBonds)}().Any()");

        OfzCurve = new Curve(MyHelper.GetNextWorkingDay(DateTime.Today, 1, ProgramDataPath + "RedArrowData.db"));
        OfzCurve.GetCurveFromDb(ProgramDataPath + "RedArrowData.db", conLink.Connector, Board);

        if (OfzCurve.Length == 0)
            return ResponseBaseModel.CreateError("OfzCurve.Length == 0");

        //quoteStrategyVolume = (decimal)QuoteVolume.Value;
        //quoteSizeStrategyVolume = (decimal)QuoteSizeVolume.Value;
        //skipVolume = (decimal)SkipSizeVolume.Value;

        curDate = MyHelper.GetNextWorkingDay(DateTime.Today, 1, ProgramDataPath + "RedArrowData.db");

        //CurrentSecurities.ForEach(security =>
        //{
        //    string bndName = security.Code.Substring(2, 5);

        //    BndPrice = OfzCurve.GetNode(security).ModelPrice;
        //    DecimalUpDown decUpD = (DecimalUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "Price_" + bndName);

        //    if (!decUpD.IsNull())
        //    {
        //        decUpD.Value = BndPrice;
        //    }

        //    LongUpDown SmallBidVolUpD = (LongUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "SmallBidVolume_" + bndName);

        //    if (!SmallBidVolUpD.IsNull())
        //        SmallBidVolUpD.Value = (long)quoteSmallStrategyBidVolume;

        //    LongUpDown SmallOfferVolUpD = (LongUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "SmallOfferVolume_" + bndName);

        //    if (!SmallOfferVolUpD.IsNull())
        //        SmallOfferVolUpD.Value = (long)quoteSmallStrategyOfferVolume;

        //    LongUpDown WorkVolUpD = (LongUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "WorkingVolume_" + bndName);

        //    if (!WorkVolUpD.IsNull())
        //        WorkVolUpD.Value = (long)quoteStrategyVolume;

        //    IntegerUpDown SmallOffset = (IntegerUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "SmallOffset_" + bndName);

        //    if (!SmallOffset.IsNull())
        //        SmallOffset.Value = 0;

        //    IntegerUpDown Offset = (IntegerUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "Offset_" + bndName);

        //    if (!Offset.IsNull())
        //        Offset.Value = 0;

        //    IntegerUpDown Lowlimit = (IntegerUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "LowLimit_" + bndName);
        //    IntegerUpDown Highlimit = (IntegerUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "HighLimit_" + bndName);

        //    SBnd = SBondList.FirstOrDefault(s => s.UnderlyingSecurity.Code == security.Code);

        //    if (!SBnd.IsNull())
        //    {
        //        decimal yield = SBnd.GetYieldForPrice(curDate, BndPrice / 100);

        //        if (yield > 0)  //Regular bonds
        //        {
        //            if (!Lowlimit.IsNull())
        //            {
        //                Lowlimit.Value =
        //                    (int)
        //                        ((BndPrice / 100 - SBnd.GetPriceFromYield(curDate, yield + lowYieldLimit / 10000, true)) *
        //                         10000);

        //                if (Lowlimit.Value < 9)
        //                    Lowlimit.Value = 9;
        //                if (Lowlimit.Value > lowLimit * 100)
        //                    Lowlimit.Value = (int)(lowLimit * 100);
        //            }

        //            if (!Highlimit.IsNull())
        //            {
        //                Highlimit.Value =
        //                    (int)
        //                        ((BndPrice / 100 - SBnd.GetPriceFromYield(curDate, yield + highYieldLimit / 10000, true)) *
        //                         10000);

        //                if (Highlimit.Value < 11)
        //                    Highlimit.Value = 11;
        //                if (Highlimit.Value > highLimit * 100)
        //                    Highlimit.Value = (int)(highLimit * 100);
        //            }

        //            if ((SBnd.Maturity - curDate).Days < 400)
        //                WorkVolUpD.Value = (long?)quoteStrategyVolume;
        //            else if ((SBnd.Maturity - curDate).Days < 1100)
        //                WorkVolUpD.Value = (long?)quoteStrategyVolume;
        //            else if ((SBnd.Maturity - curDate).Days < 1500)
        //                WorkVolUpD.Value = (long?)quoteStrategyVolume;
        //            else
        //                WorkVolUpD.Value = (long?)quoteStrategyVolume;
        //        }
        //        else
        //        {
        //            if (OfzCodesIlliquid.Contains(SBnd.UnderlyingSecurity.Code))
        //            {
        //                if ((SBnd.Maturity - curDate).Days < 300)
        //                {
        //                    WorkVolUpD.Value = 1000;
        //                    Lowlimit.Value = (int)(lowLimit * 100);
        //                    Highlimit.Value = (int)(highLimit * 100);
        //                }
        //                else
        //                {
        //                    WorkVolUpD.Value = 1000;
        //                    Lowlimit.Value = (int)(lowLimit * 2 * 100);
        //                    Highlimit.Value = (int)(highLimit * 2 * 100);
        //                }
        //            }
        //            else
        //            {
        //                if ((SBnd.Maturity - curDate).Days < 500)
        //                {
        //                    WorkVolUpD.Value = 2000;
        //                    Lowlimit.Value = (int)(lowLimit / 2 * 100);
        //                    Highlimit.Value = (int)(highLimit / 2 * 100);
        //                }

        //                else if ((SBnd.Maturity - curDate).Days < 2000)
        //                {
        //                    WorkVolUpD.Value = 2000;
        //                    Lowlimit.Value = (int)(lowLimit / 1.5m * 100);
        //                    Highlimit.Value = (int)(highLimit / 1.5m * 100);
        //                }

        //                else
        //                {
        //                    WorkVolUpD.Value = 2000;
        //                    Lowlimit.Value = (int)(lowLimit / 1.5m * 100);
        //                    Highlimit.Value = (int)(highLimit / 1.5m * 100);
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (!Lowlimit.IsNull())
        //            Lowlimit.Value = (int)(lowLimit * 100);

        //        if (!Highlimit.IsNull())
        //            Highlimit.Value = (int)(highLimit * 100);
        //    }
        //});

        //btnStart.IsEnabled = true;
        //X2.IsEnabled = true;
        //Del2.IsEnabled = true;
        //SPlus.IsEnabled = true;
        //SMinus.IsEnabled = true;
        //Reset_All.IsEnabled = true;

        //CurrentSecurities.ForEach(security =>
        //{
        //    string bndName = security.Code.Substring(2, 5);
        //    Button btnRst = (Button)LogicalTreeHelper.FindLogicalNode(MyProgram, "Reset_" + bndName);

        //    if (!btnRst.IsNull())
        //        btnRst.IsEnabled = true;
        //});
        return ResponseBaseModel.CreateError(nameof(NotImplementedException));
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> StartStrategy(StrategyStartRequestModel req, CancellationToken cancellationToken = default)
    {
        AboutConnectResponseModel _ac = await AboutConnection(cancellationToken);
        if (_ac.ConnectionState != ConnectionStatesEnum.Connected)
            return ResponseBaseModel.CreateError($"{nameof(_ac.ConnectionState)}: {_ac.ConnectionState} ({_ac.ConnectionState.DescriptionInfo()})");

        ClearStrategy();

        ProgramDataPath = await storageRepo.ReadAsync<string>(GlobalStaticCloudStorageMetadata.ProgramDataPathStockSharp, cancellationToken);
        if (!string.IsNullOrWhiteSpace(ProgramDataPath))
            return ResponseBaseModel.CreateError($"{nameof(ProgramDataPath)} - not set");

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

            SBondPositionsList.Add(new SecurityPosition(security, "Quote", currentStrategy.LowLimit / 100, currentStrategy.HightLimit / 100, currentStrategy.ValueOperation, currentStrategy.ValueOperation, currentStrategy.Offset / 100));

            if (currentStrategy.IsSmall)
                SBondSmallPositionsList.Add(new SecurityPosition(security, "Small", (decimal)0.0301, (currentStrategy.LowLimit - (decimal)0.1) / 100, currentStrategy.SmallBidVolume, currentStrategy.SmallOfferVolume, currentStrategy.SmallOffset / 100));

            if (tryFindInstrument[0].Markers.Any(x => x.MarkerDescriptor == MarkersInstrumentStockSharpEnum.Illiquid))
                SBondSizePositionsList.Add(new SecurityPosition(security, "Size", (currentStrategy.HightLimit + (decimal)0.1) / 100, (currentStrategy.LowLimit + currentStrategy.HightLimit) / 100, quoteSizeStrategyVolume, quoteSizeStrategyVolume, 0m));
        }
        if (!response.Success())
        {
            ClearStrategy();
            return response;
        }

        lock (DepthSubscriptions)
        {
            DepthSubscriptions.Clear();
            bl.ForEach(RegisterMarketDepth);
        }
        void RegisterMarketDepth(Security security)
        {
            Subscription depthSubscription = new(DataType.MarketDepth, security);
            conLink.Connector.Subscribe(depthSubscription);
            DepthSubscriptions.Add(depthSubscription);
        }
        conLink.Connector.OrderBookReceived += MarketDepthOrderBookHandle;

        if (OfzCurve is null || OfzCurve.Length == 0)
        {
            //ClearStrategy();
            //return ResponseBaseModel.CreateError("OfzCurve.Length == 0");
        }

        lock (_ordersForQuoteBuyReregister)
            _ordersForQuoteBuyReregister.Clear();
        lock (_ordersForQuoteSellReregister)
            _ordersForQuoteSellReregister.Clear();

        return ResponseBaseModel.CreateInfo("Ok");
    }

    public async Task<ResponseBaseModel> ResetStrategy(ResetStrategyRequestModel req, CancellationToken cancellationToken = default)
    {
        if (req.InstrumentId < 1)
        {
            quoteSizeStrategyVolume = req.Size;
            quoteStrategyVolume = req.Volume;
            skipVolume = req.Skip;

            DeleteAllQuotesByStrategy("Size");
            DeleteAllQuotesByStrategy("Small");
            DeleteAllQuotesByStrategy("Quote");

            lock (SBondPositionsList)
                SBondPositionsList.Clear();
            lock (SBondSizePositionsList)
                SBondSizePositionsList.Clear();
            lock (SBondSmallPositionsList)
                SBondSmallPositionsList.Clear();

            List<Security> currentSecurities = SecuritiesBonds();
            if (!currentSecurities.Any())
                return ResponseBaseModel.CreateError("BondList - not any");

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

                StrategyTradeStockSharpModel currentStrategy = dataParse[_fx];

                Security currentSecurity = currentSecurities.FirstOrDefault(x =>
                x.Code == currentStrategy.Code &&
                (int)x.Currency == currentStrategy.Currency &&
                x.Board.Code == currentStrategy.Board.Code);

                if (currentSecurity is null)
                {
                    msg = $"Security - not found for strategy: {JsonConvert.SerializeObject(currentStrategy, Formatting.Indented)}";
                    _logger.LogError(msg);
                    return ResponseBaseModel.CreateError(msg);
                }

                if (!currentStrategy.IsAlter)
                {
                    decimal WorkVol = currentStrategy.WorkingVolume;
                    decimal SmallBidVol = currentStrategy.SmallBidVolume;
                    decimal SmallOfferVol = currentStrategy.SmallOfferVolume;
                    decimal LowLimit = currentStrategy.LowLimit;
                    decimal Highlimit = currentStrategy.HightLimit;
                    decimal SmallOffset = currentStrategy.SmallOffset;
                    decimal Offset = currentStrategy.Offset;
                    bool IsSmall = currentStrategy.IsSmall;

                    SBondPositionsList.Add(new SecurityPosition(currentSecurity, "Quote", LowLimit / 100,
                     Highlimit / 100, WorkVol, WorkVol, Offset / 100));

                    if (IsSmall)
                        SBondSmallPositionsList.Add(new SecurityPosition(currentSecurity, "Small", (decimal)(0.0301), (LowLimit - (decimal)0.1) / 100, SmallBidVol, SmallOfferVol, SmallOffset / 100));

                    if (!instrument.Markers.Any(x => x.MarkerDescriptor == MarkersInstrumentStockSharpEnum.Illiquid))
                        SBondSizePositionsList.Add(new SecurityPosition(currentSecurity, "Size", (Highlimit + (decimal)0.1) / 100, (LowLimit + Highlimit) / 100, quoteSizeStrategyVolume, quoteSizeStrategyVolume, 0m));
                }
                else
                {
                    SBondPositionsList.Add(new SecurityPosition(currentSecurity, "Quote", lowLimit, highLimit, quoteStrategyVolume, quoteStrategyVolume, 0m));

                    if (!instrument.Markers.Any(x => x.MarkerDescriptor == MarkersInstrumentStockSharpEnum.Illiquid))
                        SBondSizePositionsList.Add(new SecurityPosition(currentSecurity, "Size", highLimit, lowLimit + highLimit, quoteSizeStrategyVolume, quoteSizeStrategyVolume, 0m));
                }

                Subscription sub = conLink.Connector.FindSubscriptions(currentSecurity, DataType.MarketDepth).Where(s => s.SubscriptionMessage.To == null && s.State.IsActive()).FirstOrDefault();
                OrderBookReceivedConnectorMan(sub, OderBookList[currentSecurity]);
            }
        }
        else
        {
            TResponseModel<List<InstrumentTradeStockSharpViewModel>> readInstrument = await DataRepo.GetInstrumentsAsync([req.InstrumentId], cancellationToken);


            //    SecurityPosition SbPos = SBondPositionsList.FirstOrDefault(sp => sp.Sec.Code.ContainsIgnoreCase(bondName));
            //    if (!SbPos.IsNull())
            //        SBondPositionsList.Remove(SbPos);

            //    SecurityPosition SbSizePos = SBondSizePositionsList.FirstOrDefault(sp => sp.Sec.Code.ContainsIgnoreCase(bondName));
            //    if (!SbSizePos.IsNull())
            //        SBondSizePositionsList.Remove(SbSizePos);

            //    SecurityPosition SbSmallPos = SBondSmallPositionsList.FirstOrDefault(sp => sp.Sec.Code.ContainsIgnoreCase(bondName));
            //    if (!SbSmallPos.IsNull())
            //        SBondSmallPositionsList.Remove(SbSmallPos);

            //    Security currentSecurity = CurrentSecurities.FirstOrDefault(sec => sec.Code.ContainsIgnoreCase(bondName));

            //    DecimalUpDown decUpD = (DecimalUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "Price_" + bondName);

            //    if (!decUpD.IsNull())
            //    {
            //        long? WorkVol = ((LongUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "WorkingVolume_" + bondName)).Value;
            //        long? SmallBidVol = ((LongUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "SmallBidVolume_" + bondName)).Value;
            //        long? SmallOfferVol = ((LongUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "SmallOfferVolume_" + bondName)).Value;
            //        int? LowLimit = ((IntegerUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "LowLimit_" + bondName)).Value;
            //        int? Highlimit = ((IntegerUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "HighLimit_" + bondName)).Value;
            //        int? SmallOffset = ((IntegerUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "SmallOffset_" + bondName)).Value;
            //        int? Offset = ((IntegerUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "Offset_" + bondName)).Value;
            //        bool? IsSmall = ((CheckBox)LogicalTreeHelper.FindLogicalNode(MyProgram, "IsMM_" + bondName)).IsChecked;

            //        SBondPositionsList.Add(new SecurityPosition(currentSecurity, "Quote", (decimal)LowLimit / 100, (decimal)Highlimit / 100, (decimal)WorkVol, (decimal)WorkVol, (decimal)Offset / 100));

            //        if ((bool)IsSmall)
            //            SBondSmallPositionsList.Add(new SecurityPosition(currentSecurity, "Small", (decimal)(0.0301), (decimal)(Highlimit - 0.1) / 100, (decimal)SmallBidVol, (decimal)SmallOfferVol, (decimal)SmallOffset / 100));

            //        if (OfzCodes.Contains(currentSecurity.Code) || OfzCodesNew.Contains(currentSecurity.Code))
            //            SBondSizePositionsList.Add(new SecurityPosition(currentSecurity, "Size", (decimal)(Highlimit + 0.1) / 100, (decimal)(LowLimit + Highlimit) / 100, quoteSizeStrategyVolume, quoteSizeStrategyVolume, 0m));
            //    }
            //    else
            //    {
            //        SBondPositionsList.Add(new SecurityPosition(currentSecurity, "Quote", lowLimit, highLimit, quoteStrategyVolume, quoteStrategyVolume, 0m));

            //        if (OfzCodes.Contains(currentSecurity.Code) || OfzCodesNew.Contains(currentSecurity.Code))
            //            SBondSizePositionsList.Add(new SecurityPosition(currentSecurity, "Size", highLimit, lowLimit + highLimit, quoteSizeStrategyVolume, quoteSizeStrategyVolume, 0m));
            //    }

            //    Subscription sub = conLink.Connector.FindSubscriptions(currentSecurity, DataType.MarketDepth).Where(s => s.SubscriptionMessage.To == null && s.State.IsActive()).FirstOrDefault();
            //    OrderBookReceivedConnectorMan(sub, OderBookList[currentSecurity]);
        }

        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    void OrderBookReceivedConnectorMan(Subscription subscription, IOrderBookMessage depth)
    {
        TPaginationResponseModel<InstrumentTradeStockSharpViewModel> resInstruments = DataRepo.InstrumentsSelectAsync(new()
        {
            PageNum = 0,
            PageSize = int.MaxValue,
            FavoriteFilter = true,
        }).Result;
        string _msg;
        if (resInstruments.Response is null || resInstruments.Response.Count == 0)
        {
            _msg = $"The instruments are not configured.";
            _logger.LogError(_msg);
            eventTrans.ToastClientShow(new() { HeadTitle = nameof(OrderBookReceivedConnectorMan), MessageText = _msg, TypeMessage = MessagesTypesEnum.Error });
            return;
        }
        List<StrategyTradeStockSharpModel> dataParse = ReadStrategies([.. resInstruments.Response.Select(x => x.Id)]).Result;

        if (dataParse.Count == 0)
        {
            _msg = "Dashboard - not set";

            _logger.LogError(_msg);
            eventTrans.ToastClientShow(new() { HeadTitle = nameof(OrderBookReceivedConnectorMan), MessageText = _msg, TypeMessage = MessagesTypesEnum.Error });
            return;
        }

        Order tmpOrder;
        Order newOrder;
        IOrderBookMessage tmpDepth;
        decimal price;

        Security sec = conLink.Connector.Securities.FirstOrDefault(s => s.ToSecurityId() == depth.SecurityId);
        SecurityPosition SbPos = SBondPositionsList.FirstOrDefault(sp => sp.Sec.Equals(sec));

        InstrumentTradeStockSharpViewModel currentInstrument = resInstruments.Response
            .FirstOrDefault(x => x.Code == sec.Code && x.Currency == (int)sec.Currency && x.Board.Code == sec.Board.Code);

        if (currentInstrument is null)
        {
            _msg = $"Instrument not found - {JsonConvert.SerializeObject(sec, Formatting.Indented)}";
            _logger.LogError(_msg);
            eventTrans.ToastClientShow(new() { HeadTitle = nameof(OrderBookReceivedConnectorMan), MessageText = _msg, TypeMessage = MessagesTypesEnum.Error });
            return;
        }

        StrategyTradeStockSharpModel currentStrategy = dataParse.FirstOrDefault(x => x.Id == currentInstrument.Id);
        if (currentStrategy is null)
        {
            _msg = $"Strategy not found - {JsonConvert.SerializeObject(currentInstrument, Formatting.Indented)}";
            _logger.LogError(_msg);
            eventTrans.ToastClientShow(new() { HeadTitle = nameof(OrderBookReceivedConnectorMan), MessageText = _msg, TypeMessage = MessagesTypesEnum.Error });
            return;
        }
        bool isMarketMaker = currentInstrument.Markers.Any(x => x.MarkerDescriptor == MarkersInstrumentStockSharpEnum.IsMarketMaker);
        if (!SbPos.IsNull() && !sec.IsNull())
        {
            if (!_ordersForQuoteBuyReregister.ContainsKey(sec.Code) && !depth.Bids.IsNull() && !AllOrders.Any(s => ((s.State == OrderStates.Pending) && (!s.Comment.IsNull()) && (s.Comment.ContainsIgnoreCase("Quote")) && (s.Security.Code == sec.Code) && (s.Side == Sides.Buy))))
            {
                IEnumerable<Order> Orders = AllOrders.Where(s => (s.State == OrderStates.Active) && (s.Security.Code == sec.Code) && (!s.Comment.IsNull()) && (s.Comment.ContainsIgnoreCase("Quote")) && (s.Side == Sides.Buy));

                if (Orders.IsEmpty()) //if there is no orders in stakan
                {
                    price = MyHelper.GetBestConditionPrice(sec, depth, OfzCurve.GetNode(sec).ModelPrice + SbPos.Offset, -SbPos.LowLimit, -SbPos.HighLimit, 2.101m * SbPos.BidVolume);
                    if (price > 0)
                    {
                        Order ord = new()
                        {
                            Security = sec,
                            Portfolio = SelectedPortfolio,
                            Price = price,
                            Side = Sides.Buy,
                            Comment = "Quote",
                            IsMarketMaker = isMarketMaker,
                            Volume = SbPos.BidVolume,
                            ClientCode = ClientCodeStockSharp,
                        };
                        conLink.Connector.RegisterOrder(ord);
                        eventTrans.ToastClientShow(new() { HeadTitle = $"`{nameof(OrderBookReceivedConnectorMan)}` there is no orders in MarketDepth", MessageText = string.Format("Order buy registered new: ins ={0}, price = {1}, volume = {2}", sec, price, SbPos.BidVolume), TypeMessage = MessagesTypesEnum.Warning });
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

                    price = MyHelper.GetBestConditionPrice(sec, tmpDepth, OfzCurve.GetNode(sec).ModelPrice + SbPos.Offset, -SbPos.LowLimit, -SbPos.HighLimit, 2.101m * SbPos.BidVolume);

                    if ((price > 0) && ((price != tmpOrder.Price) || (tmpOrder.Balance != SbPos.BidVolume)))
                    {
                        newOrder = new Order()
                        {
                            Security = sec,
                            Portfolio = SelectedPortfolio,
                            Price = price,
                            Side = Sides.Buy,
                            Comment = "Quote",
                            IsMarketMaker = isMarketMaker,
                            Volume = SbPos.BidVolume,
                            ClientCode = ClientCodeStockSharp,
                        };
                        _ordersForQuoteBuyReregister.Add(tmpOrder.Security.Code, newOrder);
                        eventTrans.ToastClientShow(new() { HeadTitle = $"`{nameof(OrderBookReceivedConnectorMan)}` with orders (x {Orders.Count()}) in MarketDepth", MessageText = string.Format("Order buy cancelled for reregister: ins ={0}, price = {1}, volume = {2}", sec, tmpOrder.Price, tmpOrder.Volume), TypeMessage = MessagesTypesEnum.Warning });
                        conLink.Connector.CancelOrder(tmpOrder);
                    }

                    Orders.Skip(1).ForEach(s =>
                    {
                        eventTrans.ToastClientShow(new() { HeadTitle = "Skip order", MessageText = string.Format("Order buy duplication!"), TypeMessage = MessagesTypesEnum.Warning });
                        if (s.Id != tmpOrder.Id)
                        {
                            eventTrans.ToastClientShow(new() { HeadTitle = "Warning", MessageText = string.Format("Duplicate buy order cancelled: ins ={0}, price = {1}, volume = {2}", s.Security, s.Price, s.Volume), TypeMessage = MessagesTypesEnum.Warning });
                            //                                                       s.Security, s.Price, s.Volume);
                            conLink.Connector.CancelOrder(s);
                        }
                    });
                }
            }

            //only for sell orders
            if (!_ordersForQuoteSellReregister.ContainsKey(sec.Code) && !depth.Asks.IsNull() && !AllOrders.Any(s => ((s.State == OrderStates.Pending) && (!s.Comment.IsNull()) && (s.Comment.ContainsIgnoreCase("Quote")) && (s.Security.Code == sec.Code) && (s.Side == Sides.Sell))))
            {
                IEnumerable<Order> Orders = AllOrders.Where(s => (s.State == OrderStates.Active) && (s.Security.Code == sec.Code) && (!s.Comment.IsNull()) && (s.Comment.ContainsIgnoreCase("Quote")) && (s.Side == Sides.Sell));

                if (Orders.IsEmpty()) //if there is no orders in stakan
                {
                    price = MyHelper.GetBestConditionPrice(sec, depth, OfzCurve.GetNode(sec).ModelPrice + SbPos.Offset, SbPos.LowLimit, SbPos.HighLimit, 2.101m * SbPos.OfferVolume);
                    if (price > 0)
                    {
                        Order ord = new()
                        {
                            Security = sec,
                            Portfolio = SelectedPortfolio,
                            Price = price,
                            Side = Sides.Sell,
                            Comment = "Quote",
                            IsMarketMaker = isMarketMaker,
                            Volume = SbPos.OfferVolume,
                            ClientCode = ClientCodeStockSharp,
                        };

                        conLink.Connector.RegisterOrder(ord);
                        eventTrans.ToastClientShow(new() { HeadTitle = "Warning", MessageText = string.Format("Order sell registered new: ins ={0}, price = {1}, volume = {2}", sec, price, SbPos.OfferVolume), TypeMessage = MessagesTypesEnum.Warning });
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

                    price = MyHelper.GetBestConditionPrice(sec, tmpDepth, OfzCurve.GetNode(sec).ModelPrice + SbPos.Offset, SbPos.LowLimit, SbPos.HighLimit, 2.101m * SbPos.OfferVolume);

                    if ((price > 0) && ((price != tmpOrder.Price) || (tmpOrder.Balance != SbPos.OfferVolume)))
                    {
                        newOrder = new Order()
                        {
                            Security = sec,
                            Portfolio = SelectedPortfolio,
                            Price = price,
                            Side = Sides.Sell,
                            Comment = "Quote",
                            IsMarketMaker = isMarketMaker,
                            Volume = SbPos.OfferVolume,
                            ClientCode = ClientCodeStockSharp,
                        };
                        _ordersForQuoteSellReregister.Add(tmpOrder.Security.Code, newOrder);
                        eventTrans.ToastClientShow(new() { HeadTitle = "Warning", MessageText = string.Format(" Order sell cancelled for reregister: ins ={0}, price = {1}, volume = {2}", sec, tmpOrder.Price, tmpOrder.Volume), TypeMessage = MessagesTypesEnum.Warning });
                        conLink.Connector.CancelOrder(tmpOrder);
                    }

                    Orders.Skip(1).ForEach(s =>
                    {
                        eventTrans.ToastClientShow(new() { HeadTitle = "Warning", MessageText = string.Format("Order sell duplication!"), TypeMessage = MessagesTypesEnum.Warning });
                        if (s.Id != tmpOrder.Id)
                        {
                            eventTrans.ToastClientShow(new() { HeadTitle = "Warning", MessageText = string.Format("Duplicate sell order cancelled: ins ={0}, price = {1}, volume = {2}", s.Security, s.Price, s.Volume), TypeMessage = MessagesTypesEnum.Warning });
                            conLink.Connector.CancelOrder(s);
                        }
                    });
                }
            }
        }
    }


    /// <inheritdoc/>
    public Task<ResponseBaseModel> StopStrategy(StrategyStopRequestModel req, CancellationToken cancellationToken = default)
    {
        ClearStrategy();
        return Task.FromResult(ResponseBaseModel.CreateInfo("Ok"));
    }

    private void MarketDepthOrderBookHandle(Subscription subscription, IOrderBookMessage depth)
    {
        _logger.LogInformation($"Call `{nameof(MarketDepthOrderBookHandle)}` > Стакан: {depth.SecurityId}, Время: {depth.ServerTime}");
        lock (DepthSubscriptions)
            if (!DepthSubscriptions.Any(x => x.SecurityId == subscription.SecurityId))
                return;

        eventTrans.ToastClientShow(new() { HeadTitle = nameof(MarketDepthOrderBookHandle), TypeMessage = MessagesTypesEnum.Info, MessageText = $"Стакан: {depth.SecurityId}, Время: {depth.ServerTime}" });

        // Обработка стакана
        Console.WriteLine($"Стакан: {depth.SecurityId}, Время: {depth.ServerTime}");
        Console.WriteLine($"Покупки (Bids): {depth.Bids.Length}, Продажи (Asks): {depth.Asks.Length}");
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
            //    IntegerUpDown LowLimit = (IntegerUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "LowLimit_" + bndName);
            //    IntegerUpDown Highlimit = (IntegerUpDown)LogicalTreeHelper.FindLogicalNode(MyProgram, "HighLimit_" + bndName);
            //    LowLimit.Value = LowLimit.Value / 2;
            //    Highlimit.Value = Highlimit.Value / 2;
            //}
        });

        lowLimit = Calculation(lowLimit, req.Operator, req.Operand);
        highLimit = Calculation(highLimit, req.Operator, req.Operand);

        return ResponseBaseModel.CreateInfo($"ok - `{nameof(LimitsStrategiesUpdate)}`");
    }

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> Connect(ConnectRequestModel req, CancellationToken cancellationToken = default)
    {
        Board = null;
        if (SecuritiesBonds().Any())
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
                _logger.LogError(ex, $"Ошибка добавления адаптера");
                res.Messages.InjectException(ex);
            }
        });

        if (!conLink.Connector.CanConnect)
        {
            res.AddError("can`t connect");
            return res;
        }

        await Task.WhenAll([
                Task.Run(async () => { SecurityCriteriaCodeFilter = await storageRepo.ReadAsync<string>(GlobalStaticCloudStorageMetadata.SecuritiesCriteriaCodeFilterStockSharp, cancellationToken); }, cancellationToken),
                Task.Run(async () => { ClientCodeStockSharp = await storageRepo.ReadAsync<string>(GlobalStaticCloudStorageMetadata.ClientCodeBrokerStockSharp, cancellationToken); }, cancellationToken)
            ]);

        RegisterEvents();
        if (!string.IsNullOrWhiteSpace(SecurityCriteriaCodeFilter))
            conLink.Connector.SubscriptionsOnConnect.RemoveRange(conLink.Connector.SubscriptionsOnConnect.Where(x => x.DataType == DataType.Securities));

        await conLink.Connector.ConnectAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(SecurityCriteriaCodeFilter))
        {
            lock (SecuritiesCriteriaCodesFilterLookup)
            {
                SecuritiesCriteriaCodesFilterLookup.Clear();
                foreach (string _sc in Regex.Split(SecurityCriteriaCodeFilter, @"\s+").Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct())
                {
                    SecuritiesCriteriaCodesFilterLookup.Add(new()
                    {
                        SecurityId = new SecurityId
                        {
                            SecurityCode = _sc.Trim(),
                        },
                        TransactionId = conLink.Connector.TransactionIdGenerator.GetNextId()
                    });
                    SecurityCriteriaCodeFilterSubscription = new(SecuritiesCriteriaCodesFilterLookup.Last());
                    conLink.Connector.Subscribe(SecurityCriteriaCodeFilterSubscription);
                }
            }
        }

        res.AddInfo($"connection: {conLink.Connector.ConnectionState}");
        return res;
    }

    /// <inheritdoc/>
    public Task<ResponseBaseModel> Disconnect(CancellationToken cancellationToken = default)
    {
        ClearStrategy();
        conLink.Connector.CancelOrders();
        foreach (Subscription sub in conLink.Connector.Subscriptions)
        {
            conLink.Connector.UnSubscribe(sub);
            _logger.LogInformation($"{nameof(Connector.UnSubscribe)} > {sub.GetType().FullName}");
        }

        SecurityCriteriaCodeFilter = "";
        lock (SecuritiesCriteriaCodesFilterLookup)
            SecuritiesCriteriaCodesFilterLookup.Clear();

        UnregisterEvents();
        conLink.Connector.Disconnect();

        lock (AllSecurities)
            AllSecurities.Clear();
        return Task.FromResult(ResponseBaseModel.CreateInfo("connection closed"));
    }

    /// <inheritdoc/>
    public Task<AboutConnectResponseModel> AboutConnection(CancellationToken cancellationToken = default)
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
            SecurityCriteriaCodeFilterStockSharp = SecurityCriteriaCodeFilter,
            ClientCode = ClientCodeStockSharp,
            ProgramPath = ProgramDataPath
        };

        return Task.FromResult(res);
    }

    /// <inheritdoc/>
    public Task<ResponseBaseModel> OrderRegisterAsync(CreateOrderRequestModel req, CancellationToken cancellationToken = default)
    {
        ExchangeBoard board = req.Instrument.Board is null
            ? null
            : conLink.Connector.ExchangeBoards.FirstOrDefault(x => x.Code == req.Instrument.Board.Code && (x.Exchange.Name == req.Instrument.Board.Exchange.Name || x.Exchange.CountryCode.ToString() == req.Instrument.Board.Exchange.CountryCode.ToString()));

        Security currentSec = conLink.Connector.Securities.FirstOrDefault(x => x.Code == req.Instrument.Code && x.Board.Code == board.Code && x.Board.Exchange.CountryCode == board.Exchange.CountryCode);
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
            ClientCode = ClientCodeStockSharp
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
                res.AddWarning("Подтверждение просрочено. Повторите попытку! ");
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

    void OrderReceivedHandle(Subscription subscription, Order order)
    {
        eventTrans.ToastClientShow(new()
        {
            HeadTitle = nameof(conLink.Connector.OrderReceived),
            TypeMessage = MessagesTypesEnum.Info,
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

    void OwnTradeReceivedHandle(Subscription subscription, MyTrade tr)
    {
        lock (myTrades)
            myTrades.Add(tr);
    }

    void OrderBookReceivedHandle(Subscription subscription, IOrderBookMessage orderBM)
    {
        eventTrans.ToastClientShow(new() { HeadTitle = nameof(conLink.Connector.OrderBookReceived), TypeMessage = MessagesTypesEnum.Info, MessageText = orderBM.SecurityId.ToString() });
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
            //    ClientCode = ClientCodeStockSharp,
            //};

            //    conLink.Connector.RegisterOrder(ord);
            // eventTrans.ToastClientShow(new() { HeadTitle = "Warning", MessageText = string.Format("Order sell registered for OfRStrategy: ins ={0}, price = {1}, volume = {2}", sec, ord.Price, ord.Volume), TypeMessage = MessagesTypesEnum.Warning });

            //    // New logic???
            //    this.GuiAsync(() => System.Windows.MessageBox.Show(this, "OfR Detected! " + sec.Code));
            //    conLink.Connector.OrderBookReceived -= OrderBookReceivedConnector2;
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
            //        ClientCode = ClientCodeStockSharp,
            //    };

            //    conLink.Connector.RegisterOrder(ord);
            // eventTrans.ToastClientShow(new() { HeadTitle = "Warning", MessageText = string.Format("Order buy registered for OfRStrategy: ins ={0}, price = {1}, volume = {2}", sec, ord.Price, ord.Volume), TypeMessage = MessagesTypesEnum.Warning });

            //    // New logic???
            //    this.GuiAsync(() => System.Windows.MessageBox.Show(this, "OfR Detected! " + sec.Code));
            //    conLink.Connector.OrderBookReceived -= OrderBookReceivedConnector2;
        }
    }

    #region todo
    void OrderLogReceivedHandle(Subscription subscription, IOrderLogMessage order)
    {
        //_logger.LogWarning($"Call > `{nameof(OrderLogReceivedHandle)}`: {JsonConvert.SerializeObject(order)}");
    }

    void Level1ReceivedHandle(Subscription subscription, Level1ChangeMessage levelCh)
    {
        //_logger.LogWarning($"Call > `{nameof(Level1ReceivedHandle)}`: {JsonConvert.SerializeObject(levelCh)}");
    }

    void DataTypeReceivedHandle(Subscription subscription, DataType argDt)
    {
        //_logger.LogWarning($"Call > `{nameof(DataTypeReceivedHandle)}`: {JsonConvert.SerializeObject(argDt)}");
    }
    
    void CandleReceivedHandle(Subscription subscription, ICandleMessage candleMessage)
    {
        _logger.LogWarning($"Call > `{nameof(CandleReceivedHandle)}`");
    }
    #endregion

    void LookupSecuritiesResultHandle(SecurityLookupMessage slm, IEnumerable<Security> securities, Exception ex)
    {
        string _msg;
        if (ex is not null)
        {
            _msg = $"Call > `{nameof(conLink.Connector.LookupSecuritiesResult)}`";
            eventTrans.ToastClientShow(new() { HeadTitle = nameof(conLink.Connector.LookupSecuritiesResult), TypeMessage = MessagesTypesEnum.Error, MessageText = _msg });
            _logger.LogError(ex, _msg);
        }
        else
        {
            _msg = $"Call > `{nameof(conLink.Connector.LookupSecuritiesResult)}`";
            _logger.LogInformation(_msg);
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
    #endregion

    void DeleteAllQuotesByStrategy(string strategy)
    {
        IEnumerable<Order> orders = AllOrders.Where(s => s.State == OrderStates.Active);

        if (string.IsNullOrEmpty(strategy))
        {
            foreach (Order order in orders)
            {
                conLink.Connector.CancelOrder(order);
                eventTrans.ToastClientShow(new() { HeadTitle = $"`{nameof(DeleteAllQuotesByStrategy)}` without strategy", MessageText = string.Format("Order cancelled: ins ={0}, price = {1}, volume = {2}", order.Security, order.Price, order.Volume), TypeMessage = MessagesTypesEnum.Warning });
            }
        }
        else
        {
            foreach (Order order in orders)
            {
                if ((!string.IsNullOrEmpty(order.Comment)) && order.Comment.ContainsIgnoreCase(strategy))
                    conLink.Connector.CancelOrder(order);

                eventTrans.ToastClientShow(new() { HeadTitle = $"`{nameof(DeleteAllQuotesByStrategy)}` with strategy '{strategy}'", MessageText = string.Format("Order cancelled: ins ={0}, price = {1}, volume = {2}", order.Security, order.Price, order.Volume), TypeMessage = MessagesTypesEnum.Warning });
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

        conLink.Connector.OrderBookReceived -= MarketDepthOrderBookHandle;

        Board = null;
        SelectedPortfolio = null;
        ClientCodeStockSharp = null;
        ProgramDataPath = null;

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

    public Task<ResponseBaseModel> Terminate(CancellationToken cancellationToken = default)
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

        conLink.Connector.CandleReceived -= CandleReceivedHandle;
        conLink.Connector.DataTypeReceived -= DataTypeReceivedHandle;
        conLink.Connector.Level1Received -= Level1ReceivedHandle;
        conLink.Connector.LookupSecuritiesResult -= LookupSecuritiesResultHandle;
        conLink.Connector.OrderBookReceived -= OrderBookReceivedHandle;
        conLink.Connector.OrderLogReceived -= OrderLogReceivedHandle;
        conLink.Connector.OrderReceived -= OrderReceivedHandle;
        conLink.Connector.OwnTradeReceived -= OwnTradeReceivedHandle;
        conLink.Connector.SecurityReceived -= SecurityReceivedHandle;
    }

    void RegisterEvents()
    {
        conLink.Subscribe();

        conLink.Connector.CandleReceived += CandleReceivedHandle;
        conLink.Connector.DataTypeReceived += DataTypeReceivedHandle;
        conLink.Connector.Level1Received += Level1ReceivedHandle;
        conLink.Connector.LookupSecuritiesResult += LookupSecuritiesResultHandle;
        conLink.Connector.OrderBookReceived += OrderBookReceivedHandle;
        conLink.Connector.OrderLogReceived += OrderLogReceivedHandle;
        conLink.Connector.OrderReceived += OrderReceivedHandle;
        conLink.Connector.OwnTradeReceived += OwnTradeReceivedHandle;
        conLink.Connector.SecurityReceived += SecurityReceivedHandle;
    }
}