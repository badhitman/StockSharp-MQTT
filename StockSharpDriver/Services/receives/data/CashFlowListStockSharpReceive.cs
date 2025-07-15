////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// CashFlowListStockSharpReceive
/// </summary>
public class CashFlowListStockSharpReceive(IDataStockSharpService ssRepo)
    : IMQTTReceive<int, TResponseModel<List<CashFlowViewModel>>>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.CashFlowListStockSharpReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<List<CashFlowViewModel>>> ResponseHandleActionAsync(int instrumentId, CancellationToken token = default)
    {
        //if (instrumentId is null)
        //    throw new ArgumentNullException(nameof(instrumentId));

        return await ssRepo.CashFlowList(instrumentId, token);
    }
}