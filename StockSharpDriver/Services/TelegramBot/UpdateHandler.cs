using SharedLib;
using StockSharpDriver;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Services;

/// <summary>
/// UpdateHandler
/// </summary>
public class UpdateHandler(ITelegramBotClient botClient,
    ILogger<UpdateHandler> logger,
    ITelegramDialogService receiveService,
    StoreTelegramService storeRepo) : IUpdateHandler
{
    readonly ITelegramBotClient _botClient = botClient;
    readonly ILogger<UpdateHandler> _logger = logger;

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

    async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText || message.From is null)
            return;

        MessageTelegramModelDB msg_db = await storeRepo.StoreMessage(message);

        if (message.Chat.Type == ChatType.Private)
            await Usage(msg_db, message.MessageId, TelegramMessagesTypesEnum.TextMessage, message.Chat.Id, messageText, cancellationToken);
    }

    // Process Inline Keyboard callback data
    async Task BotOnCallbackQueryReceived(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        if (callbackQuery.Message?.From is null || string.IsNullOrEmpty(callbackQuery.Data))
            return;

        callbackQuery.Message.From = callbackQuery.From;
        callbackQuery.Message.Text = callbackQuery.Data;
        MessageTelegramModelDB msg_db = await storeRepo.StoreMessage(callbackQuery.Message, TelegramMessagesTypesEnum.CallbackQuery);

        if (callbackQuery.Message.Chat.Type == ChatType.Private)
        {
            TelegramDialogResponseModel resp = await Usage(msg_db, callbackQuery.Message.MessageId, TelegramMessagesTypesEnum.CallbackQuery, callbackQuery.Message.Chat.Id, callbackQuery.Data, cancellationToken);
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id, resp.Response, true, cancellationToken: cancellationToken);

            callbackQuery.Message.From = await _botClient.GetMeAsync(cancellationToken: cancellationToken);
            callbackQuery.Message.Text = resp.Response;
            msg_db = await storeRepo.StoreMessage(callbackQuery.Message, TelegramMessagesTypesEnum.CallbackQuery);
        }
    }

    async Task<TelegramDialogResponseModel> Usage(MessageTelegramModelDB uc, int incomingMessageId, TelegramMessagesTypesEnum eventType, ChatId chatId, string messageText, CancellationToken cancellationToken)
    {
        TelegramDialogResponseModel resp = await receiveService.TelegramDialogHandleAsync(new TelegramDialogRequestModel()
        {
            MessageText = messageText,
            MessageTelegramId = incomingMessageId,
            TelegramUser = uc.From,
            TypeMessage = eventType,
        }, cancellationToken);

        IReplyMarkup? replyKB = resp.ReplyKeyboard is null
            ? null
            : new InlineKeyboardMarkup(resp.ReplyKeyboard
            .Select(x => x.Select(y => InlineKeyboardButton.WithCallbackData(y.Title ?? "~not set~", y.Data ?? "~not set~"))));

        if (eventType == TelegramMessagesTypesEnum.TextMessage)
        {
            Message message = await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"Hi {uc.From!.GetName()}",
                replyMarkup: replyKB,
                cancellationToken: cancellationToken);

            uc = await storeRepo.StoreMessage(message);
        }

        return resp;
    }


    #region Inline Mode
    async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
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

    async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult, CancellationToken cancellationToken)
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
    Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
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