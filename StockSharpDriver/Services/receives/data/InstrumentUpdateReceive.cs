////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// InstrumentUpdateReceive
/// </summary>
public class InstrumentUpdateReceive(IDataStockSharpService ssRepo)
    : IMQTTReceive<InstrumentTradeStockSharpViewModel, ResponseBaseModel>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.UpdateInstrumentStockSharpReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> ResponseHandleActionAsync(InstrumentTradeStockSharpViewModel req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.UpdateInstrumentAsync(req, token);
    }
}