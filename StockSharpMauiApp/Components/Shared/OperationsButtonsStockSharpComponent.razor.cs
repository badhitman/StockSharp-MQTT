////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib;
using Microsoft.AspNetCore.Components;
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

    decimal price, volume;
    SidesEnum side;
    OrderTypesEnum selectedOrderType;
    int selectedPortfolio;

    List<PortfolioStockSharpViewModel>? portfoliosAll;

    private readonly DialogOptions _dialogOptions = new() { FullWidth = true };

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

            OrderType = selectedOrderType,
            PortfolioId = selectedPortfolio
        };
        await SetBusyAsync();
        ResponseBaseModel res = await DriverRepo.OrderRegisterAsync(req);
        SnackBarRepo.ShowMessagesResponse(res.Messages);
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
        volume = RestoreStrategy.WorkingVolume;
        side = SidesEnum.Buy;

        if (price * volume == 0)
        {
            SnackBarRepo.Warn("price * volume == 0");
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
        volume = RestoreStrategy.WorkingVolume;

        if (price * volume == 0)
        {
            SnackBarRepo.Warn("price * volume == 0");
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
        volume = RestoreStrategy.WorkingVolume;

        if (volume > 30000)
            volume = 30000;

        if (price * volume == 0)
            return;

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
        volume = RestoreStrategy.WorkingVolume;

        if (volume > 30000)
            volume = 30000;

        if (price * volume == 0)
            return;

        _visible = true;
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
        TResponseModel<List<PortfolioStockSharpViewModel>> res = await DataRepo.GetPortfoliosAsync();
        portfoliosAll = res.Response;
        SnackBarRepo.ShowMessagesResponse(res.Messages);
        await SetBusyAsync(false);
    }
}