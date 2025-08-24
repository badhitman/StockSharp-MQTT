////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// InstrumentRubricUpdateStockSharpReceive
/// </summary>
public class InstrumentRubricUpdateStockSharpReceive(IDataStockSharpService ssRepo)
    : IMQTTReceive<InstrumentRubricUpdateModel?, ResponseBaseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.InstrumentRubricUpdateStockSharpReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel?> ResponseHandleActionAsync(InstrumentRubricUpdateModel? req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.InstrumentRubricUpdateAsync(req, token);
    }
}