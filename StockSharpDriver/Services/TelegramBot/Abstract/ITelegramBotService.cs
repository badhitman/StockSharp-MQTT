////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace StockSharpDriver;

/// <summary>
/// TelegramBot
/// </summary>
public interface ITelegramBotService : ITelegramBotStandardService
{
    /// <inheritdoc/>
    public Task<ResponseBaseModel> UserTelegramPermissionUpdateAsync(UserTelegramPermissionSetModel req, CancellationToken token = default);

    /// <inheritdoc/>
    public Task<List<ChatTelegramModelDB>> ChatsFindForUserTelegramAsync(long[] req, CancellationToken token = default);

    /// <inheritdoc/>
    public Task<List<ChatTelegramModelDB>> ChatsReadTelegramAsync(long[] req, CancellationToken token = default);

    /// <inheritdoc/>
    public Task<TPaginationResponseModel<ChatTelegramModelDB>> ChatsSelectTelegramAsync(TPaginationRequestStandardModel<string?> req, CancellationToken token = default);

    /// <inheritdoc/>
    public Task<ChatTelegramModelDB> ChatTelegramReadAsync(int chatId, CancellationToken token = default);

    /// <inheritdoc/>
    public Task<TResponseModel<MessageComplexIdsModel>> ForwardMessageTelegramAsync(ForwardMessageTelegramBotModel req, CancellationToken token = default);

    /// <inheritdoc/>
    public Task<TResponseModel<byte[]>> GetFileTelegramAsync(string req, CancellationToken token = default);

    /// <inheritdoc/>
    public Task<TPaginationResponseModel<MessageTelegramModelDB>> MessagesSelectTelegramAsync(TPaginationRequestStandardModel<SearchMessagesChatModel> req, CancellationToken token = default);
}