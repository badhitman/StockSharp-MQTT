using SharedLib;

namespace StockSharpDriver;

public class SecurityPosition(InstrumentTradeStockSharpModel sec, string stname, decimal lowlimit, decimal highlimit, decimal bidvolume, decimal offervolume, decimal offset)
{
    public InstrumentTradeStockSharpModel Sec = sec;
    public string StrategyName = stname;
    public decimal Offset = offset;
    public decimal LowLimit = lowlimit;
    public decimal HighLimit = highlimit;
    public decimal BidVolume = bidvolume;
    public decimal OfferVolume = offervolume;
    public decimal Position = 0;
}