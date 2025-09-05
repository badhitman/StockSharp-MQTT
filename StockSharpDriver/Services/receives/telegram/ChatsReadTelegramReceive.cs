////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using RemoteCallLib;
using SharedLib;
using StockSharpDriver;

namespace Transmission.Receives.telegram;

/// <summary>
/// Прочитать чаты
/// </summary>
public class ChatsReadTelegramReceive(ITelegramBotService tgRepo)
    : IMQTTReceive<long[]?, List<ChatTelegramModelDB>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.ChatsReadTelegramReceive;

    /// <inheritdoc/>
    public async Task<List<ChatTelegramModelDB>?> ResponseHandleActionAsync(long[]? chats_ids, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(chats_ids);
        return await tgRepo.ChatsReadTelegramAsync(chats_ids, token);
    }
}