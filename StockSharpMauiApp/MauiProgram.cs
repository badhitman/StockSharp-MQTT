////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using RemoteCallLib;
using SharedLib;

namespace StockSharpMauiApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();

        //        builder.Services.AddDbContextFactory<StockSharpAppContext>(opt =>
        //        {
        //#if DEBUG
        //            opt.EnableSensitiveDataLogging(true);
        //            opt.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        //#endif
        //        });

        StockSharpClientConfigModel _conf = StockSharpClientConfigModel.BuildEmpty();
        builder.Services.AddSingleton(sp => _conf);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        string appName = typeof(MauiProgram).Assembly.GetName().Name ?? "StockSharpMauiAppDemoAssemblyName";
        #region MQ Transmission (remote methods call)
        builder.Services.AddSingleton<IMQTTClient>(x => new MQttClient(x.GetRequiredService<StockSharpClientConfigModel>(), x.GetRequiredService<ILogger<MQttClient>>(), appName));
        // 
        builder.Services
            .AddScoped<IRubricsTransmission, RubricsTransmission>()
            .AddScoped<ITelegramBotStandardTransmission, TelegramBotStandardTransmission>()
            .AddScoped<IDataStockSharpService, DataStockSharpTransmission>()
            .AddScoped<IDriverStockSharpService, StockSharpDriverTransmission>()
            .AddScoped<IManageStockSharpService, ManageStockSharpTransmission>()
            .AddScoped<ILogsService, LogsServiceTransmission>()
            .AddScoped<IParametersStorageTransmission, ParametersStorageTransmission>()
            ;

        builder.Services
                   .AddSingleton<IEventsStockSharpService, StockSharpEventsServiceTransmission>()
               ;
        //
        builder.Services
            .RegisterEventNotify<UpdateConnectionHandleModel>()
            .RegisterEventNotify<PortfolioStockSharpViewModel>()
            .RegisterEventNotify<InstrumentTradeStockSharpViewModel>()
            .RegisterEventNotify<MyTradeStockSharpModel>()
            .RegisterEventNotify<OrderStockSharpViewModel>()
            .RegisterEventNotify<UserTelegramBaseModel>()
            .RegisterEventNotify<ToastShowClientModel>()
            ;
        #endregion
        return builder.Build();
    }
}