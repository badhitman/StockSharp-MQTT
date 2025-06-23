////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Transmission.Receives.StockSharpDriver;
using Transmission.Receives.storage;
using MQTTCallLib;
using SharedLib;
using Transmission.Receives.rubrics;
using Transmission.Receives.telegram;

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
            .RegisterMqListener<SaveParameterReceive, StorageCloudParameterPayloadModel, TResponseModel<int?>>()
            .RegisterMqListener<ReadParameterReceive, StorageMetadataModel, TResponseModel<StorageCloudParameterPayloadModel>>()
            .RegisterMqListener<ReadParametersReceive, StorageMetadataModel[], TResponseModel<List<StorageCloudParameterPayloadModel>>>()
            .RegisterMqListener<FindParametersReceive, FindStorageBaseModel, TResponseModel<FoundParameterModel[]>>()
            .RegisterMqListener<TagSetReceive, TagSetModel, ResponseBaseModel>()
            .RegisterMqListener<TagsSelectReceive, TPaginationRequestModel<SelectMetadataRequestModel>, TPaginationResponseModel<TagViewModel>>()
            .RegisterMqListener<RubricsListReceive, RubricsListRequestModel, List<UniversalBaseModel>>()
            .RegisterMqListener<RubricCreateOrUpdateReceive, RubricStandardModel, TResponseModel<int>>()
            .RegisterMqListener<RubricMoveReceive, TAuthRequestModel<RowMoveModel>, ResponseBaseModel>()
            .RegisterMqListener<RubricReadReceive, int, TResponseModel<List<RubricStandardModel>>>()
            .RegisterMqListener<RubricsGetReceive, int[], TResponseModel<List<RubricStandardModel>>>()
            .RegisterMqListener<GetBotUsernameReceive, object, TResponseModel<UserTelegramBaseModel>>()
            .RegisterMqListener<SendTextMessageTelegramReceive, SendTextMessageTelegramBotModel, TResponseModel<MessageComplexIdsModel>>()
            .RegisterMqListener<GoToPageForRowReceive, TPaginationRequestStandardModel<int>, TPaginationResponseModel<NLogRecordModelDB>>()
            .RegisterMqListener<MetadataLogsReceive, PeriodDatesTimesModel, TResponseModel<LogsMetadataResponseModel>>()
            .RegisterMqListener<LogsSelectReceive, TPaginationRequestStandardModel<LogsSelectRequestModel>, TPaginationResponseModel<NLogRecordModelDB>>()
            .RegisterMqListener<AboutConnectStockSharpDriverReceive, object, AboutConnectResponseModel>()
            .RegisterMqListener<LimitsStrategiesUpdateStockSharpDriverReceive, LimitsStrategiesUpdateRequestModel, ResponseBaseModel>()
            .RegisterMqListener<DisconnectStockSharpDriverReceive, object, ResponseBaseModel>()
            .RegisterMqListener<TerminateStockSharpDriverReceive, object, ResponseBaseModel>()
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
            .RegisterMqListener<StrategyStartStockSharpDriverReceive, StrategyStartRequestModel, ResponseBaseModel>()
            .RegisterMqListener<ShiftCurveStockSharpDriverReceive, ShiftCurveRequestModel, ResponseBaseModel>()
            .RegisterMqListener<StrategyStopStockSharpDriverReceive, StrategyStopRequestModel, ResponseBaseModel>()
            .RegisterMqListener<OrderRegisterRequestStockSharpDriverReceive, OrderRegisterRequestModel, OrderRegisterRequestResponseModel>()
            .RegisterMqListener<InstrumentFavoriteToggleReceive, InstrumentTradeStockSharpViewModel, ResponseBaseModel>()
            .RegisterMqListener<InstrumentsSelectStockSharpDriverReceive, InstrumentsRequestModel, TPaginationResponseModel<InstrumentTradeStockSharpViewModel>>()
            .RegisterMqListener<OrdersSelectStockSharpDriverReceive, TPaginationRequestStandardModel<OrdersSelectStockSharpRequestModel>, TPaginationResponseModel<OrderStockSharpViewModel>>()
            .RegisterMqListener<MyTradesSelectStockSharpDriverReceive, TPaginationRequestStandardModel<MyTradeSelectStockSharpRequestModel>, TPaginationResponseModel<MyTradeStockSharpViewModel>>()
            ;
    }
}