using SharedLib;
using StockSharpDriver;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;

namespace Telegram.Bot.Services;

/// <summary>
/// UpdateHandler
/// </summary>
public class UpdateHandler(ITelegramBotClient botClient,
    ILogger<UpdateHandler> logger,
    ITelegramDialogService receiveService,
    StoreTelegramService storeRepo) : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient = botClient;
    private readonly ILogger<UpdateHandler> _logger = logger;

    /// <inheritdoc/>
    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        Task handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult, cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText || message.From is null)
            return;

        MessageTelegramModelDB msg_db = await storeRepo.StoreMessage(message);
        
        if (message.Chat.Type == ChatType.Private)
            await Usage(msg_db, message.MessageId, TelegramMessagesTypesEnum.TextMessage, message.Chat.Id, messageText, cancellationToken);
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        if (callbackQuery.Message?.From is null || string.IsNullOrEmpty(callbackQuery.Data))
            return;

        MessageTelegramModelDB msg_db = await storeRepo.StoreMessage(callbackQuery.Message);

        if (callbackQuery.Message.Chat.Type == ChatType.Private)
            await Usage(msg_db, callbackQuery.Message.MessageId, TelegramMessagesTypesEnum.CallbackQuery, callbackQuery.Message.Chat.Id, callbackQuery.Data, cancellationToken);
    }


    async Task Usage(MessageTelegramModelDB uc, int incomingMessageId, TelegramMessagesTypesEnum eventType, ChatId chatId, string messageText, CancellationToken cancellationToken)
    {
        TelegramDialogResponseModel resp = await receiveService.TelegramDialogHandleAsync(new TelegramDialogRequestModel()
        {
            MessageText = messageText,
            MessageTelegramId = incomingMessageId,
            TelegramUser = uc,
            TypeMessage = eventType,
        }, cancellationToken);

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"Hi {uc.From!.GetName()}",
            cancellationToken: cancellationToken);
    }


    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results = [
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        ];

        await _botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await _botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }

    #endregion

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}


public class TelegramDialogService : ITelegramDialogService
{
    public Task<TelegramDialogResponseModel> TelegramDialogHandleAsync(TelegramDialogRequestModel tgDialog, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }
}
