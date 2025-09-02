using Ecng.Collections;
using Ecng.Common;
using SharedLib;
using StockSharpDriver;

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

    public static bool EqCountry(this Ecng.Common.CountryCodes? ccL, CountryCodesEnum? ccR)
    {
        if (ccL is null && ccR is null)
            return true;

        if (ccL is null || ccR is null)
            return false;

        return Enum.GetName(typeof(Ecng.Common.CountryCodes), ccL) == Enum.GetName(typeof(CountryCodesEnum), ccR);
    }
    public static bool EqCountry(this CountryCodesEnum? ccL, Ecng.Common.CountryCodes? ccR)
    {
        if (ccL is null && ccR is null)
            return true;

        if (ccL is null || ccR is null)
            return false;

        return Enum.GetName(typeof(Ecng.Common.CountryCodes), ccL) == Enum.GetName(typeof(CountryCodesEnum), ccR);
    }
    public static bool EqCurrencies(this CurrenciesTypesEnum? ccL, CurrencyTypes? ccR)
    {
        if (ccL is null && ccR is null)
            return true;

        if (ccL is null || ccR is null)
            return false;

        return Enum.GetName(typeof(CountryCodes), ccL) == Enum.GetName(typeof(CountryCodesEnum), ccR);
    }

    public static bool EqCurrencies(this CurrencyTypes? ccL, CurrenciesTypesEnum? ccR)
    {
        if (ccL is null && ccR is null)
            return true;

        if (ccL is null || ccR is null)
            return false;

        return Enum.GetName(typeof(CountryCodes), ccL) == Enum.GetName(typeof(CountryCodesEnum), ccR);
    }
}