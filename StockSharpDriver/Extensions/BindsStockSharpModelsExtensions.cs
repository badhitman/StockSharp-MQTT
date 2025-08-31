////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using StockSharp.BusinessEntities;
using SharedLib;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// связь моделей StockSharp с локальными моделями
/// </summary>
public static class BindsStockSharpModelsExtensions
{
    /// <inheritdoc/>
    public static OrderStockSharpModel Bind(this OrderStockSharpModel main, Order inc)
    {
        main.Id = inc.Id;

        main.Instrument = new InstrumentTradeStockSharpModel().Bind(inc.Security);
        main.Portfolio = new PortfolioStockSharpModel().Bind(inc.Portfolio);

        main.State = (OrderStatesEnum)Enum.Parse(typeof(OrderStatesEnum), Enum.GetName(inc.State)!);
        main.PositionEffect = inc.PositionEffect is null ? null : (OrderPositionEffectsEnum)Enum.Parse(typeof(OrderPositionEffectsEnum), Enum.GetName(inc.PositionEffect.Value)!);
        main.TimeInForce = inc.TimeInForce is null ? null : (TimeInForceEnum)Enum.Parse(typeof(TimeInForceEnum), Enum.GetName(inc.TimeInForce.Value)!);
        main.Type = inc.Type is null ? null : (OrderTypesEnum)Enum.Parse(typeof(OrderTypesEnum), Enum.GetName(inc.Type.Value)!);
        main.Currency = inc.Currency is null ? null : (CurrenciesTypesEnum)Enum.Parse(typeof(CurrenciesTypesEnum), Enum.GetName(inc.Currency.Value)!);
        main.Side = (SidesEnum)Enum.Parse(typeof(SidesEnum), Enum.GetName(inc.Side)!);
        main.MarginMode = inc.MarginMode is null ? null : (MarginModesEnum)Enum.Parse(typeof(MarginModesEnum), Enum.GetName(inc.MarginMode.Value)!);

        main.IsMarketMaker = inc.IsMarketMaker;
        main.Slippage = inc.Slippage;
        main.Price = inc.Price;
        main.ExpiryDate = inc.ExpiryDate;
        main.Yield = inc.Yield;
        main.Volume = inc.Volume;
        main.VisibleVolume = inc.VisibleVolume;
        main.MinVolume = inc.MinVolume;
        main.MatchedTime = inc.MatchedTime;
        main.Leverage = inc.Leverage;
        main.LatencyCancellation = inc.LatencyCancellation;
        main.IsManual = inc.IsManual;
        main.SeqNum = inc.SeqNum;
        main.Comment = inc.Comment;
        main.Time = inc.Time;
        main.Commission = inc.Commission;
        main.AveragePrice = inc.AveragePrice;
        main.Balance = inc.Balance;
        main.BoardId = inc.BoardId;
        main.UserOrderId = inc.UserOrderId;
        main.TransactionId = inc.TransactionId;
        main.StringId = inc.StringId;
        main.Status = inc.Status;
        main.PostOnly = inc.PostOnly;
        main.Slippage = inc.Slippage;
        main.LocalTime = inc.LocalTime;
        main.LatencyRegistration = inc.LatencyRegistration;
        main.LatencyEdition = inc.LatencyEdition;
        main.IsSystem = inc.IsSystem;
        main.CommissionCurrency = inc.CommissionCurrency;
        main.ClientCode = inc.ClientCode;
        main.CancelledTime = inc.CancelledTime;
        main.BrokerCode = inc.BrokerCode;

        return main;
    }

    /// <inheritdoc/>
    public static InstrumentTradeStockSharpModel Bind(this InstrumentTradeStockSharpModel main, Security inc)
    {
        main.Multiplier = inc.Multiplier;
        main.FaceValue = inc.FaceValue;
        main.SettlementDate = inc.SettlementDate;
        main.Decimals = inc.Decimals;
        main.CfiCode = inc.CfiCode;
        main.Class = inc.Class;
        main.Code = inc.Code;
        main.ExpiryDate = inc.ExpiryDate;
        main.UnderlyingSecurityId = inc.UnderlyingSecurityId;
        main.ShortName = inc.ShortName;
        main.Shortable = inc.Shortable;
        main.PrimaryId = inc.PrimaryId;
        main.Name = inc.Name;
        main.IdRemote = inc.Id;

        main.MaxVolume = inc.MaxVolume;
        main.MinVolume = inc.MinVolume;
        main.VolumeStep = inc.VolumeStep;
        main.PriceStep = inc.PriceStep;

        main.Currency = (int)(inc.Currency is null ? CurrenciesTypesEnum.None : (CurrenciesTypesEnum)Enum.Parse(typeof(CurrenciesTypesEnum), Enum.GetName(inc.Currency.Value)!));
        main.UnderlyingSecurityType = (int)(inc.UnderlyingSecurityType is null ? InstrumentsStockSharpTypesEnum.None : (InstrumentsStockSharpTypesEnum)Enum.Parse(typeof(InstrumentsStockSharpTypesEnum), Enum.GetName(inc.UnderlyingSecurityType.Value)!));
        main.TypeInstrument = (int)(inc.Type is null ? InstrumentsStockSharpTypesEnum.None : (InstrumentsStockSharpTypesEnum)Enum.Parse(typeof(InstrumentsStockSharpTypesEnum), Enum.GetName(inc.Type.Value)!));
        main.SettlementType = (int)(inc.SettlementType is null ? SettlementTypesEnum.None : (SettlementTypesEnum)Enum.Parse(typeof(SettlementTypesEnum), Enum.GetName(inc.SettlementType.Value)!));
        main.OptionType = (int)(inc.OptionType is null ? OptionInstrumentTradeTypesEnum.None : (OptionInstrumentTradeTypesEnum)Enum.Parse(typeof(OptionInstrumentTradeTypesEnum), Enum.GetName(inc.OptionType.Value)!));
        main.OptionStyle = (int)(inc.OptionStyle is null ? OptionTradeInstrumentStylesEnum.None : (OptionTradeInstrumentStylesEnum)Enum.Parse(typeof(OptionTradeInstrumentStylesEnum), Enum.GetName(inc.OptionStyle.Value)!));

        main.Board = inc.Board is null
            ? null
            : new BoardStockSharpModel().Bind(inc.Board);

        return main;
    }

