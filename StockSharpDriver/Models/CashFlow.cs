namespace StockSharpDriver;

public class CashFlow(DateTime stdate, DateTime enddate, decimal coupon, decimal notional, decimal couprate) : IComparable<CashFlow>
{
    public DateTime StDate = stdate;
    public DateTime EndDate = enddate;
    public decimal CouponRate = couprate;
    public decimal Coupon = coupon;
    public decimal Notional = notional;

    public int CompareTo(CashFlow other)
    {
        return EndDate.CompareTo(other.EndDate);
    }
}