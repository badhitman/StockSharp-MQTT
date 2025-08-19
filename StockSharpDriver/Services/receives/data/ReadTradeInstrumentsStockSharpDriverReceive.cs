////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// InstrumentsSelectStockSharpDriverReceive
/// </summary>
public class ReadTradeInstrumentsStockSharpDriverReceive(IDataStockSharpService ssRepo)
    : IMQTTReceive<object, TResponseModel<List<InstrumentTradeStockSharpViewModel>>>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.InstrumentsReadTradeStockSharpReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<List<InstrumentTradeStockSharpViewModel>>> ResponseHandleActionAsync(object req, CancellationToken token = default)
    {
        //if (req is null)
        //    throw new ArgumentNullException(nameof(req));

        return await ssRepo.ReadTradeInstrumentsAsync(token);
    }
}