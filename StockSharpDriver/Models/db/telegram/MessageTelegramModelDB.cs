////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace SharedLib;

/// <summary>
/// MessageTelegramModelDB
/// </summary>
[Microsoft.EntityFrameworkCore.Index(nameof(MessageTelegramId), nameof(ChatId), nameof(FromId))]
public class MessageTelegramModelDB : MessageTelegramViewModel
{
    /// <summary>
    /// Optional. Sender, empty for messages sent to channels
    /// </summary>
    public UserTelegramModelDB? From { get; set; }

    /// <summary>
    /// Optional. Sender of the message, sent on behalf of a chat. The channel itself for channel messages.
    /// The supergroup itself for messages from anonymous group administrators. The linked channel for messages
    /// automatically forwarded to the discussion group
    /// </summary>
    public ChatTelegramModelDB? Chat { get; set; }

    /// <summary>
    /// Ответ на сообщение
    /// </summary>
    [NotMapped]
    public MessageTelegramModelDB? ReplyToMessage { get; set; }

    /// <summary>
    /// SenderChat
    /// </summary>
    [NotMapped]
    public ChatTelegramModelDB? SenderChat { get; set; }

    /// <summary>
    /// ForwardFrom
    /// </summary>
    [NotMapped]
    public UserTelegramModelDB? ForwardFrom { get; set; }
}