////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using StockSharp.Algo;
using SharedLib;

namespace StockSharpDriver;

/// <summary>
/// StockSharpDriverService
/// </summary>
public class StockSharpDriverService(Connector connector) : IStockSharpDriverService
{
    /// <inheritdoc/>
    public Task<ResponseBaseModel> PingAsync(CancellationToken cancellationToken = default)
    {
        //StockSharp.Algo.Connector Connector = new();
        return Task.FromResult(ResponseBaseModel.CreateSuccess($"Ok - {nameof(StockSharpDriverService)}"));
    }
}