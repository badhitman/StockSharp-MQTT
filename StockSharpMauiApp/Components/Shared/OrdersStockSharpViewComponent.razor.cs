////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib.Components.StockSharp;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Newtonsoft.Json;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared;

/// <summary>
/// OrdersStockSharpViewComponent
/// </summary>
public partial class OrdersStockSharpViewComponent : StockSharpBaseComponent
{
    [Inject]
    IManageStockSharpService SsRepo { get; set; } = default!;


    [Inject]
    protected IEventNotifyReceive<OrderStockSharpModel> PortfolioEventRepo { get; set; } = default!;

    List<OrderStockSharpViewModel>? partData;
    MudTable<OrderStockSharpViewModel>? _tableRef;

    /// <summary>
    /// Here we simulate getting the paged, filtered and ordered data from the server, with a token for canceling this request
    /// </summary>
    private async Task<TableData<OrderStockSharpViewModel>> ServerReload(TableState state, CancellationToken token)
    {
        TPaginationRequestStandardModel<OrdersSelectStockSharpRequestModel> req = new()
        {
            PageNum = state.Page,
            PageSize = state.PageSize,
            Payload = new()
            {

            }
        };
        TPaginationResponseModel<OrderStockSharpViewModel> res = await SsRepo.OrdersSelectAsync(req, token);
        partData = res.Response;
        return new TableData<OrderStockSharpViewModel>() { TotalItems = res.TotalRowsCount, Items = res.Response };
    }

    protected override async Task OnInitializedAsync()
    {
        await SetBusyAsync();
        await PortfolioEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.OrderReceivedStockSharpNotifyReceive, OrderNotificationHandle);
        await SetBusyAsync(false);
    }

    private void OrderNotificationHandle(OrderStockSharpModel model)
    {
        if (partData?.Any(x => x.Id == model.Id) == true && _tableRef is not null)
        {
            InvokeAsync(_tableRef.ReloadServerData);
            SnackbarRepo.Add($"Order handle: {JsonConvert.SerializeObject(model)}", Severity.Info, c => c.DuplicatesBehavior = SnackbarDuplicatesBehavior.Allow);
        }
    }
}