////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using SharedLib;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// связь моделей StockSharp с локальными моделями
/// </summary>
public static class BindsStockSharpModelsExtensionsDB
{
    /// <inheritdoc/>
    public static InstrumentTradeStockSharpViewModel Build(this InstrumentTradeStockSharpViewModel main, InstrumentStockSharpModelDB inc)
    {
        main.Multiplier = inc.Multiplier;
        main.Currency = inc.Currency;
        main.Board = inc.Board;
        main.BondTypeInstrumentManual = inc.BondTypeInstrumentManual;
        main.CfiCode = inc.CfiCode;
        main.UnderlyingSecurityType = inc.UnderlyingSecurityType;
        main.UnderlyingSecurityId = inc.UnderlyingSecurityId;
        main.TypeInstrumentManual = inc.TypeInstrumentManual;
        main.TypeInstrument = inc.TypeInstrument;
        main.Currency = inc.Currency;
        main.ShortName = inc.ShortName;
        main.Comment = inc.Comment;
        main.Name = inc.Name;
        main.Shortable = inc.Shortable;
        main.SettlementType = inc.SettlementType;
        main.SettlementDate = inc.SettlementDate;
        main.PrimaryId = inc.PrimaryId;
        main.OptionType = inc.OptionType;
        main.OptionStyle = inc.OptionStyle;
        main.LastFairPrice = inc.LastFairPrice;
        main.LastUpdatedAtUTC = inc.LastUpdatedAtUTC;
        main.ISIN = inc.ISIN;
        main.MaturityDate = inc.MaturityDate;
        main.IssueDate = inc.IssueDate;
        main.IdRemote = inc.IdRemote;
        main.Id = inc.Id;
        main.FaceValue = inc.FaceValue;
        main.ExpiryDate = inc.ExpiryDate;
        main.Decimals = inc.Decimals;
        main.CreatedAtUTC = inc.CreatedAtUTC;
        main.CouponRate = inc.CouponRate;
        main.Code = inc.Code;
        main.Class = inc.Class;

        return main;
    }

    /// <inheritdoc/>
    public static InstrumentStockSharpModelDB Bind(this InstrumentStockSharpModelDB main, InstrumentTradeStockSharpModel inc)
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
        main.IdRemote = inc.IdRemote;

        main.Currency = inc.Currency;
        main.UnderlyingSecurityType = inc.UnderlyingSecurityType;
        main.TypeInstrument = inc.TypeInstrument;
        main.SettlementType = inc.SettlementType;
        main.OptionType = inc.OptionType;
        main.OptionStyle = inc.OptionStyle;

        main.NameNormalizedUpper = inc.Name is null ? "" : inc.Name.ToUpper();
        main.IdRemoteNormalizedUpper = inc.IdRemote?.ToUpper();

        //main.Board = new BoardStockSharpModelDB().Bind(inc.Board);

        return main;
    }

    /// <inheritdoc/>
    public static InstrumentStockSharpModelDB Bind(this InstrumentStockSharpModelDB main, InstrumentTradeStockSharpViewModel inc)
    {
        main.Id = inc.Id;
        main.LastUpdatedAtUTC = inc.LastUpdatedAtUTC;
        main.CreatedAtUTC = inc.CreatedAtUTC;

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
        main.IdRemote = inc.IdRemote;

        main.Currency = inc.Currency;
        main.UnderlyingSecurityType = inc.UnderlyingSecurityType;
        main.TypeInstrument = inc.TypeInstrument;
        main.SettlementType = inc.SettlementType;
        main.OptionType = inc.OptionType;
        main.OptionStyle = inc.OptionStyle;

        main.NameNormalizedUpper = inc.Name?.ToUpper();
        main.IdRemoteNormalizedUpper = inc.IdRemote?.ToUpper();

        //main.Board = new BoardStockSharpModelDB().Bind(inc.Board);

        return main;
    }

    /// <inheritdoc/>
    public static BoardStockSharpModelDB Bind(this BoardStockSharpModelDB main, BoardStockSharpModel inc)
    {
        main.Code = inc.Code;
        //main.Exchange = new ExchangeStockSharpModelDB().Bind(inc.Exchange);
        return main;
    }

    /// <inheritdoc/>
    public static ExchangeStockSharpModelDB Bind(this ExchangeStockSharpModelDB main, ExchangeStockSharpModel inc)
    {
        main.Name = inc.Name;
        main.CountryCode = inc.CountryCode;
        return main;
    }

    /// <inheritdoc/>
    public static PortfolioTradeModelDB Bind(this PortfolioTradeModelDB main, PortfolioStockSharpModel inc)
    {
        //main.Board = new BoardStockSharpModelDB().Bind(inc.Board);

        main.Currency = inc.Currency;
        main.State = inc.State;

        main.ClientCode = inc.ClientCode;
        main.Name = inc.Name;
        main.DepoName = inc.DepoName;
        //
        return main;
    }
}