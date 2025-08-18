using Microsoft.AspNetCore.Components.Web;
using BlazorLib.Components.StockSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using BlazorLib;
using MudBlazor;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared;

public partial class ConnectionPanelComponent : StockSharpBaseComponent
{
    [Inject]
    IJSRuntime JS { get; set; } = default!;

    [Inject]
    ITelegramBotStandardTransmission TelegramRepo { get; set; } = default!;

    [Inject]
    IDataStockSharpService DataRepo { get; set; } = default!;

    [Inject]
    IEventsStockSharp EventsNotifyRepo { get; set; } = default!;

    [Inject]
    IEventNotifyReceive<PortfolioStockSharpViewModel> PortfolioEventRepo { get; set; } = default!;

    [Inject]
    IEventNotifyReceive<UpdateConnectionHandleModel> UpdateConnectionEventRepo { get; set; } = default!;

    [Inject]
    IEventNotifyReceive<ToastShowClientModel> ToastClientRepo { get; set; } = default!;


    bool _visibleInitialQuestionsDownload;
    bool _visibleStrategyBoard;
    readonly DialogOptions _dialogOptions = new() { FullWidth = true };


    string ConnectionStateStyles => AboutConnection is null
        ? ""
        : AboutConnection.ConnectionState == ConnectionStatesEnum.Connected
            ? " text-success"
            : " text-warning";

    UserTelegramBaseModel? aboutBot;
    ResponseSimpleModel? InitialLoadCheck;
    readonly List<PortfolioStockSharpViewModel> portfolios = [];

    List<BoardStockSharpViewModel>? allBoards;
    BoardStockSharpViewModel? SelectedBoard { get; set; }

    bool CanStarted => AboutConnection?.ConnectionState == ConnectionStatesEnum.Connected && !AboutConnection.StrategyStarted;
    bool CanStopped => AboutConnection?.ConnectionState == ConnectionStatesEnum.Connected && AboutConnection.StrategyStarted;
    bool CanInitialLoad => AboutConnection?.ConnectionState == ConnectionStatesEnum.Connected && !AboutConnection.StrategyStarted;

    bool CanConnect => AboutConnection?.ConnectionState == ConnectionStatesEnum.Disconnected;
    bool CanDisconnect => AboutConnection?.ConnectionState == ConnectionStatesEnum.Connected;
    readonly InitialLoadRequestModel reqDownloadBase = new() { BigPriceDifferences = [] };
    PortfolioStockSharpModel? SelectedPortfolio { get; set; }

    async Task StartTradeAsync()
    {
        StrategyStartRequestModel req = new()
        {
            Board = SelectedBoard,
            SelectedPortfolio = SelectedPortfolio
        };
        await SetBusyAsync();
        ResponseBaseModel res = await DriverRepo.StartStrategy(req);
        SnackBarRepo.ShowMessagesResponse(res.Messages);
        await SetBusyAsync(false);
        _visibleStrategyBoard = false;
        await GetStatusConnection();
    }
    async Task StopTradeAsync()
    {
        StrategyStopRequestModel req = new();
        await SetBusyAsync();
        ResponseBaseModel res = await DriverRepo.StopStrategy(req);
        SnackBarRepo.ShowMessagesResponse(res.Messages);
        await SetBusyAsync(false);
        _visibleStrategyBoard = false;
        await GetStatusConnection();
    }

    void DisallowBigPriceDifference()
    {
        SnackBarRepo.Warn($"Disallow BigPriceDifference for instrument #{InitialLoadCheck?.Response}");
        _visibleInitialQuestionsDownload = false;
        reqDownloadBase.BigPriceDifferences.Clear();
    }

    async Task AllowBigPriceDifferenceAsync()
    {
        reqDownloadBase.BigPriceDifferences.Add(InitialLoadCheck?.Response ?? throw new Exception(nameof(AllowBigPriceDifferenceAsync)));
        _visibleInitialQuestionsDownload = false;
        await DownloadBaseAsync();
    }

    async Task DownloadBaseAsync()
    {
        await SetBusyAsync();
        InitialLoadCheck = await DriverRepo.InitialLoad(reqDownloadBase);
        await SetBusyAsync(false);

        if (!string.IsNullOrWhiteSpace(InitialLoadCheck.Response))
            _visibleInitialQuestionsDownload = true;
        else
            await GetStatusConnection();
    }