    /// <inheritdoc/>
    public static BoardStockSharpModel Bind(this BoardStockSharpModel main, ExchangeBoard inc)
    {
        main.Code = inc.Code;
        main.Exchange = new ExchangeStockSharpModel().Bind(inc.Exchange);
        return main;
    }

    /// <inheritdoc/>
    public static BoardStockSharpModel Bind(this BoardStockSharpModel main, BoardStockSharpViewModel inc)
    {
        main.Code = inc.Code;

        if (inc is BoardStockSharpModelDB _other)
            main.Exchange = _other.Exchange;
        else
            main.Exchange = inc.Exchange;

        return main;
    }

    /// <inheritdoc/>
    public static ExchangeStockSharpModel Bind(this ExchangeStockSharpModel main, Exchange inc)
    {
        main.Name = inc.Name;
        main.CountryCode = inc.CountryCode is null ? null : (int)(CountryCodesEnum)Enum.Parse(typeof(CountryCodesEnum), Enum.GetName(inc.CountryCode.Value)!);
        return main;
    }

    /// <inheritdoc/>
    public static PortfolioStockSharpModel Bind(this PortfolioStockSharpModel main, Portfolio inc)
    {
        main.Board = inc.Board is null ? null : new BoardStockSharpModel().Bind(inc.Board);

        main.Currency = inc.Currency is null ? null : (CurrenciesTypesEnum)Enum.Parse(typeof(CurrenciesTypesEnum), Enum.GetName(inc.Currency.Value)!);
        main.State = inc.State is null ? null : (PortfolioStatesEnum)Enum.Parse(typeof(PortfolioStatesEnum), Enum.GetName(inc.State.Value)!);

        main.ClientCode = inc.ClientCode;
        main.Name = inc.Name;
        main.DepoName = inc.DepoName;
        //
        main.BeginValue = inc.BeginValue;
        main.CurrentValue = inc.CurrentValue;

        return main;
    }

    /// <inheritdoc/>
    public static PositionStockSharpModel Bind(this PositionStockSharpModel main, Position inc)
    {
        main.PortfolioName = inc.PortfolioName;
        main.BeginValue = inc.BeginValue;
        main.CurrentValue = inc.CurrentValue;
        main.BlockedValue = inc.BlockedValue;
        main.CurrentPrice = inc.CurrentPrice;
        main.AveragePrice = inc.AveragePrice;
        main.UnrealizedPnL = inc.UnrealizedPnL;
        main.RealizedPnL = inc.RealizedPnL;
        main.VariationMargin = inc.VariationMargin;
        main.Commission = inc.Commission;
        main.SettlementPrice = inc.SettlementPrice;
        main.LastChangeTime = inc.LastChangeTime;
        main.LocalTime = inc.LocalTime;
        main.Description = inc.Description;
        main.Currency = inc.Currency is null ? null : (CurrenciesTypesEnum)Enum.Parse(typeof(CurrenciesTypesEnum), Enum.GetName(inc.Currency.Value)!);
        main.ExpirationDate = inc.ExpirationDate;
        main.ClientCode = inc.ClientCode;
        main.Portfolio = inc.Portfolio is null ? null : new PortfolioStockSharpModel().Bind(inc.Portfolio);
        main.Instrument = new InstrumentTradeStockSharpModel().Bind(inc.Security);
        main.DepoName = inc.DepoName;

        if (inc.LimitType is not null)
        {
            string? _gn = Enum.GetName(inc.LimitType.Value);
            if (!string.IsNullOrWhiteSpace(_gn))
                main.LimitType = inc.LimitType is null ? null : (TPlusLimitsEnum)Enum.Parse(typeof(TPlusLimitsEnum), _gn);
        }

        main.StrategyId = inc.StrategyId;
        main.Side = inc.Side is null ? null : (SidesEnum)Enum.Parse(typeof(SidesEnum), Enum.GetName(inc.Side.Value)!);
        main.Leverage = inc.Leverage;
        main.CommissionTaker = inc.CommissionTaker;
        main.CommissionMaker = inc.CommissionMaker;
        main.BuyOrdersCount = inc.BuyOrdersCount;
        main.SellOrdersCount = inc.SellOrdersCount;
        main.BuyOrdersMargin = inc.BuyOrdersMargin;
        main.SellOrdersMargin = inc.SellOrdersMargin;
        main.OrdersMargin = inc.OrdersMargin;
        main.OrdersCount = inc.OrdersCount;
        main.TradesCount = inc.TradesCount;
        main.LiquidationPrice = inc.LiquidationPrice;

        return main;
    }
}