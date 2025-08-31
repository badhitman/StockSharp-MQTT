////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using StockSharp.Algo;

namespace StockSharpDriver;

/// <summary>
/// ConnectionLink
/// </summary>
public class ConnectionLink
{
    /// <inheritdoc/>
    public delegate void ConnectHandler();
    /// <inheritdoc/>
    public delegate void DisconnectHandler();

    /// <inheritdoc/>
    public event ConnectHandler? ConnectNotify;
    /// <inheritdoc/>
    public event DisconnectHandler? DisconnectNotify;

    /// <inheritdoc/>
    public Connector Connector { get; set; } = new();

    /// <inheritdoc/>
    public void Subscribe()
    {
        if (ConnectNotify is not null)
            ConnectNotify();
    }

    /// <inheritdoc/>
    public void Unsubscribe()
    {
        if (DisconnectNotify is not null)
            DisconnectNotify();
    }
}