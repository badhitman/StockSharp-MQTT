using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using BlazorLib;
using MudBlazor;
using SharedLib;

namespace StockSharpMauiApp.Components.Layout;

public partial class MainLayout
{
    [Inject]
    ISnackbar SnackbarRepo { get; set; } = default!;

    [Inject]
    ITelegramBotStandardTransmission TelegramRepo { get; set; } = default!;


    private bool _isDarkMode = true;
    UserTelegramBaseModel? aboutBot;

    async Task AboutBotAsync()
    {
        TResponseModel<UserTelegramBaseModel> rest = await TelegramRepo.AboutBotAsync();
        SnackbarRepo.ShowMessagesResponse(rest.Messages);
        aboutBot = rest.Response;
        SnackbarRepo.Add($"TelegramBot: {JsonConvert.SerializeObject(aboutBot)}");
    }

    protected override async Task OnInitializedAsync()
    {
        await AboutBotAsync();
    }
}