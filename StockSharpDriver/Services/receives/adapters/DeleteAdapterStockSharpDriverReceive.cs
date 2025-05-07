////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// DeleteAdapterStockSharpDriverReceive
/// </summary>
public class DeleteAdapterStockSharpDriverReceive(IManageStockSharpService ssRepo)
    : IMQTTReceive<FixMessageAdapterModelDB, ResponseBaseModel>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.DeleteAdapterStockSharpReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> ResponseHandleActionAsync(FixMessageAdapterModelDB req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.DeleteAdapterAsync(req, token);
    }
}