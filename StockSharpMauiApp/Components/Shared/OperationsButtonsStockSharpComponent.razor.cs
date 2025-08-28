////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SharedLib;
using System.Diagnostics.Metrics;

namespace StockSharpMauiApp.Components.Shared;

public partial class OperationsButtonsStockSharpComponent : BlazorBusyComponentBaseModel
{
    [Inject]
    IParametersStorageTransmission StorageRepo { get; set; } = default!;


    [Parameter, EditorRequired]
    public bool Available { get; set; }

    [Parameter, EditorRequired]
    public int InstrumentId { get; set; }

    DashboardTradeStockSharpModel? RestoreStrategy;
    public bool _available;
    bool _visible;

    decimal price, volume;
    SidesEnum side;
    // $"Do you really want to {(side == SidesEnum.Buy ? "BUY" : "SELL")} {volume.ToString("#")} {RestoreStrategy?.Name} bonds @ {price.ToString("#.##")}"
    string AboutOrder => "Do you really want to " + (side == SidesEnum.Buy ? "BUY" : "SELL") + " " + volume.ToString("#") + " " + RestoreStrategy?.Name + " bonds @ " + price.ToString("#.##");

    private readonly DialogOptions _dialogOptions = new() { FullWidth = true };

    private void Submit() => _visible = false;

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

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _available = Available;
    }
}