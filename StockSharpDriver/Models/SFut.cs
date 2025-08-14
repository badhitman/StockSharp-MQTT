using Ecng.Collections;
using SharedLib;
using StockSharp.BusinessEntities;

namespace StockSharpDriver;

public class SFut(string name, InstrumentTradeStockSharpModel sec, Dictionary<SBond, decimal> convfactors, decimal reporate, DateTime deliverydate)
{
    public decimal ModelPrice { get; set; }

    public InstrumentTradeStockSharpModel UnderlyingSecurity { get; set; } = sec;

    public string Name { get; set; } = name;

    public DateTime Deliverydate { get; set; } = deliverydate;

    public decimal RepoRate { get; set; } = reporate;

    public string MicexCode { get; set; } = sec.Code;

    readonly Dictionary<SBond, decimal> ConversionFactors = convfactors;

    public decimal CalcFairPriceFromCurve(Curve crv)
    {
        ModelPrice = ConversionFactors.Keys.Select(s =>
        {
            s.ModelPrice = crv.GetNode(s.UnderlyingSecurity).ModelPrice;
            return s.GetForwardPrice(s.ModelPrice / 100, RepoRate, crv.CurveDate, Deliverydate) / ConversionFactors.TryGetValue(s);

        }).Min();

        return ModelPrice;
    }
}