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