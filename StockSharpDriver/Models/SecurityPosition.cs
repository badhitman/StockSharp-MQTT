using StockSharp.BusinessEntities;

namespace StockSharpDriver;

public class SecurityPosition(Security sec, string stname, decimal lowlimit, decimal highlimit, decimal bidvolume, decimal offervolume, decimal offset)
{
    public Security Sec = sec;
    public string StrategyName = stname;
    public decimal Offset = offset;
    public decimal LowLimit = lowlimit;
    public decimal HighLimit = highlimit;
    public decimal BidVolume = bidvolume;
    public decimal OfferVolume = offervolume;
    public decimal Position = 0;
}