////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib.Components.StockSharp;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared;

/// <summary>
/// TradingAreaComponent
/// </summary>
public partial class TradingAreaComponent : StockSharpAboutComponent
{
    /// <inheritdoc/>
    [Inject]
    protected IDataStockSharpService DataRepo { get; set; } = default!;

    [Inject]
    protected IEventNotifyReceive<UpdateConnectionHandleModel> UpdateConnectionEventRepo { get; set; } = default!;

    [Inject]
    IParametersStorageTransmission StorageRepo { get; set; } = default!;

    [Inject]
    protected IEventNotifyReceive<InstrumentTradeStockSharpViewModel> InstrumentEventRepo { get; set; } = default!;


    decimal _quoteVolume;
    decimal QuoteVolume
    {
        get => _quoteVolume;
        set
        {
            _quoteVolume = value;
            InvokeAsync(async () => { await StorageRepo.SaveParameterAsync(_quoteVolume, GlobalStaticCloudStorageMetadata.QuoteStrategyVolume, true, false); });
        }
    }

    decimal _quoteSizeVolume;
    decimal QuoteSizeVolume
    {
        get => _quoteSizeVolume;
        set
        {
            _quoteSizeVolume = value;
            InvokeAsync(async () => { await StorageRepo.SaveParameterAsync(_quoteSizeVolume, GlobalStaticCloudStorageMetadata.QuoteSizeStrategyVolume, true, false); });
        }
    }

    readonly List<InstrumentTradeStockSharpViewModel> instruments = [];
    readonly List<TradingRowComponent> RowsComponents = [];

    /// <summary>
    /// Here we simulate getting the paged, filtered and ordered data from the server
    /// </summary>
    private Task<TableData<InstrumentTradeStockSharpViewModel>> ServerReload(TableState state, CancellationToken token)
    {
        RowsComponents.Clear();
        return Task.FromResult(new TableData<InstrumentTradeStockSharpViewModel>() { TotalItems = instruments.Count, Items = instruments.Skip(state.PageSize * state.Page).Take(state.PageSize) });
    }

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

        await Task.WhenAll([
                Task.Run(async () =>
                {
                    TResponseModel<decimal> restoreQuoteSizeVolume = await StorageRepo.ReadParameterAsync<decimal>(GlobalStaticCloudStorageMetadata.QuoteSizeStrategyVolume);
                    _quoteSizeVolume = restoreQuoteSizeVolume.Response;
                }),
                Task.Run(async () =>
                {
                    TResponseModel<decimal> restoreQuoteVolume = await StorageRepo.ReadParameterAsync<decimal>(GlobalStaticCloudStorageMetadata.QuoteStrategyVolume);
                    _quoteVolume = restoreQuoteVolume.Response;
                }),

                Task.Run(async () =>
                {
                    await UpdateConnectionEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.UpdateConnectionStockSharpNotifyReceive, UpdateConnectionNotificationHandle);
                }),
                Task.Run(async () =>
                {
                    await InstrumentEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.InstrumentReceivedStockSharpNotifyReceive, InstrumentNotificationHandle);
                }),
                Task.Run(async () =>
                {
                    TResponseModel<List<InstrumentTradeStockSharpViewModel>> readTrade = await DataRepo.ReadTradeInstrumentsAsync();
                    lock(instruments)
                    {
                        instruments.Clear();
                        if(readTrade.Response is not null && readTrade.Response.Count != 0)
                            instruments.AddRange(readTrade.Response);
                    }
                })
            ]);

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

            _i = instruments.FindIndex(x => x.Id == model.Id);
            if (_i != -1)
            {
                model.Markers = instruments[_i].Markers;
                instruments[_i].Reload(model);
                instruments[_i].LastUpdatedAtUTC = DateTime.Now;
            }

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