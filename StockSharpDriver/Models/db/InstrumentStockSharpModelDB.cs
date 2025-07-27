////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;

namespace SharedLib;

/// <summary>
/// InstrumentTradeModelDB
/// </summary>
[Index(nameof(UnderlyingSecurityId)), Index(nameof(PrimaryId)), Index(nameof(LastUpdatedAtUTC))]
[Index(nameof(IdRemote)), Index(nameof(Code)), Index(nameof(Class)), Index(nameof(CfiCode))]
[Index(nameof(NameNormalizedUpper)), Index(nameof(IdRemoteNormalizedUpper)), Index(nameof(CreatedAtUTC)), Index(nameof(LastUpdatedAtUTC))]
public class InstrumentStockSharpModelDB : InstrumentTradeStockSharpViewModel, IBaseStockSharpModel
{
    /// <inheritdoc/>
    public new BoardStockSharpModelDB Board { get; set; }
    /// <inheritdoc/>
    public int BoardId { get; set; }

    /// <inheritdoc/>
    public string NameNormalizedUpper { get; set; }

    /// <inheritdoc/>
    public string IdRemoteNormalizedUpper { get; set; }

    /// <inheritdoc/>
    public new List<InstrumentMarkersModelDB> Markers { get; set; }

    /// <inheritdoc/>
    public List<CashFlowModelDB> CashFlows { get; set; }

    /// <inheritdoc/>
    public void SetUpdate(InstrumentTradeStockSharpModel req, bool nameUpdate = false)
    {
        if (nameUpdate || (string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(req.Name)))
        {
            Name = req.Name;
            NameNormalizedUpper = req.Name is null ? "" : req.Name.ToUpper();
        }

        IdRemote = req.IdRemote;
        IdRemoteNormalizedUpper = req.IdRemote.ToUpper();

        LastUpdatedAtUTC = DateTime.UtcNow;
        ExpiryDate = req.ExpiryDate;
        SettlementDate = req.SettlementDate;
        OptionStyle = req.OptionStyle;
        Currency = req.Currency;
        OptionType = req.OptionType;
        UnderlyingSecurityType = req.UnderlyingSecurityType;
        PrimaryId = req.PrimaryId;
        UnderlyingSecurityId = req.UnderlyingSecurityId;
        TypeInstrument = req.TypeInstrument;
        ShortName = req.ShortName;
        Shortable = req.Shortable;
        SettlementType = req.SettlementType;
        Multiplier = req.Multiplier;
        FaceValue = req.FaceValue;
        Decimals = req.Decimals;
        Code = req.Code;
        Class = req.Class;
        CfiCode = req.CfiCode;

        if (req is InstrumentTradeStockSharpViewModel other)
        {
            BondTypeInstrumentManual = other.BondTypeInstrumentManual;
            TypeInstrumentManual = other.TypeInstrumentManual;
            ISIN = other.ISIN;
            IssueDate = other.IssueDate;
            MaturityDate = other.MaturityDate;
            CouponRate = other.CouponRate;
            LastFairPrice = other.LastFairPrice;
            Comment = other.Comment;
        }
    }
}