﻿////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using RemoteCallLib;
using SharedLib;
using StockSharpDriver;

namespace Transmission.Receives.telegram;

/// <summary>
/// Получить чаты
/// </summary>
public class ChatsSelectTelegramReceive(ITelegramBotService tgRepo) 
    : IMQTTReceive<TPaginationRequestModel<string?>?, TPaginationResponseModel<ChatTelegramModelDB>?>
{
    /// <inheritdoc/>
    public static string QueueName => GlobalStaticConstantsTransmission.TransmissionQueues.ChatsSelectTelegramReceive;

    /// <inheritdoc/>
    public async Task<TPaginationResponseModel<ChatTelegramModelDB>?> ResponseHandleActionAsync(TPaginationRequestModel<string?>? req, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        return await tgRepo.ChatsSelectTelegramAsync(req, token);
    }
}