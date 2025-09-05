////////////////////////////////////////////////
// � https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace StockSharpDriver;

/// <summary>
/// TelegramBot
/// </summary>
public interface ITelegramBotService : ITelegramBotStandardService
{
    /// <summary>
    /// ChatsFindForUserTelegram
    /// </summary>
    public Task<List<ChatTelegramModelDB>> ChatsFindForUserTelegramAsync(long[] req, CancellationToken token = default);

    /// <summary>
    /// ChatsReadTelegram
    /// </summary>
    public Task<List<ChatTelegramModelDB>> ChatsReadTelegramAsync(long[] req, CancellationToken token = default);

    /// <summary>
    /// ChatsSelectTelegram
    /// </summary>
    public Task<TPaginationResponseModel<ChatTelegramModelDB>> ChatsSelectTelegramAsync(TPaginationRequestStandardModel<string?> req, CancellationToken token = default);

    /// <summary>
    /// ChatTelegramRead
    /// </summary>
    public Task<ChatTelegramModelDB> ChatTelegramReadAsync(int chatId, CancellationToken token = default);

    /// <summary>
    /// ForwardMessageTelegram
    /// </summary>
    public Task<TResponseModel<MessageComplexIdsModel>> ForwardMessageTelegramAsync(ForwardMessageTelegramBotModel req, CancellationToken token = default);

    /// <summary>
    /// GetFileTelegram
    /// </summary>
    public Task<TResponseModel<byte[]>> GetFileTelegramAsync(string req, CancellationToken token = default);

    /// <summary>
    /// MessagesSelectTelegram
    /// </summary>
    public Task<TPaginationResponseModel<MessageTelegramModelDB>> MessagesSelectTelegramAsync(TPaginationRequestStandardModel<SearchMessagesChatModel> req, CancellationToken token = default);
}