////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// GetRubricsForInstrumentStockSharpReceive
/// </summary>
public class GetRubricsForInstrumentStockSharpReceive(IDataStockSharpService ssRepo)
    : IMQTTReceive<int, TResponseModel<List<UniversalBaseModel>>>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.GetRubricsForInstrumentStockSharpReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<List<UniversalBaseModel>>> ResponseHandleActionAsync(int req, CancellationToken token = default)
    {
        //if (req is null)
        //    throw new ArgumentNullException(nameof(req));

        return await ssRepo.GetRubricsForInstrumentAsync(req, token);
    }
}