using Ecng.Collections;
using StockSharpDriver;
using SharedLib;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// AnotherExtensions
/// </summary>
public static class AnotherExtensions
{
    /// <inheritdoc/>
    public static decimal CalcFairPriceFromCurve(this SFut sFut, Curve crv)
    {
        sFut.ModelPrice = sFut.ConversionFactors.Keys.Select(s =>
        {
            s.ModelPrice = crv.GetNode(s.UnderlyingSecurity)?.ModelPrice ?? 0;
            return s.GetForwardPrice(s.ModelPrice / 100, sFut.RepoRate, crv.CurveDate, sFut.Deliverydate) / sFut.ConversionFactors.TryGetValue(s);

        }).Min();

        return sFut.ModelPrice;
    }
}