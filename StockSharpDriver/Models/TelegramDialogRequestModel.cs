////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace StockSharpDriver;

/// <summary>
/// Telegram dialog request
/// </summary>
public class TelegramDialogRequestModel
{
    /// <summary>
    /// Telegram MessageId
    /// </summary>
    public int MessageTelegramId { get; set; }

    /// <summary>
    /// Telegram Message text
    /// </summary>
    public string? MessageText { get; set; }

    /// <summary>
    /// Тип входящего сообщения
    /// </summary>
    public TelegramMessagesTypesEnum TypeMessage { get; set; }

    /// <inheritdoc/>
    public MessageTelegramModelDB? TelegramUser { get; set; }
}