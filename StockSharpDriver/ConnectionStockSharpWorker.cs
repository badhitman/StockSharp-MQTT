////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace StockSharpDriver;

/// <inheritdoc/>
public class ConnectionStockSharpWorker(IStockSharpDriverService driverRepo, ILogger<ConnectionStockSharpWorker> _logger) : BackgroundService
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await driverRepo.Connect();
        _logger.LogInformation($"Service started!");
        while (!stoppingToken.IsCancellationRequested)
        {
            // _logger.LogDebug($"`tic-tac`");
            await Task.Delay(200, stoppingToken);
        }
        _logger.LogInformation($"Service stopped!");
        await driverRepo.Disconnect(null);
    }
}