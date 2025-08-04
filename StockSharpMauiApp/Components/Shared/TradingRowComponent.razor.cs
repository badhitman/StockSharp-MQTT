////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib.Components.StockSharp;
using Microsoft.AspNetCore.Components;
using BlazorLib;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared;

/// <summary>
/// TradingRowComponent
/// </summary>
public partial class TradingRowComponent : StockSharpAboutComponent
{
    [Inject]
    IParametersStorageTransmission StorageRepo { get; set; } = default!;


    /// <inheritdoc/>
    [Parameter, EditorRequired]
    public required InstrumentTradeStockSharpViewModel Instrument { get; set; }

    /// <inheritdoc/>
    [CascadingParameter, EditorRequired]
    public required TradingAreaComponent Parent { get; set; }


    public StrategyTradeStockSharpModel StrategyTrade => StrategyTradeStockSharpModel.Build(Instrument, BasePrice, ValueOperation, Offset, SmallBidVolume, SmallOfferVolume, SmallOffset, WorkingVolume, IsSmall, IsAlter, LowLimit, HightLimit);
    public bool Available => !EachDisable && Instrument.LastUpdatedAtUTC >= AboutConnection!.LastConnectedAt;

    decimal _lowLimit;
    /// <inheritdoc/>
    public decimal LowLimit
    {
        get => _lowLimit;
        private set
        {
            _lowLimit = value;
            InvokeAsync(SaveStrategy);
        }
    }

    decimal _hightLimit;
    /// <inheritdoc/>
    public decimal HightLimit
    {
        get => _hightLimit;
        private set
        {
            _hightLimit = value;
            InvokeAsync(SaveStrategy);
        }
    }

    bool _isSmall;
    public bool IsSmall
    {
        get
        {
            return _isSmall;
        }
        private set
        {
            _isSmall = value;
            InvokeAsync(SaveStrategy);
        }
    }

    bool _isAlter;
    public bool IsAlter
    {
        get
        {
            return _isAlter;
        }
        private set
        {
            _isAlter = value;
            InvokeAsync(SaveStrategy);
        }
    }

    decimal _basePrice;
    /// <inheritdoc/>
    public decimal BasePrice
    {
        get => _basePrice;
        private set
        {
            _basePrice = value;
            InvokeAsync(SaveStrategy);
        }
    }

    decimal _valueOperation;
    /// <inheritdoc/>
    public decimal ValueOperation
    {
        get => _valueOperation;
        private set
        {
            _valueOperation = value;
            InvokeAsync(SaveStrategy);
        }
    }

    decimal _offset;
    /// <inheritdoc/>
    public decimal Offset
    {
        get => _offset;
        private set
        {
            _offset = value;
            InvokeAsync(SaveStrategy);
        }
    }

    decimal _smallBidVolume;
    public decimal SmallBidVolume
    {
        get => _smallBidVolume;
        private set
        {
            _smallBidVolume = value;
            InvokeAsync(SaveStrategy);
        }
    }

    decimal _smallOfferVolume;
    public decimal SmallOfferVolume
    {
        get => _smallOfferVolume;
        private set
        {
            _smallOfferVolume = value;
            InvokeAsync(SaveStrategy);
        }
    }

    decimal _smallOffset;
    public decimal SmallOffset
    {
        get => _smallOffset;
        private set
        {
            _smallOffset = value;
            InvokeAsync(SaveStrategy);
        }
    }

    decimal _workingVolume;
    public decimal WorkingVolume
    {
        get => _workingVolume;
        private set
        {
            _workingVolume = value;
            InvokeAsync(SaveStrategy);
        }
    }

    public void UpdateConnectionNotificationHandle(AboutConnectResponseModel req)
    {
        AboutConnection!.Update(req);
        StateHasChangedCall();
    }

    StrategyTradeStockSharpModel? RestoreStrategy;

    protected override async Task OnInitializedAsync()
    {
        await SetBusyAsync();

        TResponseModel<StrategyTradeStockSharpModel> restoreStrategy = await StorageRepo.ReadParameterAsync<StrategyTradeStockSharpModel>(GlobalStaticCloudStorageMetadata.TradeInstrumentStrategyStockSharp(Instrument.Id));
        RestoreStrategy = restoreStrategy.Response;

        if (RestoreStrategy is not null)
        {
            _lowLimit = RestoreStrategy.LowLimit;
            _hightLimit = RestoreStrategy.HightLimit;
            _isSmall = RestoreStrategy.IsSmall;
            _basePrice = RestoreStrategy.BasePrice;
            _valueOperation = RestoreStrategy.ValueOperation;
            _offset = RestoreStrategy.Offset;
            _isAlter = RestoreStrategy.IsAlter;

            _smallBidVolume = RestoreStrategy.SmallBidVolume;
            _smallOfferVolume = RestoreStrategy.SmallOfferVolume;
            _smallOffset = RestoreStrategy.SmallOffset;
            _workingVolume = RestoreStrategy.WorkingVolume;
        }

        Parent.AddRowComponent(this);
        await base.OnInitializedAsync();
    }

    async Task SaveStrategy()
    {
        if (RestoreStrategy is null)
            RestoreStrategy = new()
            {
                Offset = _offset,
                BasePrice = _basePrice,
                ValueOperation = _valueOperation,

                SmallBidVolume = _smallBidVolume,
                SmallOfferVolume = _smallOfferVolume,
                SmallOffset = _smallOffset,
                WorkingVolume = _workingVolume,

                IsAlter = _isAlter,
                IsSmall = _isSmall,
                LowLimit = _lowLimit,
                HightLimit = _hightLimit,

                Board = Instrument.Board,
                CfiCode = Instrument.CfiCode,
                Class = Instrument.Class,
                Code = Instrument.Code,
                Currency = Instrument.Currency,
                Decimals = Instrument.Decimals,
                ExpiryDate = Instrument.ExpiryDate,
                FaceValue = Instrument.FaceValue,
                Id = Instrument.Id,
                IdRemote = Instrument.IdRemote,
                Multiplier = Instrument.Multiplier,
                Name = Instrument.Name,
                OptionStyle = Instrument.OptionStyle,
                OptionType = Instrument.OptionType,
                PrimaryId = Instrument.PrimaryId,
                SettlementDate = Instrument.SettlementDate,
                SettlementType = Instrument.SettlementType,
                Shortable = Instrument.Shortable,
                ShortName = Instrument.ShortName,
                TypeInstrument = Instrument.TypeInstrument,
                UnderlyingSecurityId = Instrument.UnderlyingSecurityId,
                UnderlyingSecurityType = Instrument.UnderlyingSecurityType,
            };
        else
            RestoreStrategy.Reload(StrategyTrade, Instrument);

        TResponseModel<int> storeRes = await StorageRepo.SaveParameterAsync(RestoreStrategy, GlobalStaticCloudStorageMetadata.TradeInstrumentStrategyStockSharp(Instrument.Id), true, false);
        if (!storeRes.Success())
            SnackBarRepo.ShowMessagesResponse(storeRes.Messages);
    }

    public void Update(InstrumentTradeStockSharpViewModel sender)
    {
        Instrument.Reload(sender);
        StateHasChangedCall();
    }
}