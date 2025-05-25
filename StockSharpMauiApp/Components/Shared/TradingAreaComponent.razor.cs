////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib.Components.StockSharp;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using RemoteCallLib;
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
    protected IEventNotifyReceive<PortfolioStockSharpViewModel> PortfolioEventRepo { get; set; } = default!;


    int QuoteVolume { get; set; }
    int QuoteSizeVolume { get; set; }
    int SkipSizeVolume { get; set; }

    List<InstrumentTradeStockSharpViewModel>? instruments;
    List<PortfolioStockSharpViewModel>? portfolios;

    List<BoardStockSharpModel>? allBoards;
    IEnumerable<BoardStockSharpModel>? SelectedBoards { get; set; }

    PortfolioStockSharpModel? SelectedPortfolio { get; set; }

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

    async Task Connect()
    {
        ConnectRequestModel req = new()
        {
            BoardsFilter = SelectedBoards is null ? null : [.. SelectedBoards],
            Instruments = instruments,
            Portfolio = SelectedPortfolio
        };
        await Connect(req);
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await PortfolioEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.PortfolioReceivedStockSharpNotifyReceive, PortfolioNotificationHandle);

        await SetBusyAsync();
        await Task.WhenAll([
            Task.Run(async () => {
                InstrumentsRequestModel req = new()
                    {
                        PageNum = 0,
                        PageSize = int.MaxValue,
                        FavoriteFilter = true,
                    };
                TPaginationResponseModel<InstrumentTradeStockSharpViewModel> res = await DataRepo.InstrumentsSelectAsync(req);
                instruments = res.Response;
            }),
            Task.Run(async () => {
                TResponseModel<List<BoardStockSharpModel>> res = await DataRepo.GetBoardsAsync();
                allBoards = res.Response;
            }),
            Task.Run(async () => {
                TResponseModel<List<PortfolioStockSharpViewModel>> res = await DataRepo.GetPortfoliosAsync();
                portfolios = res.Response;
            })]);

        await SetBusyAsync(false);
    }

    private void PortfolioNotificationHandle(PortfolioStockSharpViewModel model)
    {
        SnackbarRepo.Add(JsonConvert.SerializeObject(model));
    }
}