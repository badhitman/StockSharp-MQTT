////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;

namespace SharedLib;

/// <inheritdoc/>
[Index(nameof(LastUpdatedAtUTC)), Index(nameof(Id)), Index(nameof(StringId)), Index(nameof(BoardId)), Index(nameof(TransactionId)), Index(nameof(BrokerCode))]
public class OrderStockSharpModelDB : OrderStockSharpViewModel, IBaseStockSharpModel
{
    ///<inheritdoc/>
    public new InstrumentStockSharpModelDB? Instrument { get; set; }
    ///<inheritdoc/>
    public int InstrumentId { get; set; }

    ///<inheritdoc/>
    public new PortfolioTradeModelDB? Portfolio { get; set; }
    ///<inheritdoc/>
    public int PortfolioId { get; set; }

    /// <inheritdoc/>
    public static OrderStockSharpModelDB Build(OrderStockSharpModel req, int instrumentId)
    {
        return new()
        {
            AveragePrice = req.AveragePrice,
            Balance = req.Balance,
            BoardId = req.BoardId,
            Id = req.Id,
            BrokerCode = req.BrokerCode,
            CancelledTime = req.CancelledTime,
            ClientCode = req.ClientCode,
            Comment = req.Comment,
            Commission = req.Commission,
            CommissionCurrency = req.CommissionCurrency,
            Currency = req.Currency,
            ExpiryDate = req.ExpiryDate,
            InstrumentId = instrumentId,
            IsManual = req.IsManual,
            IsMarketMaker = req.IsMarketMaker,
            IsSystem = req.IsSystem,
            LatencyCancellation = req.LatencyCancellation,
            LatencyEdition = req.LatencyEdition,
            LatencyRegistration = req.LatencyRegistration,
            LocalTime = req.LocalTime,
            MarginMode = req.MarginMode,
            MatchedTime = req.MatchedTime,
            MinVolume = req.MinVolume,
            PositionEffect = req.PositionEffect,
            Yield = req.Yield,
            PostOnly = req.PostOnly,
            Price = req.Price,
            SeqNum = req.SeqNum,
            Side = req.Side,
            Slippage = req.Slippage,
            State = req.State,
            Status = req.Status,
            StringId = req.StringId,
            Time = req.Time,
            TimeInForce = req.TimeInForce,
            Type = req.Type,
            TransactionId = req.TransactionId,
            UserOrderId = req.UserOrderId,
            VisibleVolume = req.VisibleVolume,
            Volume = req.Volume,
        };
    }

    /// <inheritdoc/>
    public void SetUpdate(OrderStockSharpModel inc)
    {
        Id = inc.Id;
        LastUpdatedAtUTC = DateTime.UtcNow;

        State = inc.State;
        PositionEffect = inc.PositionEffect;
        TimeInForce = inc.TimeInForce;
        Type = inc.Type;
        Currency = inc.Currency;
        Side = inc.Side;
        MarginMode = inc.MarginMode;

        IsMarketMaker = inc.IsMarketMaker;
        Slippage = inc.Slippage;
        Price = inc.Price;
        ExpiryDate = inc.ExpiryDate;
        Yield = inc.Yield;
        Volume = inc.Volume;
        VisibleVolume = inc.VisibleVolume;
        MinVolume = inc.MinVolume;
        MatchedTime = inc.MatchedTime;
        Leverage = inc.Leverage;
        LatencyCancellation = inc.LatencyCancellation;
        IsManual = inc.IsManual;
        SeqNum = inc.SeqNum;
        Comment = inc.Comment;
        Time = inc.Time;
        Commission = inc.Commission;
        AveragePrice = inc.AveragePrice;
        Balance = inc.Balance;
        BoardId = inc.BoardId;
        UserOrderId = inc.UserOrderId;
        TransactionId = inc.TransactionId;
        StringId = inc.StringId;
        Status = inc.Status;
        PostOnly = inc.PostOnly;
        Slippage = inc.Slippage;
        LocalTime = inc.LocalTime;
        LatencyRegistration = inc.LatencyRegistration;
        LatencyEdition = inc.LatencyEdition;
        IsSystem = inc.IsSystem;
        CommissionCurrency = inc.CommissionCurrency;
        ClientCode = inc.ClientCode;
        CancelledTime = inc.CancelledTime;
        BrokerCode = inc.BrokerCode;
    }
}