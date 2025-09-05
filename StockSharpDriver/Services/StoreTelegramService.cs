////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using SharedLib;
using DbcLib;

namespace StockSharpDriver;

/// <summary>
/// Сохранение в базу данных данных Telegram
/// </summary>
public class StoreTelegramService(IDbContextFactory<TelegramBotAppContext> tgDbFactory)
{
    /// <summary>
    /// Сохранить чат в базу данных
    /// </summary>
    public async Task<ChatTelegramModelDB> StoreChat(Chat chat)
    {
        using TelegramBotAppContext context = await tgDbFactory.CreateDbContextAsync();
        ChatTelegramModelDB? chat_db = await context
            .Chats
            .FirstOrDefaultAsync(x => x.ChatTelegramId == chat.Id);

        if (chat_db is null)
        {
            chat_db = new ChatTelegramModelDB()
            {
                Type = (ChatsTypesTelegramEnum)(int)chat.Type,
                ChatTelegramId = chat.Id,
                IsForum = chat.IsForum,

                Title = chat.Title,
                NormalizedTitleUpper = chat.Title?.ToUpper(),

                FirstName = chat.FirstName,
                NormalizedFirstNameUpper = chat.FirstName?.ToUpper(),

                LastName = chat.LastName,
                NormalizedLastNameUpper = chat.LastName?.ToUpper(),

                Username = chat.Username,
                NormalizedUsernameUpper = chat.Username?.ToUpper(),
            };

            await context.AddAsync(chat_db);
        }
        else
        {
            chat_db.Type = (ChatsTypesTelegramEnum)(int)chat.Type;
            chat_db.ChatTelegramId = chat.Id;
            chat_db.IsForum = chat.IsForum;

            chat_db.Title = chat.Title;
            chat_db.NormalizedTitleUpper = chat.Title?.ToUpper();

            chat_db.FirstName = chat.FirstName;
            chat_db.NormalizedFirstNameUpper = chat.FirstName?.ToUpper();

            chat_db.LastName = chat.LastName;
            chat_db.NormalizedLastNameUpper = chat.LastName?.ToUpper();

            chat_db.Username = chat.Username;
            chat_db.NormalizedUsernameUpper = chat.Username?.ToUpper();

            chat_db.LastUpdateUtc = DateTime.UtcNow;
            context.Update(chat_db);
        }
        await context.SaveChangesAsync();
        return chat_db;
    }

    /// <summary>
    /// Сохранить пользователя в базу данных
    /// </summary>
    public async Task<UserTelegramModelDB> StoreUser(User user)
    {
        using TelegramBotAppContext context = await tgDbFactory.CreateDbContextAsync();
        UserTelegramModelDB? user_db = await context
            .Users
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.UserTelegramId == user.Id);

