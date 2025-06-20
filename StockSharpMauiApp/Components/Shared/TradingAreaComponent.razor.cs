////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib.Components.StockSharp;
using Microsoft.AspNetCore.Components;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared;

/// <summary>
/// TradingAreaComponent
/// </summary>
public partial class TradingAreaComponent : StockSharpBaseComponent
{
    /// <inheritdoc/>
    [Inject]
    protected IDataStockSharpService DataRepo { get; set; } = default!;

    [Inject]
    protected IEventNotifyReceive<InstrumentTradeStockSharpViewModel> InstrumentEventRepo { get; set; } = default!;

    [Inject]
    protected IEventNotifyReceive<UpdateConnectionHandleModel> UpdateConnectionEventRepo { get; set; } = default!;

    bool ShowNamesInstruments { get; set; }

    int QuoteVolume { get; set; }
    int QuoteSizeVolume { get; set; }
    int SkipSizeVolume { get; set; }

    readonly List<InstrumentTradeStockSharpViewModel> instruments = [];

    List<TradingRowComponent> RowsComponents { get; set; } = [];

    public void AddRowComponent(TradingRowComponent sender)
    {
        lock (RowsComponents)
        {
            RowsComponents.Add(sender);
        }
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await SetBusyAsync();

        await UpdateConnectionEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.UpdateConnectionStockSharpNotifyReceive, UpdateConnectionNotificationHandle);
        await InstrumentEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.InstrumentReceivedStockSharpNotifyReceive, InstrumentNotificationHandle);

        InstrumentsRequestModel req = new()
        {
            PageNum = 0,
            PageSize = int.MaxValue,
            FavoriteFilter = true,
        };
        TPaginationResponseModel<InstrumentTradeStockSharpViewModel> res = await DataRepo.InstrumentsSelectAsync(req);
        lock (instruments)
        {
            instruments.Clear();
            if (res.Response is not null)
                instruments.AddRange(res.Response);
        }

        await SetBusyAsync(false);
    }

    private void UpdateConnectionNotificationHandle(UpdateConnectionHandleModel req)
    {
        InvokeAsync(async () =>
        {
            await GetStatusConnection();
            if (AboutConnection is null)
                throw new Exception();

            RowsComponents.ForEach(rc => rc.UpdateConnectionNotificationHandle(AboutConnection));
            StateHasChangedCall();
        });
    }

    void InstrumentNotificationHandle(InstrumentTradeStockSharpViewModel model)
    {
        lock (instruments)
        {
            if (instruments.Count == 0)
                return;

            int _i = -1;
            _i = RowsComponents.FindIndex(x => x.Instrument.Id == model.Id);
            if (_i != -1)
            {
                model.Markers = RowsComponents[_i].Instrument.Markers;
                RowsComponents[_i].Update(model);
                StateHasChangedCall();
            }
        }
    }

    public override void Dispose()
    {
        InstrumentEventRepo.UnregisterAction();
        UpdateConnectionEventRepo.UnregisterAction();
        base.Dispose();
    }
}