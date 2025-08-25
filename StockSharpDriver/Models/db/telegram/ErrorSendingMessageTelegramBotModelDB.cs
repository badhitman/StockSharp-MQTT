﻿////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;

namespace SharedLib;

/// <summary>
/// Ошибка отправки сообщения TelegramBot
/// </summary>
[Index(nameof(ChatId)), Index(nameof(IsDisabled))]
public class ErrorSendingMessageTelegramBotModelDB : IdSwitchableModel
{
    /// <summary>
    /// CreatedAtUtc
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Chat
    /// </summary>
    public long ChatId { get; set; }

    /// <summary>
    /// Имя типа исключения
    /// </summary>
    public string? ExceptionTypeName { get; set; }

    /// <summary>
    /// Если сообщение является ответом, идентификатор исходного сообщения
    /// </summary>
    public int? ReplyToMessageId { get; set; }

    /// <summary>
    /// Пересылаемое сообщение (Telegram id)
    /// </summary>
    /// <remarks>
    /// Исходное сообщение, которое пересылается
    /// </remarks>
    public int SourceMessageId { get; set; }

    /// <summary>
    /// Message (error)
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Подпись (от кого сообщение). Если null, то подписи не будет
    /// </summary>
    public string? SignFrom { get; set; }

    /// <summary>
    /// Имя режима парсинга сообщений Telegram (default: Html). Пример: Html, Markdown, MarkdownV2
    /// <a href="https://core.telegram.org/bots/api#formatting-options"/>
    /// </summary>
    public string? ParseModeName { get; set; }

    /// <summary>
    /// Признак того, что ошибка возникла не в процессе отправки, а в процессе изменения существующего сообщения Telegram
    /// </summary>
    public bool IsEditing { get; set; }

    /// <summary>
    /// ErrorCode
    /// </summary>
    public int? ErrorCode { get; set; }
}