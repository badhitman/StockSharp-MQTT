////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// GenerateRegularCashFlowsStockSharpReceive
/// </summary>
public class GenerateRegularCashFlowsStockSharpReceive(IManageStockSharpService ssRepo)
    : IMQTTReceive<CashFlowStockSharpRequestModel?, ResponseBaseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.GenerateRegularCashFlowsStockSharpReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel?> ResponseHandleActionAsync(CashFlowStockSharpRequestModel? req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.GenerateRegularCashFlowsAsync(req, token);
    }
}