////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// UpdateOrCreateAdapterStockSharpDriverReceive
/// </summary>
public class UpdateOrCreateAdapterStockSharpDriverReceive(IManageStockSharpService ssRepo)
    : IMQTTReceive<FixMessageAdapterModelDB, TResponseModel<FixMessageAdapterModelDB>>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.UpdateOrCreateAdapterStockSharpReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<FixMessageAdapterModelDB>> ResponseHandleActionAsync(FixMessageAdapterModelDB req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.UpdateOrCreateAdapterAsync(req, token);
    }
}