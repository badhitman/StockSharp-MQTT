////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// AdaptersSelectStockSharpDriverReceive
/// </summary>
public class AdaptersSelectStockSharpDriverReceive(IManageStockSharpService ssRepo)
    : IMQTTReceive<TPaginationRequestStandardModel<AdaptersRequestModel>, TPaginationResponseModel<FixMessageAdapterModelDB>>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.AdaptersSelectStockSharpReceive;

    /// <inheritdoc/>
    public async Task<TPaginationResponseModel<FixMessageAdapterModelDB>> ResponseHandleActionAsync(TPaginationRequestStandardModel<AdaptersRequestModel> req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.AdaptersSelectAsync(req, token);
    }
}
