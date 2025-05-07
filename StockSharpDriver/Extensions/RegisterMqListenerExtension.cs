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
            .RegisterMqListener<ConnectStockSharpDriverReceive, object, ResponseBaseModel>()
            .RegisterMqListener<UpdateOrCreateAdapterStockSharpDriverReceive, FixMessageAdapterModelDB, TResponseModel<FixMessageAdapterModelDB>>()
            .RegisterMqListener<AdaptersSelectStockSharpDriverReceive, TPaginationRequestStandardModel<AdaptersRequestModel>, TPaginationResponseModel<FixMessageAdapterModelDB>>()
            .RegisterMqListener<AdaptersGetStockSharpDriverReceive, int[], TResponseModel<FixMessageAdapterModelDB[]>>()
            .RegisterMqListener<DeleteAdapterStockSharpDriverReceive, FixMessageAdapterModelDB, ResponseBaseModel>()
            .RegisterMqListener<PingStockSharpDriverReceive, object, ResponseBaseModel>()
            .RegisterMqListener<GetPortfoliosStockSharpDriverReceive, int[], TResponseModel<List<PortfolioStockSharpModel>>>()
            .RegisterMqListener<GetBoardsStockSharpDriverReceive, int[], TResponseModel<List<BoardStockSharpModel>>>()
            .RegisterMqListener<GetExchangesStockSharpDriverReceive, int[], TResponseModel<List<ExchangeStockSharpModel>>>()
            .RegisterMqListener<GetInstrumentsStockSharpDriverReceive, int[], TResponseModel<List<InstrumentTradeStockSharpModel>>>()
            .RegisterMqListener<GetOrdersStockSharpDriverReceive, int[], TResponseModel<List<OrderStockSharpModel>>>()
            .RegisterMqListener<OrderRegisterStockSharpDriverReceive, CreateOrderRequestModel, ResponseBaseModel>()
            .RegisterMqListener<InstrumentFavoriteToggleReceive, InstrumentTradeStockSharpViewModel, ResponseBaseModel>()
            .RegisterMqListener<InstrumentsSelectStockSharpDriverReceive, TPaginationRequestStandardModel<InstrumentsRequestModel>, TPaginationResponseModel<InstrumentTradeStockSharpViewModel>>()
            ;
    }
}