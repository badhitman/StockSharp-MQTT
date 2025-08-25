////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using StockSharp.Algo;

namespace StockSharpDriver;

/// <inheritdoc/>
public class ConnectionLink
{
    public delegate void ConnectHandler();
    public delegate void DisconnectHandler();

    public event ConnectHandler? ConnectNotify;
    public event DisconnectHandler? DisconnectNotify;

    public Connector Connector { get; set; } = new();

    public void Subscribe()
    {
        if (ConnectNotify is not null)
            ConnectNotify();
    }

    public void Unsubscribe()
    {
        if (DisconnectNotify is not null)
            DisconnectNotify();
    }
}