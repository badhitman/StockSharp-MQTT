////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib.Components.StockSharp;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedLib;
using System.Diagnostics.Metrics;

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
    protected IEventNotifyReceive<PortfolioStockSharpViewModel> PortfolioEventRepo { get; set; } = default!;

    [Inject]
    protected IEventNotifyReceive<InstrumentTradeStockSharpViewModel> InstrumentEventRepo { get; set; } = default!;

    [Inject]
    ILogger<TradingAreaComponent> LoggerRepo { get; set; } = default!;

    int QuoteVolume { get; set; }
    int QuoteSizeVolume { get; set; }
    int SkipSizeVolume { get; set; }

    List<InstrumentTradeStockSharpViewModel> instruments = [];
    List<PortfolioStockSharpViewModel> portfolios = [];

    List<BoardStockSharpModel>? allBoards;
    IEnumerable<BoardStockSharpModel>? SelectedBoards { get; set; }

    PortfolioStockSharpModel? SelectedPortfolio { get; set; }

    List<TradingRowComponent> RowsComponents { get; set; } = [];

    public void AddRowComponent(TradingRowComponent sender)
    {
        lock (RowsComponents)
        {
            RowsComponents.Add(sender);
        }
    }

    async Task StartTradeAsync()
    {
        await SetBusyAsync();
        await SetBusyAsync(false);
    }
    async Task StopTradeAsync()
    {
        await SetBusyAsync();
        await SetBusyAsync(false);
    }
    async Task DownloadBaseAsync()
    {
        await SetBusyAsync();
        await SetBusyAsync(false);
    }
    async Task ResetAllTradesAsync()
    {
        await SetBusyAsync();
        await SetBusyAsync(false);
    }
    private string? data;

    void HandleOnChange(ChangeEventArgs args)
    {
        data = args.Value?.ToString();
    }

    async Task Connect()
    {
        ConnectRequestModel req = new()
        {
            BoardsFilter = SelectedBoards is null ? null : [.. SelectedBoards],
            Instruments = instruments,
        };
        await Connect(req);
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await SetBusyAsync();

        await PortfolioEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.PortfolioReceivedStockSharpNotifyReceive, PortfolioNotificationHandle);
        await InstrumentEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.InstrumentReceivedStockSharpNotifyReceive, InstrumentNotificationHandle);

        await Task.WhenAll([
            Task.Run(async () => {
                InstrumentsRequestModel req = new()
                    {
                        PageNum = 0,
                        PageSize = int.MaxValue,
                        FavoriteFilter = true,
                    };
                TPaginationResponseModel<InstrumentTradeStockSharpViewModel> res = await DataRepo.InstrumentsSelectAsync(req);
                lock(instruments)
                {
                    instruments.Clear();
                    if(res.Response is not null)
                        instruments.AddRange(res.Response);
                }
            }),
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

        await SetBusyAsync(false);
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

    void InstrumentNotificationHandle(InstrumentTradeStockSharpViewModel model)
    {
        //data += $"{JsonConvert.SerializeObject(model)}\n";
        //LoggerRepo.LogDebug($"{JsonConvert.SerializeObject(model)}\n");
        //Console.WriteLine($"{JsonConvert.SerializeObject(model)}\n");
        lock (instruments)
        {
            if (instruments.Count == 0)
                return;

            int _i = -1;
            _i = RowsComponents.FindIndex(x => x.Instrument.Id == model.Id);
            if (_i != -1)
            {
                RowsComponents[_i].Update(model);
                StateHasChangedCall();
            }
        }
    }

    public override void Dispose()
    {
        PortfolioEventRepo.UnregisterAction();
        InstrumentEventRepo.UnregisterAction();
        base.Dispose();
    }
}