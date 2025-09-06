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
            ReplyKeyboard = [
                [new ButtonActionModel() { Title = "demo 1.0", Data = "data-1" }],
                [new ButtonActionModel() { Title = "test 2.1", Data = "test-2-1" }, new ButtonActionModel() { Title = "test 2.2", Data = "test-2-2" }]
            ],
            Response = $"Hi {tgDialog.TelegramUser!.GetName()}"
        });
    }
}
