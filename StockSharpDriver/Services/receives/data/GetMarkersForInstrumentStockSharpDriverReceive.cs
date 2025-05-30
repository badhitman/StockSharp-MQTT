////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// GetMarkersForInstrumentStockSharpDriverReceive
/// </summary>
public class GetMarkersForInstrumentStockSharpDriverReceive(IDataStockSharpService ssRepo)
    : IMQTTReceive<int, TResponseModel<List<MarkerInstrumentStockSharpViewModel>>>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.GetMarkersForInstrumentsStockSharpReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<List<MarkerInstrumentStockSharpViewModel>>> ResponseHandleActionAsync(int req, CancellationToken token = default)
    {
        //if (req is null)
        //    throw new ArgumentNullException(nameof(req));

        return await ssRepo.GetMarkersForInstrumentAsync(req, token);
    }
}