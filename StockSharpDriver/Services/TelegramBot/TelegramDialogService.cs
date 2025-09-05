using SharedLib;
using StockSharpDriver;

namespace Telegram.Bot.Services;

public class TelegramDialogService : ITelegramDialogService
{
    public Task<TelegramDialogResponseModel> TelegramDialogHandleAsync(TelegramDialogRequestModel tgDialog, CancellationToken token = default)
    {
        return Task.FromResult(new TelegramDialogResponseModel()
        {
            MainTelegramMessageId = tgDialog.MessageTelegramId,
            ReplyKeyboard = [[new ButtonActionModel() { Title = "demo 1", Data = "data-1" }]],
            Response = $"Hi {tgDialog.TelegramUser!.GetName()}"
        });
    }
}
