////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// RubricsInstrumentUpdateStockSharpReceive
/// </summary>
public class RubricsInstrumentUpdateStockSharpReceive(IDataStockSharpService ssRepo)
    : IMQTTReceive<RubricsInstrumentUpdateModel?, ResponseBaseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.RubricsInstrumentUpdateStockSharpReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel?> ResponseHandleActionAsync(RubricsInstrumentUpdateModel? req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.RubricsInstrumentUpdateAsync(req, token);
    }
}