////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Newtonsoft.Json;
using RemoteCallLib;
using SharedLib;
using StockSharpDriver;

namespace Transmission.Receives.telegram;

/// <summary>
/// Переслать сообщение пользователю через TelegramBot ForwardMessageTelegramReceive
/// </summary>
public class ForwardMessageTelegramReceive(ILogger<ForwardMessageTelegramReceive> loggerRepo, ITelegramBotService tgRepo)
    : IMQTTReceive<ForwardMessageTelegramBotModel?, TResponseModel<MessageComplexIdsModel>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.ForwardTextMessageTelegramReceive;

    /// <inheritdoc/>
    public async Task<TResponseModel<MessageComplexIdsModel>?> ResponseHandleActionAsync(ForwardMessageTelegramBotModel? message, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        loggerRepo.LogInformation($"call `{GetType().Name}`: {JsonConvert.SerializeObject(message)}");
        return await tgRepo.ForwardMessageTelegramAsync(message, token);
    }
}