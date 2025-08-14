using System.Data;
using Ecng.Common;
using SharedLib;

namespace StockSharpDriver;

public class SBond(InstrumentTradeStockSharpModel sec)
{
    public List<CashFlow> CashFlows = [];

    public decimal ModelPrice { get; set; }

    public InstrumentTradeStockSharpModel UnderlyingSecurity { get; set; } = sec;

    public DateTime IssueDate { get; set; }

    public DateTime Maturity { get; set; }

    public string MicexCode { get; set; } = sec.Code;

    //Returns remaining notional for specific date
    public decimal GetRemainingNotional(DateTime date)
    {
        if ((date < IssueDate) || (date >= Maturity))
            return -1;
        return CashFlows.Where(s => s.EndDate > date).Select(s => s.Notional).Sum();
    }

    /// <summary>
    /// Calculates Accrued Interest for bond for specific day. If day is out of range returns -1
    /// </summary>
    public decimal GetAccruedInterest(DateTime date)
    {
        if ((date < IssueDate) || (date >= Maturity))
            return -1;

        return Math.Round(CashFlows.SkipWhile(s => s.EndDate <= date)
                                      .Take(1)
                                      .Sum(s => s.CouponRate * GetRemainingNotional(date) * (date - s.StDate).Days / 365), 2);
    }

    /// <summary>
    /// Calculate price for given Yield
    /// </summary>
    public decimal GetPriceFromYield(DateTime date, decimal yield, bool Clean)
    {
        if ((date < IssueDate) || (date >= Maturity))
            return -1;
        if (CashFlows.IsNull() || (CashFlows.Count == 0))
            return -1;

        decimal remainingNotional = GetRemainingNotional(date);

        double DirtyPrice = CashFlows.SkipWhile(s => (s.EndDate <= date))
                                  .Select(s =>

                                            (double)(s.CouponRate * remainingNotional * (s.EndDate - s.StDate).Days / 365 + s.Notional) /
                                             Math.Pow((double)(1 + yield), (double)(s.EndDate - date).Days / 365)
                                   )
                                  .Sum();
        if (Clean)
            return ((decimal)DirtyPrice - GetAccruedInterest(date)) / remainingNotional;

        return (decimal)DirtyPrice / remainingNotional;
    }

    /// <summary>
    /// Calculation of first derivate by yield
    /// </summary>
    public decimal GetFirstDerivByYield(DateTime date, decimal yield, decimal remainingNotional)
    {
        return -(decimal)CashFlows.SkipWhile(s => (s.EndDate <= date))
                              .Select(s =>
                              {
                                  decimal t = (s.EndDate - date).Days / 365m;

                                  return (double)((s.CouponRate * remainingNotional * (s.EndDate - s.StDate).Days / 365m + s.Notional) * t)
                                         / Math.Pow((double)(1 + yield), (double)(t + 1));
                              }
                              ).Sum() / remainingNotional;

    }

    /// <summary>
    /// Yield calculation for bond
    /// </summary>
    public decimal GetYieldForPrice(DateTime date, decimal price)
    {
        if ((date < IssueDate) || (date >= Maturity) || (price < 0))
            return -1;
        if (CashFlows.IsNull() || (CashFlows.Count == 0))
            return -1;

        decimal remainingNotional = GetRemainingNotional(date);

        const decimal seed = 0.05m;
        const decimal precision = 0.000000001m;
        const int maxIter = 1000;
        int nIter = 0;
        decimal nextyield;
        decimal error;

        decimal yield = seed;

        do
        {
            nextyield = yield - (GetPriceFromYield(date, yield, true) - price) / GetFirstDerivByYield(date, yield, remainingNotional);
            error = nextyield - yield;
            yield = nextyield;
            nIter++;

        } while ((Math.Abs(error) > precision) && (nIter <= maxIter));

        return nextyield;
    }

    /// <summary>
    /// Calculate forward price of the bond
    /// </summary>
    public decimal GetForwardPrice(decimal price, decimal rate, DateTime date, DateTime fwddate)
    {
        if ((date > fwddate) || (date < IssueDate) || (date >= Maturity) || (fwddate < IssueDate) || (fwddate >= Maturity))
            return -1;

        decimal interimCF = CashFlows.SkipWhile(s => (s.EndDate <= date))
                             .TakeWhile(s => s.EndDate <= fwddate)
                             .Select(s => (s.Coupon + s.Notional) * (1 + rate * (fwddate - s.EndDate).Days / 365))
                             .Sum();

        decimal remainingNotional = GetRemainingNotional(date);

        return ((price * remainingNotional + GetAccruedInterest(date)) * (1 + rate * (fwddate - date).Days / 365) - interimCF - GetAccruedInterest(fwddate)) / remainingNotional;
    }
}