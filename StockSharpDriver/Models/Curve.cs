using Ecng.Common;
using StockSharp.BusinessEntities;
using StockSharp.Algo;
using System.Data;
using System.Data.SQLite;

namespace StockSharpDriver;

/// <summary>
/// Класс для работы с кривой
/// </summary>
public class Curve(DateTime date)
{
    private int _length = 0;
    private DateTime _curveDate = date;

    public List<SBond> BondList { get; private set; } = [];

    public int Length
    {
        get { return _length; }
        private set { _length = value; }
    }

    public DateTime CurveDate
    {
        get { return _curveDate; }
        set { _curveDate = value; }
    }

    //Load Curve form database
    public void GetCurveFromDb(string DbName, Connector trader)
    {
        string secName;
        decimal secPrice;
        Security security;
        int j, tableSize;

        SQLiteConnection conn = new("Data Source=" + DbName + "; Version=3;");
        try
        {
            conn.Open();
        }
        catch (SQLiteException ex)
        {
            BondList = null;
            Length = 0;
            Console.WriteLine(ex.Message);
        }

        if (conn.State == ConnectionState.Open)
        {
            SQLiteCommand cmd = new("SELECT * FROM BondPrices ORDER BY rowid DESC")
            {
                Connection = conn
            };
            SQLiteDataReader reader = cmd.ExecuteReader();
            reader.Read();


            tableSize = reader.FieldCount;
            var dt = reader.GetDateTime(reader.GetOrdinal("DTime"));
            dt = MyHelper.GetNextWorkingDay(dt, 1, DbName);

            if (CurveDate.Day != dt.Day)
            {
                BondList.Clear();
                Length = 0;
                throw new Exception("Wrong Date! Pls update the curve!");
            }
            else
            {
                for (int i = 0; i <= tableSize - 1; i++)
                {
                    secName = reader.GetName(i);
                    security = trader.Securities.FirstOrDefault(s => (s.Code == secName) && (s.Board == ExchangeBoard.MicexTqob));
                    if ((!security.IsNull()) && (secName != "SU26217RMFS8"))
                    {
                        secPrice = Convert.ToDecimal(reader.GetValue(i));
                        this.AddNode(new SBond(security), secPrice);
                    }
                }

                reader.Read();  //previous curve data

                j = 0;

                while (j <= tableSize - 1)
                {
                    secName = reader.GetName(j);
                    security = trader.Securities.FirstOrDefault(s => (s.Code == secName) && (s.Board == ExchangeBoard.MicexTqob));
                    if ((!security.IsNull()) && (secName != "SU26217RMFS8"))
                    {
                        secPrice = Convert.ToDecimal(reader.GetValue(j));

                        if (Math.Abs(GetNode(security).ModelPrice - secPrice) >= 0.2m)
                        {
                            throw new Exception("Big price difference");
                            //if (MessageBox.Show(secName + " Do you want to continue?", "Big price difference in", MessageBoxButton.YesNo) == MessageBoxResult.No)
                            //{
                            //    BondList.Clear();
                            //    Length = 0;
                            //    MessageBox.Show("Curve loading terminated!");
                            //    break;
                            //}
                        }
                    }
                    j++;
                }
            }
            reader.Close();
            reader.DoDispose();
            conn.Close();
        }
        conn.Dispose();
    }

    public void AddNode(SBond bondSec, decimal price)
    {
        var tmpBond = BondList.Find(s => s.UnderlyingSecurity == bondSec.UnderlyingSecurity);

        if (tmpBond != null)
            tmpBond.ModelPrice = price;
        else
        {
            tmpBond = bondSec;
            tmpBond.ModelPrice = price;
            BondList.Add(tmpBond);
            _length++;
        }
    }

    //Returns node which corresponds to security
    public SBond GetNode(Security sec)
    {
        return BondList.Find(s => s.UnderlyingSecurity.Code == sec.Code);
    }
}