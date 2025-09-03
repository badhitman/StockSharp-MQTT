////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;

namespace SharedLib;

/// <summary>
/// Площадка
/// </summary>
[Index(nameof(Code))]
public class BoardStockSharpModelDB : BoardStockSharpViewModel
{
    /// <summary>
    /// Exchange
    /// </summary>
    public new ExchangeStockSharpModelDB? Exchange { get; set; }
    /// <summary>
    /// Exchange
    /// </summary>
    public int? ExchangeId { get; set; }

    /// <inheritdoc/>
    public override ExchangeStockSharpModel? GetExchange() => Exchange is null ? null : new ExchangeStockSharpModel().Bind(Exchange);

    /// <summary>
    /// Инструменты (биржевые торговые)
    /// </summary>
    public List<InstrumentStockSharpModelDB>? Instruments { get; set; }

    /// <inheritdoc/>
    public void SetUpdate(BoardStockSharpModel req)
    {
        Code = req.Code;
        LastUpdatedAtUTC = DateTime.UtcNow;
    }
}