using BlazorLib.Components.StockSharp;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using BlazorLib;
using SharedLib;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace StockSharpMauiApp.Components.Shared;

public partial class ConnectionPanelComponent : StockSharpBaseComponent
{
    [Inject]
    ITelegramBotStandardTransmission TelegramRepo { get; set; } = default!;

    [Inject]
    IDataStockSharpService DataRepo { get; set; } = default!;

    [Inject]
    IEventNotifyReceive<PortfolioStockSharpViewModel> PortfolioEventRepo { get; set; } = default!;

    [Inject]
    protected IEventsStockSharpService EventsNotifyRepo { get; set; } = default!;

    [Inject]
    protected IEventNotifyReceive<UpdateConnectionHandleModel> UpdateConnectionEventRepo { get; set; } = default!;


    private bool _visible;
    private readonly DialogOptions _dialogOptions = new() { FullWidth = true };

    private void Submit() => _visible = false;

    string ConnectionStateStyles => AboutConnection is null
        ? ""
        : AboutConnection.ConnectionState == ConnectionStatesEnum.Connected
            ? " text-success"
            : " text-warning";

    UserTelegramBaseModel? aboutBot;

    readonly List<PortfolioStockSharpViewModel> portfolios = [];

    List<BoardStockSharpModel>? allBoards;
    BoardStockSharpModel? SelectedBoard { get; set; }
    bool CantStarted => AboutConnection?.ConnectionState != ConnectionStatesEnum.Connected;

    bool CanConnect => AboutConnection?.ConnectionState == ConnectionStatesEnum.Disconnected;

    bool CanDisconnect => AboutConnection?.ConnectionState == ConnectionStatesEnum.Connected;

    PortfolioStockSharpModel? SelectedPortfolio { get; set; }

    async Task StartTradeAsync()
    {
        _visible = true;

        //if (RowsComponents.Any(x => !x.Available))
        //{
        //    SnackbarRepo.Add("Instruments not initialized!", MudBlazor.Severity.Error);
        //    return;
        //}

        //StrategyStartRequestModel req = new()
        //{
        //    Instruments = [.. RowsComponents.Select(x => x.StrategyTrade)],
        //    Board = SelectedBoard,
        //};
        await SetBusyAsync();
        //ResponseBaseModel res = await DriverRepo.StrategyStartAsync(req);
        //SnackbarRepo.ShowMessagesResponse(res.Messages);
        await SetBusyAsync(false);
    }
    async Task StopTradeAsync()
    {
        StrategyStopRequestModel req = new();
        await SetBusyAsync();
        ResponseBaseModel res = await DriverRepo.StrategyStopAsync(req);
        SnackbarRepo.ShowMessagesResponse(res.Messages);
        await SetBusyAsync(false);
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
        await GetStatusConnection();
        if (AboutConnection is not null)
            await EventsNotifyRepo.UpdateConnectionHandle(new UpdateConnectionHandleModel() { CanConnect = AboutConnection.CanConnect, ConnectionState = AboutConnection.ConnectionState });
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
        StateHasChangedCall();
    }

    public override void Dispose()
    {
        PortfolioEventRepo.UnregisterAction();
        UpdateConnectionEventRepo.UnregisterAction();
        base.Dispose();
    }
}