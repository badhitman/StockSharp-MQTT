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
    /// <summary>
    /// Delegate [event] for <c>Connect</c> event
    /// </summary>
    public delegate void ConnectHandler();
    /// <summary>
    /// Delegate [event] for <c>Disconnect</c> event
    /// </summary>
    public delegate void DisconnectHandler();

    /// <inheritdoc/>
    public event ConnectHandler? ConnectNotify;
    /// <inheritdoc/>
    public event DisconnectHandler? DisconnectNotify;

    /// <inheritdoc/>
    public Connector Connector { get; set; } = new();

    /// <summary>
    /// Notify outer service`s to open connection
    /// </summary>
    public void Subscribe()
    {
        if (ConnectNotify is not null)
            ConnectNotify();
    }

    /// <summary>
    /// Notify outer service`s to close connection
    /// </summary>
    public void Unsubscribe()
    {
        if (DisconnectNotify is not null)
            DisconnectNotify();
    }
}