////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// OrdersSelectStockSharpDriverReceive
/// </summary>
public class OrdersSelectStockSharpDriverReceive(IManageStockSharpService ssRepo)
    : IMQTTReceive<TPaginationRequestStandardModel<OrdersSelectStockSharpRequestModel>?, TPaginationResponseModel<OrderStockSharpViewModel>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.OrdersSelectStockSharpReceive;

    /// <inheritdoc/>
    public async Task<TPaginationResponseModel<OrderStockSharpViewModel>?> ResponseHandleActionAsync(TPaginationRequestStandardModel<OrdersSelectStockSharpRequestModel>? req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.OrdersSelectAsync(req, token);
    }
}