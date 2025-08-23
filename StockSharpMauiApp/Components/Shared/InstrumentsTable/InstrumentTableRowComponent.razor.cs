////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared.InstrumentsTable;

public partial class InstrumentTableRowComponent : BlazorBusyComponentBaseModel
{
    [Inject]
    protected IEventNotifyReceive<InstrumentTradeStockSharpViewModel> InstrumentEventRepo { get; set; } = default!;

    [Inject]
    IJSRuntime JsRuntimeRepo { get; set; } = default!;


    [Parameter, EditorRequired]
    public required InstrumentTradeStockSharpViewModel Context { get; set; }

    [Parameter, EditorRequired]
    public required InstrumentsTableStockSharpComponent Owner { get; set; }


    InstrumentTradeStockSharpViewModel? manualOrderContext;
    bool ManualOrderCreating;
    private readonly DialogOptions _dialogOptions = new() { FullWidth = true, MaxWidth = MaxWidth.ExtraLarge };


    public void ManualOrder()
    {
        if (Owner.EachDisable)
            return;

        manualOrderContext = GlobalTools.CreateDeepCopy(Context);
        ManualOrderCreating = true;
    }

    protected async override Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await InstrumentEventRepo.RegisterAction($"{GlobalStaticConstantsTransmission.TransmissionQueues.InstrumentReceivedStockSharpNotifyReceive}:{Context.Id}", InstrumentNotificationHandle);
        Owner.AddRowTable(this);
    }

    void InstrumentNotificationHandle(InstrumentTradeStockSharpViewModel model)
    {
        model.Markers = Context.Markers;
        Context.Reload(model);
        StateHasChangedCall();
        InvokeAsync(async () => { await JsRuntimeRepo.InvokeVoidAsync("TradeInstrumentStrategy.ButtonSplash", model.Id); });
    }

    public override void Dispose()
    {
        InstrumentEventRepo.UnregisterAction();
        base.Dispose();
    }
}