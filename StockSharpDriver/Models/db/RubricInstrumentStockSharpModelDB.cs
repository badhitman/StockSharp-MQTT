////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using System.ComponentModel.DataAnnotations;

namespace SharedLib;

/// <summary>
/// RubricInstrumentStockSharpModelDB
/// </summary>
public class RubricInstrumentStockSharpModelDB
{
    /// <inheritdoc/>
    [Key]
    public int Id { get; set; }

    /// <inheritdoc/>
    public InstrumentStockSharpModelDB Instrument { get; set; }

    /// <inheritdoc/>
    public int InstrumentId { get; set; }

    /// <summary>
    /// <see cref="RubricModelDB"/>
    /// </summary>
    public int RubricId { get; set; }
}