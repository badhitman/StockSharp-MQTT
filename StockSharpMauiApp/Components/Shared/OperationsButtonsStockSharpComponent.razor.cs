////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.AspNetCore.Components;
using BlazorLib;
using MudBlazor;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared;

public partial class OperationsButtonsStockSharpComponent : StockSharpBaseComponent
{
    [Inject]
    IParametersStorageTransmission StorageRepo { get; set; } = default!;

    [Inject]
    IDataStockSharpService DataRepo { get; set; } = default!;

    [Inject]
    IEventNotifyReceive<PortfolioStockSharpViewModel> PortfolioEventRepo { get; set; } = default!;


    [Parameter, EditorRequired]
    public bool Available { get; set; }

    [Parameter, EditorRequired]
    public int InstrumentId { get; set; }


    DashboardTradeStockSharpModel? RestoreStrategy;
    public bool _available;
    bool _visible;

    decimal price;
    decimal Volume { get; set; }

    List<PortfolioStockSharpViewModel> portfoliosAll = [];

    private readonly DialogOptions _dialogOptions = new() { FullWidth = true };

    SidesEnum side;

    OrderTypesEnum _selectedOrderType;
    OrderTypesEnum SelectedOrderType
    {
        get => _selectedOrderType;
        set
        {
            _selectedOrderType = value;
            InvokeAsync(async () => { await StorageRepo.SaveParameterAsync(value, GlobalStaticCloudStorageMetadata.DashboardTradeOrderType, true); });
        }
    }

    int _selectedPortfolioId;
    int SelectedPortfolioId
    {
        get => _selectedPortfolioId;
        set
        {
            _selectedPortfolioId = value;
            InvokeAsync(async () => { await StorageRepo.SaveParameterAsync(value, GlobalStaticCloudStorageMetadata.DashboardTradePortfolio, true); });
        }
    }

    public void AvailableSet(bool available)
    {
        _available = available;
        StateHasChangedCall();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _available = Available;
        await SetBusyAsync();
        TResponseModel<int> tradePortfolio = default!;
        await Task.WhenAll([
            PortfolioEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.PortfolioReceivedStockSharpNotifyReceive, PortfolioNotificationHandle),
            Task.Run(async () => {
                tradePortfolio = await StorageRepo.ReadParameterAsync<int>(GlobalStaticCloudStorageMetadata.DashboardTradePortfolio);
            }),
            Task.Run(async () => {
                TResponseModel<OrderTypesEnum?> orderType = await StorageRepo.ReadParameterAsync<OrderTypesEnum?>(GlobalStaticCloudStorageMetadata.DashboardTradeOrderType);
                if(orderType.Success() && orderType.Response is not null)
                    _selectedOrderType = orderType.Response.Value;
            }),
            Task.Run(async () => {
                TResponseModel<List<PortfolioStockSharpViewModel>> res = await DataRepo.GetPortfoliosAsync();
                if(res.Response is not null)
                {
                    lock (portfoliosAll)
                    {
                        portfoliosAll.AddRange(res.Response);
                    }
                }

                SnackBarRepo.ShowMessagesResponse(res.Messages);
            }),
        ]);

        if (tradePortfolio.Success() && tradePortfolio.Response != default)
            _selectedPortfolioId = tradePortfolio.Response;

        if (portfoliosAll is not null && portfoliosAll.Count != 0 && (SelectedPortfolioId == default || !portfoliosAll.Any(x => x.Id == SelectedPortfolioId)))
            _selectedPortfolioId = portfoliosAll.First().Id;

