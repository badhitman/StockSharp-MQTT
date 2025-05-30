////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using System.ComponentModel.DataAnnotations;

namespace SharedLib;

/// <summary>
/// Markers of instruments
/// </summary>
public class InstrumentMarkersModelDB
{
    [Key]
    public int Id { get; set; }

    /// <inheritdoc/>
    public InstrumentStockSharpMarkersEnum MarkerDescriptor { get; set; }

    /// <inheritdoc/>
    public InstrumentStockSharpModelDB Instrument {  get; set; }

    /// <inheritdoc/>
    public int InstrumentId { get; set; }
}