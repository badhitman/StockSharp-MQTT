////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.AspNetCore.Components;
using BlazorLib;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared;

public partial class TestComponent : BlazorBusyComponentBaseModel
{
    [Inject]
    IDataStockSharpService SsMainRepo { get; set; } = default!;

    [Inject]
    IDriverStockSharpService SsDrvRepo { get; set; } = default!;


    List<BoardStockSharpViewModel>? myBoards;
    BoardStockSharpModel? SelectedBoard { get; set; }

    OrderTypesEnum orderTypeCreate = OrderTypesEnum.Market;
    SidesEnum orderSideCreate = SidesEnum.Buy;

    List<PortfolioStockSharpViewModel>? myPortfolios;
    PortfolioStockSharpModel? SelectedPortfolio { get; set; }
    decimal? PriceNewOrder { get; set; }
    decimal? VolumeNewOrder { get; set; }

    List<InstrumentTradeStockSharpViewModel>? myInstruments;

    InstrumentTradeStockSharpViewModel? SelectedInstrument { get; set; }

    bool disposedValue;

    async Task NewOrder()
    {
        if (SelectedPortfolio is null)
        {
            SnackbarRepo.Error("Не выбран портфель");
            return;
        }

        if (SelectedInstrument is null)
        {
            SnackbarRepo.Error("Не выбран инструмент");
            return;
        }

        if (PriceNewOrder is null || PriceNewOrder <= 0)
        {
            SnackbarRepo.Error("Не указана стоимость");
            return;
        }

        if (VolumeNewOrder is null || VolumeNewOrder <= 0)
        {
            SnackbarRepo.Error("Не указан объём");
            return;
        }

        await SetBusyAsync();
        CreateOrderRequestModel req = new()
        {
            Instrument = SelectedInstrument,
            OrderType = orderTypeCreate,
            Portfolio = SelectedPortfolio,
            Price = PriceNewOrder.Value,
            Side = orderSideCreate,
            Volume = VolumeNewOrder.Value,
        };
        ResponseBaseModel res = await SsDrvRepo.OrderRegisterAsync(req);
        SnackbarRepo.ShowMessagesResponse(res.Messages);
        await SetBusyAsync(false);
    }

    protected override async Task OnInitializedAsync()
    {
        await SetBusyAsync();

        await Task.WhenAll([
                Task.Run(async () => {
                    TResponseModel<List<PortfolioStockSharpViewModel>> resPortfolios = await SsMainRepo.GetPortfoliosAsync();
                    SnackbarRepo.ShowMessagesResponse(resPortfolios.Messages);
                    myPortfolios = resPortfolios.Response;
                }),
                Task.Run(async () => {
                    TResponseModel<List<BoardStockSharpViewModel>> resBoards = await SsMainRepo.GetBoardsAsync();
                    SnackbarRepo.ShowMessagesResponse(resBoards.Messages);
                    myBoards = resBoards.Response;
                }),
                Task.Run(async () => {
                    TPaginationResponseModel<InstrumentTradeStockSharpViewModel> resInstruments = await SsMainRepo.InstrumentsSelectAsync(new() { PageSize = 100, FavoriteFilter = true });
                    myInstruments = resInstruments.Response;
                }),
            ]);

        await SetBusyAsync(false);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: освободить управляемое состояние (управляемые объекты)
            }

            // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
            // TODO: установить значение NULL для больших полей
            disposedValue = true;
        }
    }

    // // TODO: переопределить метод завершения, только если "Dispose(bool disposing)" содержит код для освобождения неуправляемых ресурсов
    // ~TestComponent()
    // {
    //     // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
    //     Dispose(disposing: false);
    // }

    public override void Dispose()
    {
        // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
        Dispose(disposing: true);
        base.Dispose();
    }
}