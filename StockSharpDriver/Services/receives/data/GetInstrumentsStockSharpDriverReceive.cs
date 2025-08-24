////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// GetInstrumentsStockSharpDriverReceive
/// </summary>
public class GetInstrumentsStockSharpDriverReceive(IDataStockSharpService ssRepo)
    : IMQTTReceive<int[]?, TResponseModel<List<InstrumentTradeStockSharpViewModel>>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.GetInstrumentsStockSharpReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<List<InstrumentTradeStockSharpViewModel>>?> ResponseHandleActionAsync(int[]? req, CancellationToken token = default)
    {
        //if (req is null)
        //    throw new ArgumentNullException(nameof(req));

        return await ssRepo.GetInstrumentsAsync(req, token);
    }
}