        await SetBusyAsync(false);
    }

    void PortfolioNotificationHandle(PortfolioStockSharpViewModel model)
    {
        lock (portfoliosAll)
        {
            int _pf = portfoliosAll.FindIndex(x => x.Id == model.Id);
            if (_pf == -1)
                portfoliosAll.Add(model);
            else
                portfoliosAll[_pf].Reload(model);
        }
        StateHasChangedCall();
    }

    async Task Submit()
    {
        PortfolioStockSharpViewModel? _currPort = portfoliosAll.FirstOrDefault(x => x.Id == SelectedPortfolioId);
        if (AboutConnection is null)
        {
            SnackBarRepo.Error("AboutConnection is null");
            return;
        }
        if (_currPort is null)
        {
            SnackBarRepo.Warn($"Current portfolio wrong: not found #{SelectedPortfolioId}");
            return;
        }
        if (_currPort.LastUpdatedAtUTC < AboutConnection.LastConnectedAt)
        {
            SnackBarRepo.Warn($"Current portfolio wrong: not actuality portfolio `{_currPort.LastUpdatedAtUTC.GetHumanDateTime()}` vs CurrentConnection `{AboutConnection.LastConnectedAt.Value.GetHumanDateTime()}`");
            return;
        }

        CreateOrderRequestModel req = new()
        {
            Comment = "Manual",
            InstrumentId = InstrumentId,
            IsManual = true,
            Price = price,
            Volume = Volume,
            Side = side,

            OrderType = SelectedOrderType,
            PortfolioId = SelectedPortfolioId
        };

        await SetBusyAsync();
        ResponseBaseModel res = await DriverRepo.OrderRegisterAsync(req);
        SnackBarRepo.ShowMessagesResponse(res.Messages);

        if (res.Success())
            _visible = false;

        await SetBusyAsync(false);
    }

    async Task Buy()
    {
        await SetBusyAsync();
        TResponseModel<DashboardTradeStockSharpModel?> restoreStrategy = await StorageRepo.ReadParameterAsync<DashboardTradeStockSharpModel>(GlobalStaticCloudStorageMetadata.TradeInstrumentStrategyStockSharp(InstrumentId));
        RestoreStrategy = restoreStrategy.Response;
        await SetBusyAsync(false);
        if (RestoreStrategy is null)
        {
            SnackBarRepo.Error("RestoreStrategy is null");
            return;
        }

        price = RestoreStrategy.BasePrice;
        Volume = RestoreStrategy.ValueOperation;
        side = SidesEnum.Buy;

        if (price * Volume == 0)
        {
            SnackBarRepo.Warn("price or volume == 0");
            return;
        }

        _visible = true;
    }

    async Task Sell()
    {
        await SetBusyAsync();
        TResponseModel<DashboardTradeStockSharpModel?> restoreStrategy = await StorageRepo.ReadParameterAsync<DashboardTradeStockSharpModel>(GlobalStaticCloudStorageMetadata.TradeInstrumentStrategyStockSharp(InstrumentId));
        RestoreStrategy = restoreStrategy.Response;
        await SetBusyAsync(false);
        if (RestoreStrategy is null)
        {
            SnackBarRepo.Error("RestoreStrategy is null");
            return;
        }

        side = SidesEnum.Sell;
        price = RestoreStrategy.BasePrice;
        Volume = RestoreStrategy.ValueOperation;

        if (price * Volume == 0)
        {
            SnackBarRepo.Warn("price or volume == 0");
            return;
        }

        _visible = true;
    }

    async Task Bid()
    {
        await SetBusyAsync();
        TResponseModel<DashboardTradeStockSharpModel?> restoreStrategy = await StorageRepo.ReadParameterAsync<DashboardTradeStockSharpModel>(GlobalStaticCloudStorageMetadata.TradeInstrumentStrategyStockSharp(InstrumentId));
        RestoreStrategy = restoreStrategy.Response;
        await SetBusyAsync(false);
        if (RestoreStrategy is null)
        {
            SnackBarRepo.Error("RestoreStrategy is null");
            return;
        }

        side = SidesEnum.Buy;
        price = RestoreStrategy.BasePrice;
        Volume = RestoreStrategy.ValueOperation;

        if (Volume > 30000)
            Volume = 30000;

        if (price * Volume == 0)
        {
            SnackBarRepo.Warn("price or volume == 0");
            return;
        }

        _visible = true;
    }

    async Task Ask()
    {
        await SetBusyAsync();
        TResponseModel<DashboardTradeStockSharpModel?> restoreStrategy = await StorageRepo.ReadParameterAsync<DashboardTradeStockSharpModel>(GlobalStaticCloudStorageMetadata.TradeInstrumentStrategyStockSharp(InstrumentId));
        RestoreStrategy = restoreStrategy.Response;
        await SetBusyAsync(false);
        if (RestoreStrategy is null)
        {
            SnackBarRepo.Error("RestoreStrategy is null");
            return;
        }

        side = SidesEnum.Sell;
        price = RestoreStrategy.BasePrice;
        Volume = RestoreStrategy.ValueOperation;

        if (Volume > 30000)
            Volume = 30000;

        if (price * Volume == 0)
        {
            SnackBarRepo.Warn("price or volume == 0");
            return;
        }

        _visible = true;
    }
    public override void Dispose()
    {
        PortfolioEventRepo.UnregisterAction();
        base.Dispose();
    }
}