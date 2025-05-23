using Ecng.Common;
using SharedLib;
using StockSharp.BusinessEntities;
using System.Data;

namespace StockSharpDriver;

/// <summary>
/// Класс для работы с облигациями
/// </summary>
public class SBond
{
    private BndTypeEnum _sBondType; //  Type -  Govt, Corp, etc..
    private decimal _modelPrice; // theoretical price from the curve
    private Security _underlyingSecurity; // security from which bond is derived
    private string _issuer;
    private string _name;
    private DateTime _maturity;
    private DateTime _issuedate;
    private string _ISIN;
    private string _micexCode;

    public List<CashFlow> CashFlows;

    public decimal ModelPrice
    {
        get { return _modelPrice; }
        set { _modelPrice = value; }
    }

    public Security UnderlyingSecurity
    {
        get { return _underlyingSecurity; }
        set { _underlyingSecurity = value; }
    }

    public DateTime IssueDate
    {
        get { return _issuedate; }
        set { _issuedate = value; }
    }

    public DateTime Maturity
    {
        get { return _maturity; }
        set { _maturity = value; }
    }

    public string MicexCode
    {
        get { return _micexCode; }
        set { _micexCode = value; }
    }

    public SBond()
    {
        _sBondType = BndTypeEnum.Govt;
        _modelPrice = 0;
        _underlyingSecurity = new Security();
        _issuer = "";
        CashFlows = new List<CashFlow>();
        _ISIN = "";
        _micexCode = "";
    }

    public SBond(Security sec) : this()
    {
        _underlyingSecurity = sec;
        _micexCode = sec.Code;
    }

    public void CreateRegularOFZ(string name, string isin, DateTime issuedate, DateTime maturity, decimal rate)
    {
        _name = name;
        _ISIN = isin;
        _issuedate = issuedate;
        _maturity = maturity;

        DateTime dt = issuedate;

        while (dt < maturity)
        {
            CashFlows.Add(new CashFlow(dt, dt + TimeSpan.FromDays(182), Math.Round(1000 * rate * 182 / 365, 2), 0, rate));
            dt += TimeSpan.FromDays(182);
        }
        CashFlows.First(s => s.EndDate.Equals(maturity)).Notional = 1000;
        CashFlows.Sort();
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

        return Math.Round(CashFlows.SkipWhile(s => (s.EndDate <= date))
                                      .Take(1)
                                      .Select(s => s.CouponRate * GetRemainingNotional(date) * (date - s.StDate).Days / 365)
                                      .Sum(), 2);
    }

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
    public decimal GetFisrtDerivByYield(DateTime date, decimal yield, decimal remainingNotional)
    {
        return -(decimal)CashFlows.SkipWhile(s => s.EndDate <= date)
                              .Sum(s =>
                              {
                                  decimal t = (s.EndDate - date).Days / 365m;

                                  return (double)((s.CouponRate * remainingNotional * (s.EndDate - s.StDate).Days / 365m + s.Notional) * t) / Math.Pow((double)(1 + yield), (double)(t + 1));
                              }) / remainingNotional;

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
            nextyield = yield - (GetPriceFromYield(date, yield, true) - price) / GetFisrtDerivByYield(date, yield, remainingNotional);
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

        return ((price * remainingNotional + GetAccruedInterest(date)) * (1 + rate * (fwddate - date).Days / 365) - interimCF - GetAccruedInterest(fwddate)) / remainingNotional;
    }
}