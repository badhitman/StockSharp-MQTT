////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using StockSharpDriver;
using SharedLib;

namespace Transmission.Receives.telegram;

/// <summary>
/// UsersReadTelegramReceive
/// </summary>
public class UsersReadTelegramReceive(ITelegramBotService tgRepo)
    : IMQTTReceive<int[]?, List<UserTelegramModelDB>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.UsersReadTelegramReceive;

    /// <inheritdoc/>
    public async Task<List<UserTelegramModelDB>?> ResponseHandleActionAsync(int[]? req, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        return await tgRepo.UsersReadTelegramAsync(req, token);
    }
}