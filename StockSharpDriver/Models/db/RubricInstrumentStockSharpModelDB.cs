////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SharedLib;

/// <summary>
/// RubricInstrumentStockSharpModelDB
/// </summary>
[Index(nameof(RubricId))]
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