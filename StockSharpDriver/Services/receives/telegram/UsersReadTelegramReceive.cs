////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Newtonsoft.Json;
using SharedLib;
using StockSharpDriver;

namespace Transmission.Receives.telegram;

/// <summary>
/// UsersReadTelegramReceive
/// </summary>
public class UsersReadTelegramReceive(ITelegramBotService tgRepo, ILogger<UsersReadTelegramReceive> _logger)
    : IMQTTReceive<int[]?, List<UserTelegramModelDB>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.UsersReadTelegramReceive;

    /// <inheritdoc/>
    public async Task<List<UserTelegramModelDB>?> ResponseHandleActionAsync(int[]? req, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        _logger.LogInformation($"call `{GetType().Name}`: {JsonConvert.SerializeObject(req)}");
        return await tgRepo.UsersReadTelegramAsync(req, token);
    }
}