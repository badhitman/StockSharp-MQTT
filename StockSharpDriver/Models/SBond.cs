using StockSharp.BusinessEntities;
using System.Data;
using Ecng.Common;

namespace StockSharpDriver;

public class SBond(Security sec)
{
    private DateTime _maturity;
    public List<CashFlow> CashFlows = [];

    decimal _modelPrice; // theoretical price from the curve
    public decimal ModelPrice
    {
        get { return _modelPrice; }
        set { _modelPrice = value; }
    }

    Security _underlyingSecurity = sec; // security from which bond is derived
    public Security UnderlyingSecurity
    {
        get { return _underlyingSecurity; }
        set { _underlyingSecurity = value; }
    }

    DateTime _issueDate;
    public DateTime IssueDate
    {
        get { return _issueDate; }
        set { _issueDate = value; }
    }

    public DateTime Maturity
    {
        get { return _maturity; }
        set { _maturity = value; }
    }

    private string _micexCode = sec.Code;
    public string MicexCode
    {
        get { return _micexCode; }
        set { _micexCode = value; }
    }

    //Returns remanining notional for specific date
    public decimal GetRemainingNotional(DateTime date)
    {
        if ((date < IssueDate) || (date >= Maturity))
            return -1;
        return CashFlows.Where(s => s.EndDate > date).Select(s => s.Notional).Sum();
    }

    //Calculates Accrued Interest for bond for scpecific day. If day is out of range retuirns -1
    public decimal GetAccruedInterest(DateTime date)
    {
        if ((date < IssueDate) || (date >= Maturity))
            return -1;

        return Math.Round(CashFlows.SkipWhile(s => s.EndDate <= date)
                                      .Take(1)
                                      .Sum(s => s.CouponRate * GetRemainingNotional(date) * (date - s.StDate).Days / 365), 2);
    }

    //Calculte price for given Yield
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

    //Calculation of first derivate by yield
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

    //Yield calculation for bond
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
            nextyield = yield -
                        (GetPriceFromYield(date, yield, true) - price) /
                        GetFirstDerivByYield(date, yield, remainingNotional);
            error = nextyield - yield;
            yield = nextyield;
            nIter++;

        } while ((Math.Abs(error) > precision) && (nIter <= maxIter));

        return nextyield;
    }

    //Calculate foerward price of the bond
    public decimal GetForwardPrice(decimal price, decimal rate, DateTime date, DateTime fwddate)
    {
        if ((date > fwddate) || (date < IssueDate) || (date >= Maturity) || (fwddate < IssueDate) || (fwddate >= Maturity))
            return -1;

        decimal interimCF = CashFlows.SkipWhile(s => (s.EndDate <= date))
                             .TakeWhile(s => (s.EndDate <= fwddate))
                             .Select(s => (s.Coupon + s.Notional) * (1 + rate * (fwddate - s.EndDate).Days / 365))
                             .Sum();

        decimal remainingNotional = GetRemainingNotional(date);

        return ((price * remainingNotional + GetAccruedInterest(date)) * (1 + rate * (fwddate - date).Days / 365) -
                interimCF - GetAccruedInterest(fwddate)) / remainingNotional;

    }
}