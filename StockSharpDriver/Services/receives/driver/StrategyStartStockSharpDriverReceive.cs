////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// StrategyStartStockSharpDriverReceive
/// </summary>
public class StrategyStartStockSharpDriverReceive(IDriverStockSharpService ssRepo)
    : IMQTTReceive<StrategyStartRequestModel?, ResponseBaseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.StrategyStartStockSharpDriverReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel?> ResponseHandleActionAsync(StrategyStartRequestModel? req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.StartStrategy(req, token);
    }
}