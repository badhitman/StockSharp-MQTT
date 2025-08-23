﻿////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib;
using SharedLib;

namespace StockSharpMauiApp.Components;

/// <summary>
/// StockSharpBaseComponent
/// </summary>
public class StockSharpBaseComponent : StockSharpAboutComponent
{
    /// <inheritdoc/>
    protected async Task Connect(ConnectRequestModel req)
    {
        await SetBusyAsync();
        ResponseBaseModel _con = await DriverRepo.Connect(req);
        SnackBarRepo.ShowMessagesResponse(_con.Messages);
        await GetStatusConnection();
    }

    /// <inheritdoc/>
    protected async Task Disconnect()
    {
        await SetBusyAsync();
        ResponseBaseModel _con = await DriverRepo.Disconnect();
        SnackBarRepo.ShowMessagesResponse(_con.Messages);
        //_con = await DriverRepo.Terminate();
        //SnackbarRepo.ShowMessagesResponse(_con.Messages);
        await GetStatusConnection();
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }
}