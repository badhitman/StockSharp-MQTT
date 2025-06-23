////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// LimitsStrategiesUpdateStockSharpDriverReceive
/// </summary>
public class LimitsStrategiesUpdateStockSharpDriverReceive(IDriverStockSharpService ssRepo)
    : IMQTTReceive<LimitsStrategiesUpdateRequestModel, ResponseBaseModel>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.LimitsStrategiesUpdateStockSharpDriverReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> ResponseHandleActionAsync(LimitsStrategiesUpdateRequestModel req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.LimitsStrategiesUpdate(req,token);
    }
}