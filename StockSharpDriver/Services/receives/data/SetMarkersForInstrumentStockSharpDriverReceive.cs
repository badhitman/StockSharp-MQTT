////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// SetMarkersForInstrumentStockSharpDriverReceive
/// </summary>
public class SetMarkersForInstrumentStockSharpDriverReceive(IDataStockSharpService ssRepo)
    : IMQTTReceive<SetMarkersForInstrumentRequestModel?, ResponseBaseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.SetMarkersForInstrumentStockSharpReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel?> ResponseHandleActionAsync(SetMarkersForInstrumentRequestModel? req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.SetMarkersForInstrumentAsync(req, token);
    }
}