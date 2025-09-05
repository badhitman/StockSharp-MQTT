////////////////////////////////////////////////
// � https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace StockSharpDriver;

/// <summary>
/// TelegramDialog
/// </summary>
public interface ITelegramDialogService
{
    /// <summary>
    /// ��������� �������� ��������� �� Telegram
    /// </summary>
    public Task<TelegramDialogResponseModel> TelegramDialogHandleAsync(TelegramDialogRequestModel tgDialog, CancellationToken token = default);
}