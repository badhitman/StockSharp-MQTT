using Ecng.Common;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Algo;
using System.Data;
using System.Data.SQLite;

namespace StockSharpDriver;

public static class MyHelper
{
    public static DateTime GetNextWorkingDay(DateTime date, int daynumber, string DbName)
    {
        DateTime dt = new();

        if (daynumber < 0)
        {
            return dt;
        }

        SQLiteConnection conn = new("Data Source=" + DbName + "; Version=3;");

        try
        {
            conn.Open();
        }
        catch (SQLiteException ex)
        {
            Console.WriteLine(ex.Message);
        }

        if (conn.State == ConnectionState.Open)
        {
            SQLiteCommand cmd = new("SELECT * FROM TradeCalendar ORDER BY TradeDate ASC", conn);
            SQLiteDataReader reader = cmd.ExecuteReader();
            reader.Read();

            dt = reader.GetDateTime(reader.GetOrdinal("TradeDate"));

            if (date.Date > dt.Date)
            {
                bool notEnd = true;

                while (notEnd && (date.Date > dt.Date))
                {
                    if (!reader.Read())
                    {
                        notEnd = false;
                        dt = new DateTime();
                    }
                    else
                        dt = reader.GetDateTime(reader.GetOrdinal("TradeDate")).Date;
                }
            }

            for (int i = 0; i < daynumber; i++)
            {
                if (reader.Read())
                    dt = reader.GetDateTime(reader.GetOrdinal("TradeDate")).Date;
                else
                    dt = new DateTime();
            }

            reader.Close();
            reader.DoDispose();
            conn.Close();
        }

        conn.Dispose();
        return dt;
    }

    public static decimal GetBestConditionPrice(Security sec, IOrderBookMessage depth, decimal modelPrice, decimal lowLimit, decimal highLimit, decimal skipVolume)
    {
        QuoteChange? bBid = depth.GetBestBid();
        QuoteChange? bAsk = depth.GetBestAsk();

        if (sec.PriceStep == 0) { sec.PriceStep = 0.001m; }

        if (bAsk is null || bBid is null)
            return 0;

        //If limits are wrong
        if (Math.Abs(highLimit) < Math.Abs(lowLimit))
            return 0;

        //If limits have different sighns
        if (highLimit * lowLimit < 0)
            return 0;

        //zero limit
        if (highLimit == 0)
            return sec.ShrinkPrice(modelPrice);

        if ((highLimit < 0) && (bBid == null))
            return sec.ShrinkPrice(modelPrice + highLimit);

        if ((highLimit > 0) && (bAsk == null))
            return sec.ShrinkPrice(modelPrice + highLimit);

        if (highLimit < 0)
        {
            if ((bAsk.Value.Price < (modelPrice + highLimit)) || (depth.Bids.Sum(item => item.Volume) <= skipVolume))
                return sec.ShrinkPrice(modelPrice + highLimit);

            decimal sum = 0;
            int k = -1;
            while (sum < skipVolume)
            {
                k++;
                sum += depth.Bids[k].Volume;
            }

            if (depth.Bids[k].Price < modelPrice + highLimit)
                return sec.ShrinkPrice(modelPrice + highLimit);

            if (depth.Bids[k].Price > modelPrice + lowLimit)
                return sec.ShrinkPrice(modelPrice + lowLimit);

            if (depth.Bids[k].Price == modelPrice + lowLimit)
                return sec.ShrinkPrice(modelPrice + lowLimit + (sec.PriceStep ?? 0));

            return sec.ShrinkPrice(depth.Bids[k].Price + (sec.PriceStep ?? 0));
        }
        else
        {
            if ((bAsk.Value.Price > modelPrice + highLimit) || (depth.Asks.Sum(item => item.Volume) <= skipVolume))
                return sec.ShrinkPrice(modelPrice + highLimit);

            decimal sum = 0;
            int k = -1;
            while (sum < skipVolume)
            {
                k++;
                sum += depth.Asks[k].Volume;
            }

            if (depth.Asks[k].Price > modelPrice + highLimit)
                return sec.ShrinkPrice(modelPrice + highLimit);

            if (depth.Asks[k].Price < modelPrice + lowLimit)
                return sec.ShrinkPrice(modelPrice + lowLimit);

            if (depth.Asks[k].Price == modelPrice + lowLimit)
                return sec.ShrinkPrice(modelPrice + lowLimit - (sec.PriceStep ?? 0));

            return sec.ShrinkPrice(depth.Asks[k].Price - (sec.PriceStep ?? 0));
        }
    }
}