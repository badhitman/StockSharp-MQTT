////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// AboutConnectStockSharpDriverReceive
/// </summary>
public class AboutConnectStockSharpDriverReceive(IDriverStockSharpService ssRepo)
    : IMQTTReceive<object?, AboutConnectResponseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.AboutConnectStockSharpReceive;

    /// <inheritdoc/>
    public async Task<AboutConnectResponseModel?> ResponseHandleActionAsync(object? req, CancellationToken token = default)
    {
        //if (req is null)
        //    throw new ArgumentNullException(nameof(req));

        return await ssRepo.AboutConnection(token);
    }
}