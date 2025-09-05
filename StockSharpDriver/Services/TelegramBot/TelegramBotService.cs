////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Newtonsoft.Json;
using Telegram.Bot;
using SharedLib;
using DbcLib;

namespace StockSharpDriver;

/// <summary>
/// TelegramBotService
/// </summary>
public class TelegramBotServiceImplement(ILogger<TelegramBotServiceImplement> _logger,
    ITelegramBotClient _botClient,
    IDbContextFactory<TelegramBotAppContext> tgDbFactory,
    StoreTelegramService storeTgRepo) : ITelegramBotService
{
    /// <inheritdoc/>
    public async Task<List<ChatTelegramModelDB>> ChatsFindForUserTelegramAsync(long[] chats_ids, CancellationToken token = default)
    {
        TResponseModel<ChatTelegramModelDB[]?> res = new();
        using TelegramBotAppContext context = await tgDbFactory.CreateDbContextAsync(token);

        int[] users_ids = await context.Users.Where(x => chats_ids.Contains(x.UserTelegramId)).Select(x => x.Id).ToArrayAsync(cancellationToken: token);

        IQueryable<ChatTelegramModelDB> q = users_ids.Length == 0
            ? context.Chats.Where(x => chats_ids.Contains(x.ChatTelegramId))
            : context.Chats.Where(x => chats_ids.Contains(x.ChatTelegramId) || context.JoinsUsersToChats.Any(y => y.ChatId == x.Id && users_ids.Contains(y.UserId)));

        return await q
            .Include(x => x.UsersJoins!)
            .ThenInclude(x => x.User)
            .ToListAsync(cancellationToken: token);
    }

    /// <inheritdoc/>
    public async Task<List<ChatTelegramModelDB>> ChatsReadTelegramAsync(long[] chats_ids, CancellationToken token = default)
    {
        TResponseModel<ChatTelegramModelDB[]> res = new();
        using TelegramBotAppContext context = await tgDbFactory.CreateDbContextAsync(token);
        return await context.Chats.Where(x => chats_ids.Contains(x.ChatTelegramId)).ToListAsync(cancellationToken: token)
        ;
    }

    /// <inheritdoc/>
    public async Task<TPaginationResponseModel<ChatTelegramModelDB>> ChatsSelectTelegramAsync(TPaginationRequestModel<string?> req, CancellationToken token = default)
    {
        if (req.PageSize < 5)
            req.PageSize = 5;

        using TelegramBotAppContext context = await tgDbFactory.CreateDbContextAsync(token);

        IQueryable<ChatTelegramModelDB> q = context
            .Chats
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Payload))
        {
            string find_req = req.Payload.ToUpper();
            q = q.Where(x =>
            (x.NormalizedFirstNameUpper != null && x.NormalizedFirstNameUpper.Contains(find_req)) ||
            (x.NormalizedLastNameUpper != null && x.NormalizedLastNameUpper.Contains(find_req)) ||
            (x.NormalizedTitleUpper != null && x.NormalizedTitleUpper.Contains(find_req)) ||
            (x.NormalizedUsernameUpper != null && x.NormalizedUsernameUpper.Contains(find_req))
            );
        }

        IQueryable<ChatTelegramModelDB> TakePart(IQueryable<ChatTelegramModelDB> q, DirectionsEnum direct)
        {
            return direct == DirectionsEnum.Up
                ? q.OrderBy(x => x.LastUpdateUtc).Skip(req.PageNum * req.PageSize).Take(req.PageSize)
                : q.OrderByDescending(x => x.LastUpdateUtc).Skip(req.PageNum * req.PageSize).Take(req.PageSize);
        }

        return new()
        {
            PageNum = req.PageNum,
            PageSize = req.PageSize,
            SortBy = req.SortBy,
            SortingDirection = req.SortingDirection,
            TotalRowsCount = await q.CountAsync(cancellationToken: token),
            Response = await TakePart(q, req.SortingDirection).ToListAsync(cancellationToken: token),
        };
    }

    /// <inheritdoc/>
    public async Task<ChatTelegramModelDB> ChatTelegramReadAsync(int chatId, CancellationToken token = default)
    {
        TResponseModel<ChatTelegramModelDB> res = new();
        using TelegramBotAppContext context = await tgDbFactory.CreateDbContextAsync(token);
        return await context
            .Chats
            .Include(x => x.UsersJoins!)
            .ThenInclude(x => x.User)
            .FirstAsync(x => x.Id == chatId, cancellationToken: token);
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<MessageComplexIdsModel>> ForwardMessageTelegramAsync(ForwardMessageTelegramBotModel message, CancellationToken token = default)
    {
        TResponseModel<MessageComplexIdsModel> res = new();
        Message sender_msg;
        try
        {
            sender_msg = await _botClient.ForwardMessageAsync(chatId: message.DestinationChatId, fromChatId: message.SourceChatId, messageId: message.SourceMessageId, cancellationToken: token);

            MessageTelegramModelDB msg_db = await storeTgRepo.StoreMessage(sender_msg);
            res.Response = new()
            {
                TelegramId = sender_msg.MessageId,
                DatabaseId = msg_db.Id,
            };
        }
        catch (Exception ex)
        {
            int? errorCode = null;
            if (ex is ApiRequestException _are)
                errorCode = _are.ErrorCode;
            else if (ex is RequestException _re)
                errorCode = (int?)_re.HttpStatusCode;

            res.AddError("Ошибка отправки Telegram сообщения. error E06E939D-6E93-45CE-A5F5-19A417A27DC1");
            _logger.LogError(ex, $"{nameof(ForwardMessageTelegramAsync)}: {nameof(errorCode)} {errorCode}");
            res.Messages.InjectException(ex);
            return res;
        }

        return res;
    }

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

    /// <inheritdoc/>
    public async Task<TResponseModel<byte[]>> GetFileTelegramAsync(string fileId, CancellationToken token = default)
    {
        TResponseModel<byte[]> res = new();
        try
        {
            Telegram.Bot.Types.File fileTg = await _botClient.GetFileAsync(fileId, cancellationToken: token);
            MemoryStream ms = new();

            if (string.IsNullOrWhiteSpace(fileTg.FilePath))
            {
                res.AddError($"Ошибка получения {nameof(fileTg.FilePath)}");
                return res;
            }

            await _botClient.DownloadFileAsync(fileTg.FilePath, ms, token);
            res.Response = ms.ToArray();
        }
        catch (Exception ex)
        {
            res.AddError(ex.Message);
        }
        return res;
    }

    /// <inheritdoc/>
    public async Task<TPaginationResponseModel<MessageTelegramModelDB>> MessagesSelectTelegramAsync(TPaginationRequestModel<SearchMessagesChatModel> req, CancellationToken token = default)
    {
        if (req.PageSize < 5)
            req.PageSize = 5;

        using TelegramBotAppContext context = await tgDbFactory.CreateDbContextAsync(token);
        IQueryable<MessageTelegramModelDB> q = context.Messages.AsQueryable();

        if (req.Payload.ChatId != 0)
            q = q.Where(x => x.ChatId == req.Payload.ChatId);

        if (!string.IsNullOrWhiteSpace(req.Payload.SearchQuery))
        {
            req.Payload.SearchQuery = req.Payload.SearchQuery.ToUpper();
            q = q.Where(x => (x.NormalizedTextUpper != null && x.NormalizedTextUpper.Contains(req.Payload.SearchQuery)) || (x.NormalizedCaptionUpper != null && x.NormalizedCaptionUpper.Contains(req.Payload.SearchQuery)));
        }

        async Task<List<MessageTelegramModelDB>> Include(IQueryable<MessageTelegramModelDB> query)
        {
            var dbData = await
                (from msg in query

                 join joinSender in context.Users on msg.FromId equals joinSender.Id into getSender
                 from sender in getSender.DefaultIfEmpty()

                 join joinForward in context.Users on msg.ForwardFromId equals joinForward.UserTelegramId into getForward
                 from forward in getForward.DefaultIfEmpty()

                 join joinChat in context.Chats on msg.ChatId equals joinChat.Id into getChat
                 from chat in getChat.DefaultIfEmpty()

                 select new { msg, sender, forward, chat }).ToListAsync(cancellationToken: token);

            dbData.ForEach(r =>
            {
                r.msg.ForwardFrom = r.forward;
                r.msg.Chat = r.chat;
                r.msg.From = r.sender;
            });

            return dbData.Select(x => x.msg).ToList();
        }

        IQueryable<MessageTelegramModelDB> TakePart(IQueryable<MessageTelegramModelDB> q, DirectionsEnum direct)
        {
            return direct == DirectionsEnum.Up
                ? q.OrderBy(x => x.CreatedAtUtc).Skip(req.PageNum * req.PageSize).Take(req.PageSize)
                : q.OrderByDescending(x => x.CreatedAtUtc).Skip(req.PageNum * req.PageSize).Take(req.PageSize);
        }

        return new()
        {
            PageNum = req.PageNum,
            PageSize = req.PageSize,
            SortBy = req.SortBy,
            SortingDirection = req.SortingDirection,
            TotalRowsCount = await q.CountAsync(cancellationToken: token),
            Response = await Include(TakePart(q, req.SortingDirection))
        };
    }

    /// <inheritdoc/>
    public async Task<TResponseModel<MessageComplexIdsModel>> SendTextMessageTelegramAsync(SendTextMessageTelegramBotModel message, CancellationToken token = default)
    {
        TResponseModel<MessageComplexIdsModel> res = new();
        string msg;
        if (string.IsNullOrWhiteSpace(message.Message))
        {
            res.AddError("Текст сообщения не может быть пустым");
            return res;
        }
        using TelegramBotAppContext context = await tgDbFactory.CreateDbContextAsync(token);
        UserTelegramModelDB? currUserDb = await context.Users.FirstOrDefaultAsync(x => x.UserTelegramId == message.UserTelegramId, cancellationToken: token);

        if (currUserDb is null)
        {
            res.AddError($"Пользователь TG#{message.UserTelegramId} не найден в БД");
            return res;
        }

        ParseMode parse_mode;
        if (Enum.TryParse(typeof(ParseMode), message.ParseModeName, true, out object? parse_mode_out))
            parse_mode = (ParseMode)parse_mode_out!;
        else
        {
            parse_mode = ParseMode.Html;
            msg = $"Имя режима парсинга сообщения [{message.ParseModeName}] не допустимо. Установлен режим [{parse_mode}]. warning {{5A277B97-29B6-4B99-A022-A00E3F76E0C3}}";
            _logger.LogWarning(msg);
            res.AddWarning(msg);
        }

        IReplyMarkup? replyKB = message.ReplyKeyboard is null
            ? null
            : new InlineKeyboardMarkup(message.ReplyKeyboard.Select(x => x.Select(y => InlineKeyboardButton.WithCallbackData(y.Title ?? "~not set~", y.Data ?? "~not set~"))));

        Message sender_msg;
        MessageTelegramModelDB msg_db;
        try
        {
            string msg_text = string.IsNullOrWhiteSpace(message.From)
            ? message.Message
                : $"{message.Message}\n--- {message.From.Trim()}";

            if (message.Files is not null && message.Files.Count != 0)
            {
                if (message.Files.Count == 1)
                {
                    FileAttachModel file = message.Files[0];

                    if (GlobalToolsStandard.IsImageFile(file.Name))
                    {
                        sender_msg = await _botClient.SendPhotoAsync(chatId: message.UserTelegramId, photo: InputFile.FromStream(new MemoryStream(file.Data), file.Name), caption: msg_text, replyMarkup: replyKB, parseMode: parse_mode, replyToMessageId: message.ReplyToMessageId!.Value, cancellationToken: token);
                    }
                    else
                    {
                        sender_msg = await _botClient.SendDocumentAsync(chatId: message.UserTelegramId, document: InputFile.FromStream(new MemoryStream(file.Data), file.Name), caption: msg_text, parseMode: parse_mode, replyToMessageId: message.ReplyToMessageId, cancellationToken: token);
                    }

                    msg_db = await storeTgRepo.StoreMessage(sender_msg);
                    res.Response = new MessageComplexIdsModel()
                    {
                        DatabaseId = msg_db.Id,
                        TelegramId = sender_msg.MessageId
                    };
                }
                else
                {
                    Message[] senders_msgs = await _botClient.SendMediaGroupAsync(chatId: message.UserTelegramId, media: [.. message.Files.Select(ToolsStatic.ConvertFile)], replyToMessageId: message.ReplyToMessageId, cancellationToken: token);

                    foreach (Message mm in senders_msgs)
                    {
                        msg_db = await storeTgRepo.StoreMessage(mm);
                        res.Response = new MessageComplexIdsModel()
                        {
                            DatabaseId = msg_db.Id,
                            TelegramId = mm.MessageId
                        };
                    }
                }
            }
            else
            {
                sender_msg = await _botClient.SendTextMessageAsync(chatId: message.UserTelegramId, text: msg_text, parseMode: parse_mode, replyToMessageId: message.ReplyToMessageId, cancellationToken: token);

                msg_db = await storeTgRepo.StoreMessage(sender_msg);
                res.Response = new MessageComplexIdsModel()
                {
                    DatabaseId = msg_db.Id,
                    TelegramId = sender_msg.MessageId
                };
            }
        }
        catch (Exception ex)
        {
            int? errorCode = null;
            if (ex is ApiRequestException _are)
                errorCode = _are.ErrorCode;
            else if (ex is RequestException _re)
                errorCode = (int?)_re.HttpStatusCode;

            msg = "Ошибка отправки Telegram сообщения. error FA51C4EC-6AC7-4F7D-9B64-A6D6436DFDDA";
            res.AddError(msg);
            _logger.LogError(ex, msg);
            res.Messages.InjectException(ex);
            return res;
        }

        if (message.MainTelegramMessageId.HasValue && message.MainTelegramMessageId != 0)
        {
            try
            {
                await _botClient.DeleteMessageAsync(chatId: message.UserTelegramId, message.MainTelegramMessageId.Value, cancellationToken: token);
            }
            finally { }
        }

        return res;
    }
}