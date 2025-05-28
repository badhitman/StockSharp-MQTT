////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib.Components.StockSharp;
using Microsoft.AspNetCore.Components;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared;

/// <summary>
/// TradingRowComponent
/// </summary>
public partial class TradingRowComponent : StockSharpBaseComponent
{
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
        }
    }


    protected override void OnInitialized()
    {
        base.OnInitialized();
        Parent.AddRowComponent(this);
    }

    public void Update(InstrumentTradeStockSharpViewModel sender)
    {
        Instrument.Reload(sender);
        StateHasChangedCall();
    }
}