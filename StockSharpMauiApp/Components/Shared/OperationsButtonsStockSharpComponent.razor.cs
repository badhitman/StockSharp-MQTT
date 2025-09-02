////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.AspNetCore.Components;
using BlazorLib;
using MudBlazor;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared;

public partial class OperationsButtonsStockSharpComponent : BlazorBusyComponentBaseModel
{
    [Inject]
    IParametersStorageTransmission StorageRepo { get; set; } = default!;

    [Inject]
    IDriverStockSharpService DriverRepo { get; set; } = default!;

    [Inject]
    IDataStockSharpService DataRepo { get; set; } = default!;


    [Parameter, EditorRequired]
    public bool Available { get; set; }

    [Parameter, EditorRequired]
    public int InstrumentId { get; set; }


    DashboardTradeStockSharpModel? RestoreStrategy;
    public bool _available;
    bool _visible;

    decimal price;
    decimal volume { get; set; }

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

    List<PortfolioStockSharpViewModel>? portfoliosAll;

    private readonly DialogOptions _dialogOptions = new() { FullWidth = true };

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

        await Task.WhenAll([
            Task.Run(async () => {
                TResponseModel<int> tradePortfolio = await StorageRepo.ReadParameterAsync<int>(GlobalStaticCloudStorageMetadata.DashboardTradePortfolio);
                if(tradePortfolio.Success() && tradePortfolio.Response != default)
                    _selectedPortfolioId = tradePortfolio.Response;
            }),
            Task.Run(async () => {
                TResponseModel<OrderTypesEnum?> orderType = await StorageRepo.ReadParameterAsync<OrderTypesEnum?>(GlobalStaticCloudStorageMetadata.DashboardTradeOrderType);
                if(orderType.Success() && orderType.Response is not null)
                    _selectedOrderType = orderType.Response.Value;
            }),
            Task.Run(async () => {
                TResponseModel<List<PortfolioStockSharpViewModel>> res = await DataRepo.GetPortfoliosAsync();
                portfoliosAll = res.Response;
                SnackBarRepo.ShowMessagesResponse(res.Messages);
            }),
        ]);

        if (portfoliosAll is not null && portfoliosAll.Count != 0 && (SelectedPortfolioId == default || !portfoliosAll.Any(x => x.Id == SelectedPortfolioId)))
            SelectedPortfolioId = portfoliosAll.First().Id;

        await SetBusyAsync(false);
    }

    async Task Submit()
    {
        CreateOrderRequestModel req = new()
        {
            Comment = "Manual",
            InstrumentId = InstrumentId,
            IsManual = true,
            Price = price,
            Volume = volume,
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
        volume = RestoreStrategy.ValueOperation;
        side = SidesEnum.Buy;

        nFieldRef?.ForceRender(true);

        if (price * volume == 0)
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
        volume = RestoreStrategy.ValueOperation;

        if (price * volume == 0)
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
        volume = RestoreStrategy.ValueOperation;

        if (volume > 30000)
            volume = 30000;

        if (price * volume == 0)
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
        volume = RestoreStrategy.ValueOperation;

        if (volume > 30000)
            volume = 30000;

        if (price * volume == 0)
        {
            SnackBarRepo.Warn("price or volume == 0");
            return;
        }

        _visible = true;
    }
}