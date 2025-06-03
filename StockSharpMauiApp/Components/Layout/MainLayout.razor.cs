using BlazorLib;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SharedLib;

namespace StockSharpMauiApp.Components.Layout;

public partial class MainLayout
{
    [Inject]
    ISnackbar SnackbarRepo { get; set; } = default!;

    [Inject]
    ITelegramBotStandardService TelegramRepo { get; set; } = default!;


    private bool _isDarkMode = true;
    UserTelegramBaseModel? aboutBot;

    async Task AboutBotAsync()
    {
        TResponseModel<UserTelegramBaseModel> rest = await TelegramRepo.AboutBotAsync();
        SnackbarRepo.ShowMessagesResponse(rest.Messages);
        aboutBot = rest.Response;
        SnackbarRepo.Add($"Bot name:{aboutBot.Username}");
    }

    protected override async Task OnInitializedAsync()
    {
        await AboutBotAsync();
    }
}