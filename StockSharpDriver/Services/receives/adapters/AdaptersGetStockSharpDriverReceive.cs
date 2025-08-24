////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// AdaptersGetStockSharpDriverReceive
/// </summary>
public class AdaptersGetStockSharpDriverReceive(IManageStockSharpService ssRepo)
    : IMQTTReceive<int[]?, TResponseModel<FixMessageAdapterModelDB[]>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.AdaptersGetStockSharpReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<FixMessageAdapterModelDB[]>?> ResponseHandleActionAsync(int[]? req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.AdaptersGetAsync(req, token);
    }
}
