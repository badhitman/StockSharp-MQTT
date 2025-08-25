using StockSharp.BusinessEntities;
using System.Data.SQLite;
using StockSharp.Algo;
using System.Data;
using Ecng.Common;
using SharedLib;

namespace StockSharpDriver;

/// <summary>
/// Класс для работы с кривой
/// </summary>
public class CurveModel : CurveBaseModel
{
    public CurveModel(DateTime date)
    {
        CurveDate = date;
    }

    /// <summary>
    /// Load Curve form database
    /// </summary>
    public string? GetCurveFromDb(string DbName, Connector trader, List<BoardStockSharpViewModel> boards, List<string>? bigPriceDifferences, ref IEventsStockSharp eventTrans)
    {
        string secName;
        decimal secPrice;
        Security? security;
        int j, tableSize;

        BondList.Clear();

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
                return boards.Any(board =>
                {
                    if (reqEx.Code != board.Code)
                        return false;

                    ExchangeStockSharpModel? _ge = board.GetExchange();
                    if (reqEx.Exchange is null && _ge is null)
                        return true;
                    if (reqEx.Exchange is null || _ge is null)
                        return false;

                    return
                        reqEx.Exchange.Name == _ge.Name &&
                        (int?)reqEx.Exchange.CountryCode == _ge.CountryCode;
                });
            }

            if (CurveDate.Day != dt.Day)
            {
                reqToast = new()
                {
                    HeadTitle = $"Wrong Date!",
                    TypeMessage = MessagesTypesEnum.Error,
                    MessageText = $"Pls update the curve! {nameof(GetCurveFromDb)}: [CurveDate.Day]!=[{dt.Day}]"
                };
                eventTrans.ToastClientShow(reqToast);
                BondList.Clear();
            }
            else
            {
                for (int i = 0; i <= tableSize - 1; i++)
                {
                    secName = reader.GetName(i);
                    security = trader.Securities.FirstOrDefault(s => (s.Code == secName) && checkBoard(s.Board));
                    if (security is not null)
                    {
                        secPrice = Convert.ToDecimal(reader.GetValue(i));
                        AddNode(new SBond(new InstrumentTradeStockSharpModel().Bind(security)), secPrice);
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
                        SBond? _gn = GetNode(new InstrumentTradeStockSharpModel().Bind(security));
                        if (_gn is not null && Math.Abs(_gn.ModelPrice - secPrice) >= 0.2m)
                        {
                            if (bigPriceDifferences?.Contains(secName) != true)
                            {
                                BondList.Clear();

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
        SBond? tmpBond = BondList.Find(s => s.UnderlyingSecurity == bondSec.UnderlyingSecurity);

        if (tmpBond != null)
            tmpBond.ModelPrice = price;
        else
        {
            tmpBond = bondSec;
            tmpBond.ModelPrice = price;
            BondList.Add(tmpBond);
        }
    }

    //Returns node which corresponds to security
    public SBond? GetNode(InstrumentTradeStockSharpModel sec) => BondList.Find(s => s.UnderlyingSecurity.Code == sec.Code);
}