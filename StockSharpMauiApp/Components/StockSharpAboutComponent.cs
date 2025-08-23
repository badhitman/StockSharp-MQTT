////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib;
using Microsoft.AspNetCore.Components;
using SharedLib;

namespace StockSharpMauiApp.Components;

/// <summary>
/// StockSharpAboutComponent
/// </summary>
public class StockSharpAboutComponent : BlazorBusyComponentBaseModel
{
    /// <inheritdoc/>
    [Inject]
    protected IDriverStockSharpService DriverRepo { get; set; } = default!;

    [Inject]
    IEventNotifyReceive<UpdateConnectionHandleModel> ConnectionEventRepo { get; set; } = default!;


    /// <inheritdoc/>
    protected AboutConnectResponseModel? AboutConnection;

    /// <inheritdoc/>
    public bool EachDisable => AboutConnection is null || AboutConnection.ConnectionState != ConnectionStatesEnum.Connected;


    /// <inheritdoc/>
    protected virtual async Task GetStatusConnection()
    {
        if (!IsBusyProgress)
            await SetBusyAsync();

        if (AboutConnection is null)
            AboutConnection = await DriverRepo.AboutConnection();
        else
            AboutConnection.Update(await DriverRepo.AboutConnection());

        await SetBusyAsync(false);
        SnackBarRepo.ShowMessagesResponse(AboutConnection.Messages);
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await SetBusyAsync();
        await ConnectionEventRepo.RegisterAction(GlobalStaticConstantsTransmission.TransmissionQueues.UpdateConnectionStockSharpNotifyReceive, UpdateConnectionAction);
        await GetStatusConnection();
    }

    private void UpdateConnectionAction(UpdateConnectionHandleModel model)
    {
        if (AboutConnection is null)
            return;

        AboutConnection.CanConnect = model.CanConnect;
        AboutConnection.ConnectionState = model.ConnectionState;
        StateHasChangedCall();
        InvokeAsync(async () => {
            if (AboutConnection is null)
                AboutConnection = await DriverRepo.AboutConnection();
            else
                AboutConnection.Update(await DriverRepo.AboutConnection());

            StateHasChangedCall();
        });
    }

    public override void Dispose()
    {
        ConnectionEventRepo.UnregisterAction();
        base.Dispose();
    }
}