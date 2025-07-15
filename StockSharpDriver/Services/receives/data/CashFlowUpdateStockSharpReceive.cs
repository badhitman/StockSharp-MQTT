////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// CashFlowUpdateStockSharpReceive
/// </summary>
public class CashFlowUpdateStockSharpReceive(IDataStockSharpService ssRepo)
    : IMQTTReceive<CashFlowViewModel, ResponseBaseModel>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.CashFlowUpdateStockSharpReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> ResponseHandleActionAsync(CashFlowViewModel req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.CashFlowUpdateAsync(req, token);
    }
}