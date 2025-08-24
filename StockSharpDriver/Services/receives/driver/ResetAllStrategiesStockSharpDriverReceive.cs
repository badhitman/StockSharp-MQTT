////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// ResetAllStrategiesStockSharpDriverReceive
/// </summary>
public class ResetAllStrategiesStockSharpDriverReceive(IDriverStockSharpService ssRepo)
    : IMQTTReceive<ResetStrategyRequestBaseModel?, ResponseBaseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.ResetAllStrategiesStockSharpDriverReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel?> ResponseHandleActionAsync(ResetStrategyRequestBaseModel? req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.ResetAllStrategies(req, token);
    }
}