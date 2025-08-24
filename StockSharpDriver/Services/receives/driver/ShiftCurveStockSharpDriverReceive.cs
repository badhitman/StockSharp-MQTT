////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// ShiftCurveStockSharpDriverReceive
/// </summary>
public class ShiftCurveStockSharpDriverReceive(IDriverStockSharpService ssRepo)
    : IMQTTReceive<ShiftCurveRequestModel?, ResponseBaseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.ShiftCurveStockSharpDriverReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel?> ResponseHandleActionAsync(ShiftCurveRequestModel? req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.ShiftCurve(req, token);
    }
}