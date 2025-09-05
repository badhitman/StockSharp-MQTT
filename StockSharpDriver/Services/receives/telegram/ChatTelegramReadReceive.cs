////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using RemoteCallLib;
using SharedLib;
using StockSharpDriver;

namespace Transmission.Receives.telegram;

/// <summary>
/// Прочитать данные чата
/// </summary>
public class ChatTelegramReadReceive(ITelegramBotService tgRepo)
    : IMQTTReceive<int, ChatTelegramModelDB?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.ChatReadTelegramReceive;

    /// <inheritdoc/>
    public async Task<ChatTelegramModelDB?> ResponseHandleActionAsync(int chat_id, CancellationToken token = default)
    {
        return await tgRepo.ChatTelegramReadAsync(chat_id, token);
    }
}