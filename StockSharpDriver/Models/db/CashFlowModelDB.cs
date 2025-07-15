////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

namespace SharedLib;

/// <summary>
/// CashFlowModelDB
/// </summary>
public class CashFlowModelDB : CashFlowViewModel
{
    /// <inheritdoc/>
    public int InstrumentId { get; set; }

    /// <inheritdoc/>
    public InstrumentStockSharpModelDB Instrument { get; set; }
}