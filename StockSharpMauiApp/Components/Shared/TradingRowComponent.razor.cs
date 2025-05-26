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


    bool Available => !EachDisable && Instrument.LastUpdatedAtUTC >= AboutConnection!.LastConnectedAt;


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
}