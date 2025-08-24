////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov
////////////////////////////////////////////////

using SharedLib;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Services;

/// <summary>
/// TelegramBotStandardService
/// </summary>
public class TelegramBotStandardService(ITelegramBotClient _botClient, ILogger<TelegramBotStandardService> _logger) : ITelegramBotStandardService
{
    /// <inheritdoc/>
    public async Task<TResponseModel<UserTelegramBaseModel>> AboutBotAsync(CancellationToken token = default)
    {
        TResponseModel<UserTelegramBaseModel> res = new();
        User me;
        string msg;
        try
        {
            me = await _botClient.GetMeAsync(cancellationToken: token);
        }
        catch (Exception ex)
        {
            msg = "Ошибка получения данных бота `_botClient.GetMe`. error {50EE48C7-5A8A-420B-8B71-D1E2E44E48F4}";
            _logger.LogError(ex, msg);
            res.Messages.InjectException(ex);
            return res;
        }
        res.Response = new()
        {
            AddedToAttachmentMenu = me.AddedToAttachmentMenu,
            FirstName = me.FirstName,
            IsBot = me.IsBot,
            IsPremium = me.IsPremium,
            LanguageCode = me.LanguageCode,
            LastName = me.LastName,
            Username = me.Username,
            UserTelegramId = me.Id,
        };
        return res;
    }


    public async Task<TResponseModel<MessageComplexIdsModel>> SendTextMessageTelegramAsync(SendTextMessageTelegramBotModel message, CancellationToken token = default)
    {

        TResponseModel<MessageComplexIdsModel> res = new();
        string msg;
        if (string.IsNullOrWhiteSpace(message.Message))
        {
            res.AddError("Текст сообщения не может быть пустым");
            return res;
        }

        //TResponseModel<TelegramUserBaseModel> tgUser = await IdentityRepo.GetTelegramUserAsync(message.UserTelegramId, token);
        //if (tgUser.Response is null || !tgUser.Success())
        //{
        //    if (tgUser.Success())
        //        res.AddError($"Пользователь TG#{message.UserTelegramId} не найден в БД");
        //    res.AddRangeMessages(tgUser.Messages);
        //    return res;
        //}

        ParseMode parse_mode;
        if (Enum.TryParse(typeof(ParseMode), message.ParseModeName, true, out object? parse_mode_out) && parse_mode_out is not null)
            parse_mode = (ParseMode)parse_mode_out;
        else
        {
            parse_mode = ParseMode.Html;
            msg = $"Имя режима парсинга сообщения [{message.ParseModeName}] не допустимо. Установлен режим [{parse_mode}]. warning {{5A277B97-29B6-4B99-A022-A00E3F76E0C3}}";
            _logger.LogWarning(msg);
            res.AddWarning(msg);
        }

        IReplyMarkup? replyKB = message.ReplyKeyboard is null
            ? null
            : new InlineKeyboardMarkup(message.ReplyKeyboard
            .Select(x => x.Select(y => InlineKeyboardButton.WithCallbackData(y.Title, y.Data))));
        Message sender_msg;
        //MessageTelegramModelDB msg_db;
        try
        {
            string msg_text = string.IsNullOrWhiteSpace(message.From)
            ? message.Message
                : $"{message.Message}\n--- {message.From.Trim()}";

            if (message.Files is not null && message.Files.Count != 0)
            {
                //if (message.Files.Count == 1)
                //{
                //    FileAttachModel file = message.Files[0];

                //    if (GlobalToolsStandard.IsImageFile(file.Name))
                //    {
                //        sender_msg = await _botClient.SendPhoto(chatId: message.UserTelegramId, photo: InputFile.FromStream(new MemoryStream(file.Data), file.Name), caption: msg_text, replyMarkup: replyKB, parseMode: parse_mode, replyParameters: message.ReplyToMessageId!.Value, cancellationToken: token);
                //    }
                //    else
                //    {
                //        sender_msg = await _botClient.SendDocument(chatId: message.UserTelegramId, document: InputFile.FromStream(new MemoryStream(file.Data), file.Name), caption: msg_text, parseMode: parse_mode, replyParameters: message.ReplyToMessageId, cancellationToken: token);
                //    }

                //    msg_db = await storeTgRepo.StoreMessage(sender_msg);
                //    res.Response = new MessageComplexIdsModel()
                //    {
                //        DatabaseId = msg_db.Id,
                //        TelegramId = sender_msg.MessageId
                //    };
                //}
                //else
                //{
                //    Message[] senders_msgs = await _botClient.SendMediaGroup(chatId: message.UserTelegramId, media: message.Files.Select(ToolsStatic.ConvertFile).ToArray(), replyParameters: message.ReplyToMessageId, cancellationToken: token);

                //    foreach (Message mm in senders_msgs)
                //    {
                //        msg_db = await storeTgRepo.StoreMessage(mm);
                //        res.Response = new MessageComplexIdsModel()
                //        {
                //            DatabaseId = msg_db.Id,
                //            TelegramId = mm.MessageId
                //        };
                //    }
                //}
            }
            else
            {
                sender_msg = await _botClient.SendTextMessageAsync(chatId: message.UserTelegramId, text: msg_text, parseMode: parse_mode, replyToMessageId: message.ReplyToMessageId, cancellationToken: token);

                //msg_db = await storeTgRepo.StoreMessage(sender_msg);
                res.Response = new MessageComplexIdsModel()
                {
                    //DatabaseId = msg_db.Id,
                    TelegramId = sender_msg.MessageId
                };
            }
        }
        catch (Exception ex)
        {
            //using TelegramBotContext context = await tgDbFactory.CreateDbContextAsync(token);
            //int? errorCode = null;
            //if (ex is ApiRequestException _are)
            //    errorCode = _are.ErrorCode;
            //else if (ex is RequestException _re)
            //    errorCode = (int?)_re.HttpStatusCode;

            //await context.AddAsync(new ErrorSendingMessageTelegramBotModelDB()
            //{
            //    ChatId = message.UserTelegramId,
            //    CreatedAtUtc = DateTime.UtcNow,
            //    ReplyToMessageId = message.ReplyToMessageId,
            //    ParseModeName = message.ParseModeName,
            //    SignFrom = message.From,
            //    Message = $"{ex.Message}\n\n{JsonConvert.SerializeObject(message)}",
            //    ExceptionTypeName = ex.GetType().FullName,
            //    ErrorCode = errorCode
            //}, token);
            //await context.SaveChangesAsync(token);

            msg = "Ошибка отправки Telegram сообщения. error FA51C4EC-6AC7-4F7D-9B64-A6D6436DFDDA";
            res.AddError(msg);
            _logger.LogError(ex, msg);
            res.Messages.InjectException(ex);
            return res;
        }

        if (message.MainTelegramMessageId.HasValue && message.MainTelegramMessageId != 0)
        {
            //try
            //{
            //    await _botClient.DeleteMessage(chatId: message.UserTelegramId, message.MainTelegramMessageId.Value, cancellationToken: token);
            //}
            //finally { }
            //await IdentityRepo.UpdateTelegramMainUserMessageAsync(new() { MessageId = 0, UserId = message.UserTelegramId }, token);
        }

        return res;
    }
}