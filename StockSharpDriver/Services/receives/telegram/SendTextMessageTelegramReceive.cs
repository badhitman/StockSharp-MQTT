////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Newtonsoft.Json;
using SharedLib;
using StockSharpDriver;

namespace Transmission.Receives.telegram;

/// <summary>
/// Отправить сообщение пользователю через TelegramBot SendTextMessageTelegramBotModel
/// </summary>
public class SendTextMessageTelegramReceive(ITelegramBotService tgRepo, ILogger<SendTextMessageTelegramReceive> _logger)
    : IMQTTReceive<SendTextMessageTelegramBotModel?, TResponseModel<MessageComplexIdsModel>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.SendTextMessageTelegramReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<MessageComplexIdsModel>?> ResponseHandleActionAsync(SendTextMessageTelegramBotModel? message, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        _logger.LogInformation($"call `{GetType().Name}`: {JsonConvert.SerializeObject(message)}");
        return await tgRepo.SendTextMessageTelegramAsync(message, token);
    }
}