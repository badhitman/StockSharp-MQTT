////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using System.ComponentModel.DataAnnotations;

namespace SharedLib;

/// <summary>
/// MyTradeStockSharpModelDB
/// </summary>
public class MyTradeStockSharpModelDB : MyTradeStockSharpViewModel, IBaseStockSharpModel
{
    /// <inheritdoc/>
    public new OrderStockSharpModelDB Order { get; set; }

    /// <inheritdoc/>
    public int OrderId { get; set; }
}