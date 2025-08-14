using Ecng.Common;
using SharedLib;
using StockSharp.Algo;
using StockSharp.BusinessEntities;
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

    /// <summary>
    /// Load Curve form database
    /// </summary>
    public string GetCurveFromDb(string DbName, Connector trader, BoardStockSharpModel board, List<string> bigPriceDifferences, ref IEventsStockSharp eventTrans)
    {
        string secName;
        decimal secPrice;
        Security security;
        int j, tableSize;

        BondList.Clear();
        Length = 0;

        ToastShowClientModel reqToast;
        SQLiteConnection conn = new("Data Source=" + DbName + "; Version=3;");
        try
        {
            conn.Open();
        }
        catch (Exception ex)
        {
            reqToast = new()
            {
                HeadTitle = $"SQLite connection exception ({nameof(GetCurveFromDb)}): {ex.GetType().Name}]",
                TypeMessage = MessagesTypesEnum.Warning,
                MessageText = ex.Message,
            };
            eventTrans.ToastClientShow(reqToast);
            return ex.Message;
        }
        if (conn.State == ConnectionState.Open)
        {
            SQLiteCommand cmd = new("SELECT * FROM BondPrices ORDER BY rowid DESC", conn);
            SQLiteDataReader reader = cmd.ExecuteReader();
            reader.Read();

            tableSize = reader.FieldCount;
            DateTime dt = reader.GetDateTime(reader.GetOrdinal("DTime"));
            dt = MyHelper.GetNextWorkingDay(dt, 1, DbName);

            bool checkBoard(ExchangeBoard reqEx)
            {
                return reqEx.Code == board.Code && reqEx.Exchange.Name == board.Exchange.Name && (int?)reqEx.Exchange.CountryCode == board.Exchange.CountryCode;
            }

            if (CurveDate.Day != dt.Day)
            {
                reqToast = new() 
                { 
                    HeadTitle = $"{nameof(GetCurveFromDb)}: [CurveDate.Day]!=[{dt.Day}]", 
                    TypeMessage = MessagesTypesEnum.Warning, 
                    MessageText = $"Wrong Date! Pls update the curve!" 
                };
                eventTrans.ToastClientShow(reqToast);
                BondList.Clear();
                Length = 0;
            }
            else
            {
                for (int i = 0; i <= tableSize - 1; i++)
                {
                    secName = reader.GetName(i);
                    security = trader.Securities.FirstOrDefault(s => (s.Code == secName) && checkBoard(s.Board));
                    if ((security is not null))
                    {
                        secPrice = Convert.ToDecimal(reader.GetValue(i));
                        AddNode(new SBond(security), secPrice);
                    }
                }

                reader.Read();  //previous curve data

                j = 0;

                while (j <= tableSize - 1)
                {
                    secName = reader.GetName(j);
                    security = trader.Securities.FirstOrDefault(s => (s.Code == secName) && checkBoard(s.Board));
                    if (security is not null)
                    {
                        secPrice = Convert.ToDecimal(reader.GetValue(j));

                        if (Math.Abs(GetNode(security).ModelPrice - secPrice) >= 0.2m)
                        {
                            if (!bigPriceDifferences.Contains(secName))
                            {
                                BondList.Clear();
                                Length = 0;

                                reqToast = new()
                                {
                                    HeadTitle = "Curve loading request action!",
                                    TypeMessage = MessagesTypesEnum.Warning,
                                    MessageText = $"Sign big price difference please (instrument `{secName}`)"
                                };

                                eventTrans.ToastClientShow(reqToast);

                                reader.Close();
                                reader.DoDispose();
                                conn.Close();

                                conn.Dispose();
                                return secName;
                            }
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
        return null;
    }

    public void AddNode(SBond bondSec, decimal price)
    {
        SBond tmpBond = BondList.Find(s => s.UnderlyingSecurity == bondSec.UnderlyingSecurity);

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