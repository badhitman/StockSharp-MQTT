////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// MyTradesSelectStockSharpDriverReceive
/// </summary>
public class MyTradesSelectStockSharpDriverReceive(IManageStockSharpService ssRepo)
    : IMQTTReceive<TPaginationRequestStandardModel<MyTradeSelectStockSharpRequestModel>, TPaginationResponseModel<MyTradeStockSharpViewModel>>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.MyTradesSelectStockSharpReceive;

    /// <inheritdoc/>
    public async Task<TPaginationResponseModel<MyTradeStockSharpViewModel>> ResponseHandleActionAsync(TPaginationRequestStandardModel<MyTradeSelectStockSharpRequestModel> req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.TradesSelectAsync(req, token);
    }
}