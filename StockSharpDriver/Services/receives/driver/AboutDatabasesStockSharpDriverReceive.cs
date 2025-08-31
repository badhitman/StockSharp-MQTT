////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// AboutDatabasesStockSharpDriverReceive
/// </summary>
public class AboutDatabasesStockSharpDriverReceive(IManageStockSharpService ssRepo)
    : IMQTTReceive<object?, AboutDatabasesResponseModel?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.AboutDatabasesStockSharpReceive;

    /// <inheritdoc/>
    public async Task<AboutDatabasesResponseModel?> ResponseHandleActionAsync(object? req, CancellationToken token = default)
    {
        //if (req is null)
        //    throw new ArgumentNullException(nameof(req));

        return await ssRepo.AboutDatabases(token);
    }
}