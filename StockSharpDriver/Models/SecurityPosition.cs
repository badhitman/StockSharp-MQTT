using SharedLib;

namespace StockSharpDriver;

/// <summary>
/// SecurityPosition
/// </summary>
public class SecurityPosition(InstrumentTradeStockSharpModel sec, string stname, decimal lowlimit, decimal highlimit, decimal bidvolume, decimal offervolume, decimal offset)
{
    /// <inheritdoc/>
    public InstrumentTradeStockSharpModel Sec { get; set; } = sec;

    /// <inheritdoc/>
    public string StrategyName { get; set; } = stname;

    /// <inheritdoc/>
    public decimal Offset { get; set; } = offset;

    /// <inheritdoc/>
    public decimal LowLimit { get; set; } = lowlimit;

    /// <inheritdoc/>
    public decimal HighLimit { get; set; } = highlimit;

    /// <inheritdoc/>
    public decimal BidVolume { get; set; } = bidvolume;

    /// <inheritdoc/>
    public decimal OfferVolume { get; set; } = offervolume;

    /// <inheritdoc/>
    public decimal Position { get; set; } = 0;
}