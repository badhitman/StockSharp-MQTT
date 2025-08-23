////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib;
using Microsoft.AspNetCore.Components;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared.InstrumentsTable;

public partial class InstrumentTableRowComponent : BlazorBusyComponentBaseModel
{
    [Inject]
    protected IEventNotifyReceive<InstrumentTradeStockSharpViewModel> InstrumentEventRepo { get; set; } = default!;


    [Parameter, EditorRequired]
    public required InstrumentTradeStockSharpViewModel Context { get; set; }

    [Parameter, EditorRequired]
    public required InstrumentsTableStockSharpComponent Owner { get; set; }

    protected async override Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await InstrumentEventRepo.RegisterAction($"{GlobalStaticConstantsTransmission.TransmissionQueues.InstrumentReceivedStockSharpNotifyReceive}:{Context.Id}", InstrumentNotificationHandle);
    }

    void InstrumentNotificationHandle(InstrumentTradeStockSharpViewModel model)
    {
        model.Markers = Context.Markers;
        Context.Reload(model);
        StateHasChangedCall();
    }

    public override void Dispose()
    {
        InstrumentEventRepo.UnregisterAction();
        base.Dispose();
    }
}