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
    [CascadingParameter, EditorRequired]
    public required TradingAreaComponent Parent { get; set; }


    public StrategyTradeStockSharpModel StrategyTrade => StrategyTradeStockSharpModel.Build(Instrument, BasePrice, ValueOperation, ShiftPosition, IsMM, L1, L2);
    public bool Available => !EachDisable && Instrument.LastUpdatedAtUTC >= AboutConnection!.LastConnectedAt;

    decimal _l1;
    /// <inheritdoc/>
    public decimal L1
    {
        get => _l1;
        private set
        {
            _l1 = value;
            InvokeAsync(SaveStrategy);
        }
    }

    decimal _l2;
    /// <inheritdoc/>
    public decimal L2
    {
        get => _l2;
        private set
        {
            _l2 = value;
            InvokeAsync(SaveStrategy);
        }
    }

    bool _isMM;
    public bool IsMM
    {
        get
        {
            return _isMM;
        }
        private set
        {
            _isMM = value;
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
            _l1 = RestoreStrategy.L1;
            _l2 = RestoreStrategy.L2;
            _isMM = RestoreStrategy.IsMM;
            _basePrice = RestoreStrategy.BasePrice;
            _valueOperation = RestoreStrategy.ValueOperation;
            _shiftPosition = RestoreStrategy.ShiftPosition;
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
                IsMM = _isMM,
                L2 = _l2,
                L1 = _l1,

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