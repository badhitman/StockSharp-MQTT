////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;
using StockSharpDriver;

namespace Transmission.Receives.telegram;

/// <summary>
/// Получить сообщения из чата
/// </summary>
public class MessagesSelectTelegramReceive(ITelegramBotService tgRepo)
    : IMQTTReceive<TPaginationRequestModel<SearchMessagesChatModel>?, TPaginationResponseModel<MessageTelegramModelDB>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.MessagesChatsSelectTelegramReceive;

    /// <inheritdoc/>
    public async Task<TPaginationResponseModel<MessageTelegramModelDB>?> ResponseHandleActionAsync(TPaginationRequestModel<SearchMessagesChatModel>? req, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        return await tgRepo.MessagesSelectTelegramAsync(req, token);
    }
}