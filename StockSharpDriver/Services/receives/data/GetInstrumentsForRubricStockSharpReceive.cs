////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// GetInstrumentsForRubricStockSharpReceive
/// </summary>
public class GetInstrumentsForRubricStockSharpReceive(IDataStockSharpService ssRepo)
    : IMQTTReceive<int, TResponseModel<List<InstrumentTradeStockSharpViewModel>>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.GetInstrumentsForRubricStockSharpReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<List<InstrumentTradeStockSharpViewModel>>?> ResponseHandleActionAsync(int req, CancellationToken token = default)
    {
        //if (req is null)
        //    throw new ArgumentNullException(nameof(req));

        return await ssRepo.GetInstrumentsForRubricAsync(req, token);
    }
}