////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Transmission.Receives.StockSharpDriver;
using Transmission.Receives.storage;
using MQTTCallLib;
using SharedLib;

namespace StockSharpService;

/// <summary>
/// MQ listen
/// </summary>
public static class RegisterMqListenerExtension
{
    /// <summary>
    /// RegisterMqListeners
    /// </summary>
    public static IServiceCollection StockSharpRegisterMqListeners(this IServiceCollection services)
    {
        return services
            .RegisterMqListener<GoToPageForRowReceive, TPaginationRequestStandardModel<int>, TPaginationResponseModel<NLogRecordModelDB>>()
            .RegisterMqListener<MetadataLogsReceive, PeriodDatesTimesModel, TResponseModel<LogsMetadataResponseModel>>()
            .RegisterMqListener<LogsSelectReceive, TPaginationRequestStandardModel<LogsSelectRequestModel>, TPaginationResponseModel<NLogRecordModelDB>>()
            .RegisterMqListener<AboutConnectStockSharpDriverReceive, object, AboutConnectResponseModel>()
            .RegisterMqListener<DisconnectStockSharpDriverReceive, object, ResponseBaseModel>()
            .RegisterMqListener<ConnectStockSharpDriverReceive, ConnectRequestModel, ResponseBaseModel>()
            .RegisterMqListener<UpdateOrCreateAdapterStockSharpDriverReceive, FixMessageAdapterModelDB, TResponseModel<FixMessageAdapterModelDB>>()
            .RegisterMqListener<AdaptersSelectStockSharpDriverReceive, TPaginationRequestStandardModel<AdaptersRequestModel>, TPaginationResponseModel<FixMessageAdapterModelDB>>()
            .RegisterMqListener<AdaptersGetStockSharpDriverReceive, int[], TResponseModel<FixMessageAdapterModelDB[]>>()
            .RegisterMqListener<DeleteAdapterStockSharpDriverReceive, FixMessageAdapterModelDB, ResponseBaseModel>()
            .RegisterMqListener<PingStockSharpDriverReceive, object, ResponseBaseModel>()
            .RegisterMqListener<GetPortfoliosStockSharpDriverReceive, int[], TResponseModel<List<PortfolioStockSharpViewModel>>>()
            .RegisterMqListener<GetBoardsStockSharpDriverReceive, int[], TResponseModel<List<BoardStockSharpModel>>>()
            .RegisterMqListener<SetMarkersForInstrumentStockSharpDriverReceive, SetMarkersForInstrumentRequestModel, ResponseBaseModel>()
            .RegisterMqListener<GetExchangesStockSharpDriverReceive, int[], TResponseModel<List<ExchangeStockSharpModel>>>()
            .RegisterMqListener<GetInstrumentsStockSharpDriverReceive, int[], TResponseModel<List<InstrumentTradeStockSharpViewModel>>>()
            .RegisterMqListener<GetMarkersForInstrumentStockSharpDriverReceive, int, TResponseModel<List<MarkerInstrumentStockSharpViewModel>>>()
            .RegisterMqListener<GetOrdersStockSharpDriverReceive, int[], TResponseModel<List<OrderStockSharpModel>>>()
            .RegisterMqListener<OrderRegisterStockSharpDriverReceive, CreateOrderRequestModel, ResponseBaseModel>()
            .RegisterMqListener<OrderRegisterRequestStockSharpDriverReceive, OrderRegisterRequestModel, OrderRegisterRequestResponseModel>()
            .RegisterMqListener<InstrumentFavoriteToggleReceive, InstrumentTradeStockSharpViewModel, ResponseBaseModel>()
            .RegisterMqListener<InstrumentsSelectStockSharpDriverReceive, InstrumentsRequestModel, TPaginationResponseModel<InstrumentTradeStockSharpViewModel>>()
            .RegisterMqListener<OrdersSelectStockSharpDriverReceive, TPaginationRequestStandardModel<OrdersSelectStockSharpRequestModel>, TPaginationResponseModel<OrderStockSharpViewModel>>()
            .RegisterMqListener<MyTradesSelectStockSharpDriverReceive, TPaginationRequestStandardModel<MyTradeSelectStockSharpRequestModel>, TPaginationResponseModel<MyTradeStockSharpViewModel>>()
            ;
    }
}