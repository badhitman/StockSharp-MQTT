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
            SQLiteCommand cmd = new("SELECT * FROM TradeCalendar ORDER BY TradeDate ASC")
            {
                Connection = conn
            };
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

        if (bAsk.IsNull() || bBid.IsNull())
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

        return 100;
    }
}