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
public partial class TradingRowComponent : StockSharpBaseComponent
{
    [Inject]
    IParametersStorageTransmission StorageRepo { get; set; } = default!;


    /// <inheritdoc/>
    [Parameter, EditorRequired]
    public required InstrumentTradeStockSharpViewModel Instrument { get; set; }

    /// <inheritdoc/>
    [Parameter]
    public bool ShowNamesInstruments { get; set; }

    /// <inheritdoc/>
    [CascadingParameter, EditorRequired]
    public required TradingAreaComponent Parent { get; set; }


    public StrategyTradeStockSharpModel StrategyTrade => StrategyTradeStockSharpModel.Build(Instrument, BasePrice, ValueOperation, ShiftPosition, SmallBidVolume, SmallOfferVolume, SmallOffset, WorkingVolume, IsSmall, IsAlter, LowLimit, HightLimit);
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

    decimal _hightLimitl;
    /// <inheritdoc/>
    public decimal HightLimit
    {
        get => _hightLimitl;
        private set
        {
            _hightLimitl = value;
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

    decimal _shiftPosition;
    /// <inheritdoc/>
    public decimal ShiftPosition
    {
        get => _shiftPosition;
        private set
        {
            _shiftPosition = value;
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


    StorageMetadataModel StoreKey => new()
    {
        ApplicationName = GlobalStaticConstantsTransmission.TransmissionQueues.TradeInstrumentStrategyStockSharpReceive,
        OwnerPrimaryKey = Instrument.Id,
        PropertyName = GlobalStaticConstantsRoutes.Routes.DUMP_ACTION_NAME,
    };

    public void UpdateConnectionNotificationHandle(AboutConnectResponseModel req)
    {
        AboutConnection!.Update(req);
        StateHasChangedCall();
    }

    StrategyTradeStockSharpModel? RestoreStrategy;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await SetBusyAsync();
        TResponseModel<StrategyTradeStockSharpModel> restoreStrategy = await StorageRepo.ReadParameterAsync<StrategyTradeStockSharpModel>(StoreKey);
        RestoreStrategy = restoreStrategy.Response;

        if (RestoreStrategy is not null)
        {
            _lowLimit = RestoreStrategy.LowLimit;
            _hightLimitl = RestoreStrategy.HightLimit;
            _isSmall = RestoreStrategy.IsSmall;
            _basePrice = RestoreStrategy.BasePrice;
            _valueOperation = RestoreStrategy.ValueOperation;
            _shiftPosition = RestoreStrategy.ShiftPosition;
            _isAlter = RestoreStrategy.IsAlter;

            _smallBidVolume = RestoreStrategy.SmallBidVolume;
            _smallOfferVolume = RestoreStrategy.SmallOfferVolume;
            _smallOffset = RestoreStrategy.SmallOffset;
            _workingVolume = RestoreStrategy.WorkingVolume;
        }

        await SetBusyAsync(false);
        Parent.AddRowComponent(this);
    }

    async Task SaveStrategy()
    {
        if (RestoreStrategy is null)
            RestoreStrategy = new()
            {
                ShiftPosition = _shiftPosition,
                BasePrice = _basePrice,
                ValueOperation = _valueOperation,

                SmallBidVolume = _smallBidVolume,
                SmallOfferVolume = _smallOfferVolume,
                SmallOffset = _smallOffset,
                WorkingVolume = _workingVolume,

                IsAlter = _isAlter,
                IsSmall = _isSmall,
                LowLimit = _lowLimit,
                HightLimit = _hightLimitl,

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

        TResponseModel<int> storeRes = await StorageRepo.SaveParameterAsync(RestoreStrategy, StoreKey, true, false);
        if (!storeRes.Success())
            SnackbarRepo.ShowMessagesResponse(storeRes.Messages);
    }

    public void Update(InstrumentTradeStockSharpViewModel sender)
    {
        Instrument.Reload(sender);
        StateHasChangedCall();
    }
}