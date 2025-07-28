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
public partial class TradingAreaComponent : StockSharpAboutComponent
{
    /// <inheritdoc/>
    [Inject]
    protected IDataStockSharpService DataRepo { get; set; } = default!;

    [Inject]
    protected IEventNotifyReceive<InstrumentTradeStockSharpViewModel> InstrumentEventRepo { get; set; } = default!;

    [Inject]
    protected IEventNotifyReceive<UpdateConnectionHandleModel> UpdateConnectionEventRepo { get; set; } = default!;

    [Inject]
    IParametersStorageTransmission StorageRepo { get; set; } = default!;


    bool ShowNamesInstruments { get; set; }

    decimal _quoteVolume;
    decimal QuoteVolume
    {
        get => _quoteVolume;
        set
        {
            _quoteVolume = value;
            InvokeAsync(async () => { await StorageRepo.SaveParameterAsync(_quoteVolume, GlobalStaticCloudStorageMetadata.QuoteVolume, true, false); });
        }
    }

    decimal _quoteSizeVolume;
    decimal QuoteSizeVolume
    {
        get => _quoteSizeVolume;
        set
        {
            _quoteSizeVolume = value;
            InvokeAsync(async () => { await StorageRepo.SaveParameterAsync(_quoteSizeVolume, GlobalStaticCloudStorageMetadata.QuoteSizeVolume, true, false); });
        }
    }

    decimal _skipSizeVolume;
    decimal SkipSizeVolume
    {
        get => _skipSizeVolume;
        set
        {
            _skipSizeVolume = value;
            InvokeAsync(async () => { await StorageRepo.SaveParameterAsync(_skipSizeVolume, GlobalStaticCloudStorageMetadata.SkipSizeVolume, true, false); });
        }
    }

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

        TResponseModel<MarkersInstrumentStockSharpEnum?[]?> _readMarkersFilter = await StorageRepo.ReadParameterAsync<MarkersInstrumentStockSharpEnum?[]?>(GlobalStaticCloudStorageMetadata.MarkersDashboard);
        MarkersInstrumentStockSharpEnum?[]? _markersSelected = _readMarkersFilter.Response;

        await Task.WhenAll([
                Task.Run(async () =>
                {
                    TResponseModel<decimal> restoreSkipSizeVolume = await StorageRepo.ReadParameterAsync<decimal>(GlobalStaticCloudStorageMetadata.SkipSizeVolume);
                    _skipSizeVolume = restoreSkipSizeVolume.Response;
                }),
                Task.Run(async () =>
                {
                    TResponseModel<decimal> restoreQuoteSizeVolume = await StorageRepo.ReadParameterAsync<decimal>(GlobalStaticCloudStorageMetadata.QuoteSizeVolume);
                    _quoteSizeVolume = restoreQuoteSizeVolume.Response;
                }),
                Task.Run(async () =>
                {
                    TResponseModel<decimal> restoreQuoteVolume = await StorageRepo.ReadParameterAsync<decimal>(GlobalStaticCloudStorageMetadata.QuoteVolume);
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
                    InstrumentsRequestModel req = new()
                    {
                        PageNum = 0,
                        PageSize = int.MaxValue,
                    };

                    if(_markersSelected is not null)
                    {
                        req.MarkersFilter = _markersSelected;
                    }

                    TPaginationResponseModel<InstrumentTradeStockSharpViewModel> res = await DataRepo.InstrumentsSelectAsync(req);
                    lock (instruments)
                    {
                        instruments.Clear();
                        if (res.Response is not null)
                            instruments.AddRange(res.Response);
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