    async Task Connect()
    {
        ConnectRequestModel req = new() { };
        await Connect(req);
        if (AboutConnection is not null)
            await EventsNotifyRepo.UpdateConnectionHandle(new UpdateConnectionHandleModel() { CanConnect = AboutConnection.CanConnect, ConnectionState = AboutConnection.ConnectionState });
        await SetBusyAsync(false);
    }

    protected async Task TerminateConnection()
    {
        ResponseBaseModel res = await DriverRepo.Terminate();
        SnackBarRepo.ShowMessagesResponse(res.Messages);
        await GetStatusConnection();
        if (AboutConnection?.Messages.Count == 0)
        {
            SnackBarRepo.Info(AboutConnection.ConnectionState.ToString() ?? "error");
        }
    }

    string _myConnectStyles = "secondary";
    string? MyConnectStyles => AboutConnection is null || AboutConnection.ConnectionState != ConnectionStatesEnum.Connected
        ? $"outline-{_myConnectStyles}"
        : "success";

    string? MyConnectTitle { get; set; }
    void MyConnectMouseOver(MouseEventArgs e)
    {
        _myConnectStyles = AboutConnection?.ConnectionState == ConnectionStatesEnum.Disconnected
            ? "primary"
            : "secondary";

        if (AboutConnection is null)
        {
            MyConnectTitle = "Diagnostic...";
        }
        else if (AboutConnection.ConnectionState == ConnectionStatesEnum.Connected)
        {
            MyConnectTitle = "Connected!";
        }
        else
        {
            MyConnectTitle = "Connect";
        }
    }
    void MyConnectMouseOut(MouseEventArgs e)
    {
        _myConnectStyles = "secondary";
        MyConnectTitle = null;
    }

    string? CheckConnectTitle { get; set; }
    void CheckConnectMouseOver(MouseEventArgs e)
    {
        CheckConnectTitle = "Check";
    }
    void CheckConnectMouseOut(MouseEventArgs e)
    {
        CheckConnectTitle = "";
    }

    string? DisconnectTitle { get; set; }
    void DisconnectMouseOver(MouseEventArgs e)
    {
        DisconnectTitle = "Close";
    }
    void DisconnectMouseOut(MouseEventArgs e)
    {
        DisconnectTitle = "";
    }

    async Task AboutBotAsync()
    {
        TResponseModel<UserTelegramBaseModel> rest = await TelegramRepo.AboutBotAsync();
        SnackBarRepo.ShowMessagesResponse(rest.Messages);
        aboutBot = rest.Response;
        SnackBarRepo.Info($"TelegramBot: {JsonConvert.SerializeObject(aboutBot)}");
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await PortfolioEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.PortfolioReceivedStockSharpNotifyReceive, PortfolioNotificationHandle);
        await UpdateConnectionEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.UpdateConnectionStockSharpNotifyReceive, UpdateConnectionNotificationHandle);
        await ToastClientRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.ToastClientShowStockSharpNotifyReceive, ToastShowHandle);

        await AboutBotAsync();

        await Task.WhenAll([
              Task.Run(async () => {
                TResponseModel<List<BoardStockSharpViewModel>> res = await DataRepo.GetBoardsAsync();
                allBoards = res.Response;
            }),
            Task.Run(async () => {
                TResponseModel<List<PortfolioStockSharpViewModel>> res = await DataRepo.GetPortfoliosAsync();
                lock (portfolios)
                {
                    portfolios.Clear();
                    if(res.Response is not null)
                        portfolios.AddRange(res.Response);
                }
            })]);
    }

    private void ToastShowHandle(ToastShowClientModel toast)
    {
        InvokeAsync(async () => await JS.InvokeVoidAsync($"Toast.{toast.TypeMessage}", toast.HeadTitle, toast.MessageText));
        SnackBarRepo.SaveToast(toast);
    }

    void PortfolioNotificationHandle(PortfolioStockSharpViewModel model)
    {
        lock (portfolios)
        {
            int _pf = portfolios.FindIndex(x => x.Id == model.Id);
            if (_pf == -1)
                portfolios.Add(model);
            else
                portfolios[_pf].Reload(model);
        }
        StateHasChangedCall();
    }

    private void UpdateConnectionNotificationHandle(UpdateConnectionHandleModel req)
    {
        AboutConnection?.Update(req);
        InvokeAsync(GetStatusConnection);
    }

    public override void Dispose()
    {
        PortfolioEventRepo.UnregisterAction();
        UpdateConnectionEventRepo.UnregisterAction();
        ToastClientRepo.UnregisterAction();
        base.Dispose();
    }
}