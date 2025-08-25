////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;

namespace SharedLib;

/// <summary>
/// Markers of instruments
/// </summary>
[Index(nameof(MarkerDescriptor))]
public class InstrumentMarkersModelDB : MarkerInstrumentStockSharpViewModel
{
    /// <inheritdoc/>
    public InstrumentStockSharpModelDB? Instrument { get; set; }

    /// <inheritdoc/>
    public int InstrumentId { get; set; }
}