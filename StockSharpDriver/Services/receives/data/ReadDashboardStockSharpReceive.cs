////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Transmission.Receives.StockSharpDriver;

/// <summary>
/// ReadDashboardStockSharpReceive
/// </summary>
public class ReadDashboardStockSharpReceive(IDriverStockSharpService ssRepo)
    : IMQTTReceive<int[]?, List<DashboardTradeStockSharpModel>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.ReadDashboardStrategiesStockSharpDriverReceive;

    /// <inheritdoc/>
    public async Task<List<DashboardTradeStockSharpModel>?> ResponseHandleActionAsync(int[]? req, CancellationToken token = default)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        return await ssRepo.ReadDashboard(req, token);
    }
}