////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// InitialLoadStockSharpDriverReceive
/// </summary>
public class InitialLoadStockSharpDriverReceive(IDriverStockSharpService ssRepo)
    : IMQTTReceive<InitialLoadRequestModel, ResponseBaseModel>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.InitialLoadStockSharpDriverReceive;

    /// <inheritdoc/>
    public async Task<ResponseBaseModel> ResponseHandleActionAsync(InitialLoadRequestModel req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.InitialLoad(req, token);
    }
}