        if (user_db is null)
        {
            user_db = new()
            {
                FirstName = user.FirstName,
                NormalizedFirstNameUpper = user.FirstName.ToUpper(),

                LastName = user.LastName,
                NormalizedLastNameUpper = user.LastName?.ToUpper(),

                Username = user.Username,
                NormalizedUsernameUpper = user.Username?.ToUpper(),

                UserTelegramId = user.Id,
                IsBot = user.IsBot,
                IsPremium = user.IsPremium,

                AddedToAttachmentMenu = user.AddedToAttachmentMenu,
                LanguageCode = user.LanguageCode,
            };
            await context.AddAsync(user_db);
        }
        else
        {
            user_db.FirstName = user.FirstName;
            user_db.NormalizedFirstNameUpper = user.FirstName.ToUpper();

            user_db.LastName = user.LastName;
            user_db.NormalizedLastNameUpper = user.LastName?.ToUpper();

            user_db.Username = user.Username;
            user_db.NormalizedUsernameUpper = user.Username?.ToUpper();

            user_db.UserTelegramId = user.Id;
            user_db.IsBot = user.IsBot;
            user_db.IsPremium = user.IsPremium;

            user_db.AddedToAttachmentMenu = user.AddedToAttachmentMenu;
            user_db.LanguageCode = user.LanguageCode;
            user_db.LastUpdateUtc = DateTime.UtcNow;

            context.Update(user_db);
        }
        await context.SaveChangesAsync();
        return user_db;
    }

    /// <summary>
    /// Сохранить сообщение в базу данных
    /// </summary>
    public async Task<MessageTelegramModelDB> StoreMessage(Message message)
    {
        ChatTelegramModelDB chat_db = await StoreChat(message.Chat);
        ChatTelegramModelDB? sender_chat_db = message.SenderChat is null ? null : await StoreChat(message.SenderChat);
        UserTelegramModelDB? from_db = message.From is null ? null : await StoreUser(message.From);
        UserTelegramModelDB? forward_from_db = message.ForwardFrom is null ? null : await StoreUser(message.ForwardFrom);

        MessageTelegramModelDB? replyToMessageDB = message.ReplyToMessage is null ? null : await StoreMessage(message.ReplyToMessage);

        using TelegramBotAppContext context = await tgDbFactory.CreateDbContextAsync();

        IIncludableQueryable<MessageTelegramModelDB, UserTelegramModelDB?> q = context
            .Messages
            .Include(x => x.Chat)
            .Include(x => x.From);

        MessageTelegramModelDB? messageDb = from_db is null
            ? await q.FirstOrDefaultAsync(x => x.MessageTelegramId == message.MessageId && x.ChatId == chat_db.ChatTelegramId && x.FromId == null)
            : await q.FirstOrDefaultAsync(x => x.MessageTelegramId == message.MessageId && x.ChatId == chat_db.ChatTelegramId && x.FromId == from_db.Id);

        if (messageDb is null)
        {
            messageDb = new()
            {
                ChatId = chat_db.Id,
                FromId = from_db?.Id,
                EditDate = message.EditDate,
                ForwardDate = message.ForwardDate,

                ForwardFromChatId = message.ForwardFromChat?.Id,
                ForwardFromMessageId = message.ForwardFromMessageId,
                ForwardFromId = message.ForwardFrom?.Id,
                ForwardSenderName = message.ForwardSenderName,
                ForwardSignature = message.ForwardSignature,

                IsAutomaticForward = message.IsAutomaticForward,
                MessageTelegramId = message.MessageId,
                MessageThreadId = message.MessageThreadId,

                ViaBotId = message.ViaBot?.Id,
                IsTopicMessage = message.IsTopicMessage,
                SenderChatId = sender_chat_db?.Id,
                ReplyToMessageId = replyToMessageDB?.Id,
                //
                Caption = message.Caption,
                NormalizedCaptionUpper = message.Caption?.ToUpper(),

                Text = message.Text,
                NormalizedTextUpper = message.Text?.ToUpper(),

                AuthorSignature = message.AuthorSignature,
                MediaGroupId = message.MediaGroupId,
            };

            await context.AddAsync(messageDb);

            await context.SaveChangesAsync();
        }
        else
        {
            messageDb.ChatId = chat_db.Id;
            messageDb.FromId = from_db?.Id;
            messageDb.EditDate = message.EditDate;
            messageDb.ForwardDate = message.ForwardDate;

            messageDb.ForwardFromChatId = message.ForwardFromMessageId;
            messageDb.ForwardFromMessageId = message.ForwardFromMessageId;
            messageDb.ForwardFromId = forward_from_db?.Id;
            messageDb.ForwardSenderName = message.ForwardSenderName;
            messageDb.ForwardSignature = message.ForwardSignature;

            messageDb.IsAutomaticForward = message.IsAutomaticForward;
            messageDb.MessageTelegramId = message.MessageId;
            messageDb.MessageThreadId = message.MessageThreadId;

            messageDb.ViaBotId = message.ViaBot?.Id;
            messageDb.IsTopicMessage = message.IsTopicMessage;
            messageDb.SenderChatId = sender_chat_db?.Id;
            messageDb.ReplyToMessageId = replyToMessageDB?.Id;
            //
            messageDb.Caption = message.Caption;
            messageDb.NormalizedCaptionUpper = message.Caption?.ToUpper();

            messageDb.Text = message.Text;
            messageDb.NormalizedTextUpper = message.Text?.ToUpper();

            messageDb.AuthorSignature = message.AuthorSignature;
            messageDb.MediaGroupId = message.MediaGroupId;

            context.Update(messageDb);
            await context.SaveChangesAsync();
        }

        chat_db.LastMessageId = messageDb.Id;
        context.Update(chat_db);

        if (sender_chat_db is not null && sender_chat_db.Id != chat_db.Id)
        {
            sender_chat_db.LastMessageId = messageDb.Id;
            context.Update(sender_chat_db);
        }

        if (from_db is not null)
        {
            from_db.LastMessageId = messageDb.Id;
            context.Update(from_db);
        }

        if (from_db is not null && from_db.UserTelegramId != chat_db.ChatTelegramId && !await context.JoinsUsersToChats.AnyAsync(x => x.UserId == from_db.Id && x.ChatId == chat_db.Id))
        {
            await context.AddAsync(new JoinUserChatModelDB() { ChatId = chat_db.Id, UserId = from_db.Id });
            await context.SaveChangesAsync();
        }
        await context.SaveChangesAsync();

        messageDb.Chat = chat_db;
        messageDb.From = from_db;
        messageDb.SenderChat = sender_chat_db;
        messageDb.ForwardFrom = forward_from_db;
        messageDb.ReplyToMessage = replyToMessageDB;

        return messageDb;
    }
}