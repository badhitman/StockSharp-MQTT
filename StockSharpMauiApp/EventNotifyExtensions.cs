////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using RemoteCallLib;
using SharedLib;

namespace StockSharpMauiApp;

/// <summary>
/// EventNotifyExtensions
/// </summary>
public static class EventNotifyExtensions
{
    /// <inheritdoc/>
    public static IServiceCollection RegisterEventNotify<T>(this IServiceCollection services)
    {
        services.AddTransient<IEventNotifyReceive<T>, EventNotifyReceive<T>>();
        return services;
    }
}

/// <summary>
/// связь моделей StockSharp с локальными моделями
/// </summary>
public static class BindsStockSharpModelsExtensions
{
    public static DashboardTradeStockSharpModel Bind(this DashboardTradeStockSharpModel main, InstrumentTradeStockSharpViewModel inc)
    {
        main.Id = inc.Id;
        main.Board = inc.Board;
        main.Name = inc.Name;

        main.IdRemote = inc.IdRemote;
        main.Code = inc.Code;
        main.ShortName = inc.ShortName;
        main.TypeInstrument = inc.TypeInstrument;
        main.UnderlyingSecurityType = inc.UnderlyingSecurityType;
        main.Currency = inc.Currency;
        main.Class = inc.Class;
        main.PriceStep = inc.PriceStep;
        main.VolumeStep = inc.VolumeStep;
        main.MinVolume = inc.MinVolume;
        main.MaxVolume = inc.MaxVolume;
        main.Multiplier = inc.Multiplier;
        main.Decimals = inc.Decimals;
        main.ExpiryDate = inc.ExpiryDate;
        main.SettlementDate = inc.SettlementDate;
        main.CfiCode = inc.CfiCode;
        main.FaceValue = inc.FaceValue;
        main.SettlementType = inc.SettlementType;
        main.OptionStyle = inc.OptionStyle;
        main.PrimaryId = inc.PrimaryId;
        main.UnderlyingSecurityId = inc.UnderlyingSecurityId;
        main.OptionType = inc.OptionType;
        main.Shortable = inc.Shortable;

        return main;
    }
}