using Microsoft.AspNetCore.Components.Web;
using BlazorLib.Components.StockSharp;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using BlazorLib;
using SharedLib;
using MudBlazor;

namespace StockSharpMauiApp.Components.Shared;

public partial class ConnectionPanelComponent : StockSharpBaseComponent
{
    [Inject]
    ITelegramBotStandardTransmission TelegramRepo { get; set; } = default!;

    [Inject]
    IDataStockSharpService DataRepo { get; set; } = default!;

    [Inject]
    IEventsStockSharpService EventsNotifyRepo { get; set; } = default!;

    [Inject]
    IEventNotifyReceive<PortfolioStockSharpViewModel> PortfolioEventRepo { get; set; } = default!;

    [Inject]
    IEventNotifyReceive<UpdateConnectionHandleModel> UpdateConnectionEventRepo { get; set; } = default!;


    private bool _visibleStrategyBoard;
    private readonly DialogOptions _dialogOptions = new() { FullWidth = true };


    string ConnectionStateStyles => AboutConnection is null
        ? ""
        : AboutConnection.ConnectionState == ConnectionStatesEnum.Connected
            ? " text-success"
            : " text-warning";

    UserTelegramBaseModel? aboutBot;

    readonly List<PortfolioStockSharpViewModel> portfolios = [];

    List<BoardStockSharpModel>? allBoards;
    BoardStockSharpModel? SelectedBoard { get; set; }

    bool CanStarted => AboutConnection?.ConnectionState == ConnectionStatesEnum.Connected && !AboutConnection.StrategyStarted;
    bool CanStopped => AboutConnection?.ConnectionState == ConnectionStatesEnum.Connected && AboutConnection.StrategyStarted;

    bool CanConnect => AboutConnection?.ConnectionState == ConnectionStatesEnum.Disconnected;
    bool CanDisconnect => AboutConnection?.ConnectionState == ConnectionStatesEnum.Connected;

    PortfolioStockSharpModel? SelectedPortfolio { get; set; }

    async Task StartTradeAsync()
    {

        //if (RowsComponents.Any(x => !x.Available))
        //{
        //    SnackbarRepo.Add("Instruments not initialized!", MudBlazor.Severity.Error);
        //    return;
        //}

        StrategyStartRequestModel req = new()
        {
            Board = SelectedBoard,
            SelectedPortfolio = SelectedPortfolio
        };
        await SetBusyAsync();
        ResponseBaseModel res = await DriverRepo.StartStrategy(req);
        SnackbarRepo.ShowMessagesResponse(res.Messages);

        await SetBusyAsync(false);
        _visibleStrategyBoard = false;
    }
    async Task StopTradeAsync()
    {
        StrategyStopRequestModel req = new();
        await SetBusyAsync();
        ResponseBaseModel res = await DriverRepo.StopStrategy(req);
        SnackbarRepo.ShowMessagesResponse(res.Messages);
        await SetBusyAsync(false);

        _visibleStrategyBoard = false;
    }
    async Task DownloadBaseAsync()
    {
        await SetBusyAsync();
        await SetBusyAsync(false);
    }

    async Task Connect()
    {
        ConnectRequestModel req = new()
        {

        };
        await Connect(req);
        if (AboutConnection is not null)
            await EventsNotifyRepo.UpdateConnectionHandle(new UpdateConnectionHandleModel() { CanConnect = AboutConnection.CanConnect, ConnectionState = AboutConnection.ConnectionState });
        await SetBusyAsync(false);
    }

    protected  async Task TerminateConnection()
    {
        await base.GetStatusConnection();
        if (AboutConnection?.Messages.Count == 0)
        {
            SnackbarRepo.Info(AboutConnection.ConnectionState.ToString() ?? "error");
        }
    }

    protected override async Task GetStatusConnection()
    {
        await base.GetStatusConnection();
        if (AboutConnection?.Messages.Count == 0)
        {
            SnackbarRepo.Info(AboutConnection.ConnectionState.ToString() ?? "error");
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
        SnackbarRepo.ShowMessagesResponse(rest.Messages);
        aboutBot = rest.Response;
        SnackbarRepo.Add($"TelegramBot: {JsonConvert.SerializeObject(aboutBot)}");
    }

    void PortfolioNotificationHandle(PortfolioStockSharpViewModel model)
    {
        lock (portfolios)
        {
            int _pf = portfolios.FindIndex(x => x.Id == model.Id);
            if (_pf < 0)
                portfolios.Add(model);
            else
                portfolios[_pf].Reload(model);
        }
        StateHasChangedCall();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await PortfolioEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.PortfolioReceivedStockSharpNotifyReceive, PortfolioNotificationHandle);
        await UpdateConnectionEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.UpdateConnectionStockSharpNotifyReceive, UpdateConnectionNotificationHandle);

        await AboutBotAsync();

        await Task.WhenAll([
              Task.Run(async () => {
                TResponseModel<List<BoardStockSharpModel>> res = await DataRepo.GetBoardsAsync();
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

    private void UpdateConnectionNotificationHandle(UpdateConnectionHandleModel req)
    {
        AboutConnection?.Update(req);
        InvokeAsync(GetStatusConnection);
    }

    public override void Dispose()
    {
        PortfolioEventRepo.UnregisterAction();
        UpdateConnectionEventRepo.UnregisterAction();
        base.Dispose();
    }
}