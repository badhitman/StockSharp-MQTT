////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using System.ComponentModel.DataAnnotations;

namespace SharedLib;

/// <summary>
/// MyTradeStockSharpModelDB
/// </summary>
public class MyTradeStockSharpModelDB : MyTradeStockSharpModel, IBaseStockSharpModel
{
    /// <summary>
    /// Идентификатор/Key
    /// </summary>
    [Key]
    public int Id { get; set; }


    /// <inheritdoc/>
    public DateTime LastUpdatedAtUTC { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAtUTC { get; set; }

    /// <inheritdoc/>
    public new OrderStockSharpModelDB Order { get; set; }

    /// <inheritdoc/>
    public int OrderId { get; set; }
}
