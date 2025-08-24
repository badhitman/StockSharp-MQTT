////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// ResetStrategytStockSharpDriverReceive
/// </summary>
public class ResetStrategytStockSharpDriverReceive(IDriverStockSharpService ssRepo)
    : IMQTTReceive<ResetStrategyRequestModel?, ResponseBaseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.ResetStrategyStockSharpDriverReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel?> ResponseHandleActionAsync(ResetStrategyRequestModel? req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.ResetStrategy(req, token);
    }
}