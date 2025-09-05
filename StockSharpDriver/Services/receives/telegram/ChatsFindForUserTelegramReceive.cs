////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;
using StockSharpDriver;

namespace Transmission.Receives.telegram;

/// <summary>
/// Найти чаты пользователей ITelegramBotService
/// </summary>
public class ChatsFindForUserTelegramReceive(ITelegramBotService tgRepo)
    : IMQTTReceive<long[]?, List<ChatTelegramModelDB>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.ChatsFindForUserTelegramReceive;

    /// <inheritdoc/>
    public async Task<List<ChatTelegramModelDB>?> ResponseHandleActionAsync(long[]? chats_ids, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(chats_ids);
        return await tgRepo.ChatsFindForUserTelegramAsync(chats_ids, token);
    }
}