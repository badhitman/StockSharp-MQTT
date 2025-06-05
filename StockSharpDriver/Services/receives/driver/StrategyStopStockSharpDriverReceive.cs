////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// StrategyStopStockSharpDriverReceive
/// </summary>
public class StrategyStopStockSharpDriverReceive(IDriverStockSharpService ssRepo)
    : IMQTTReceive<StrategyStopRequestModel, ResponseBaseModel>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.StrategyStopStockSharpDriverReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> ResponseHandleActionAsync(StrategyStopRequestModel req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.StrategyStopAsync(req, token);
    }
}