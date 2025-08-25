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
    public InstrumentStockSharpModelDB? Instrument { get; set; }
}