////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib.Components.StockSharp;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared;

/// <summary>
/// TradesStockSharpViewComponent
/// </summary>
public partial class TradesStockSharpViewComponent : StockSharpBaseComponent
{
    [Inject]
    IManageStockSharpService SsRepo { get; set; } = default!;


    [Inject]
    protected IEventNotifyReceive<MyTradeStockSharpModel> MyTradeEventRepo { get; set; } = default!;


    List<MyTradeStockSharpViewModel>? partData;
    MudTable<MyTradeStockSharpViewModel>? _tableRef;

    /// <summary>
    /// Here we simulate getting the paged, filtered and ordered data from the server, with a token for canceling this request
    /// </summary>
    private async Task<TableData<MyTradeStockSharpViewModel>> ServerReload(TableState state, CancellationToken token)
    {
        TPaginationRequestStandardModel<MyTradeSelectStockSharpRequestModel> req = new()
        {
            PageNum = state.Page,
            PageSize = state.PageSize,
            Payload = new()
            {

            }
        };
        TPaginationResponseModel<MyTradeStockSharpViewModel> res = await SsRepo.TradesSelectAsync(req, token);
        partData = res.Response;
        return new TableData<MyTradeStockSharpViewModel>() { TotalItems = res.TotalRowsCount, Items = res.Response };
    }
    private void MyTradeNotificationHandle(MyTradeStockSharpModel model)
    {
        //if (partData?.Any(x => x.Id == model.Id) == true && _tableRef is not null)
        //{
        //    InvokeAsync(_tableRef.ReloadServerData);
        //    SnackbarRepo.Add($"Order handle: {JsonConvert.SerializeObject(model)}", Severity.Info, c => c.DuplicatesBehavior = SnackbarDuplicatesBehavior.Allow);
        //}
    }

    protected override async Task OnInitializedAsync()
    {
        await SetBusyAsync();
        await MyTradeEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.OwnTradeReceivedStockSharpNotifyReceive, MyTradeNotificationHandle);
        await SetBusyAsync(false);
    }

    public override void Dispose()
    {
        MyTradeEventRepo.UnregisterAction();
        base.Dispose();
    }
}