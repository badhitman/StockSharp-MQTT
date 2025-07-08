////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

namespace SharedLib;

/// <summary>
/// CashFlowModelDB
/// </summary>
public class CashFlowModelDB : CashFlowViewModel
{
    public int InstrumentId { get; set; }
    public InstrumentStockSharpModelDB Instrument {  get; set; }
}