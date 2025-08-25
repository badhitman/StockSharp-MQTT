////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;

namespace SharedLib;

/// <summary>
/// Биржа
/// </summary>
[Index(nameof(Name))]
public class ExchangeStockSharpModelDB : ExchangeStockSharpViewModel
{
    /// <summary>
    /// Boards
    /// </summary>
    public List<BoardStockSharpModelDB>? Boards { get; set; }

    /// <inheritdoc/>
    public void SetUpdate(ExchangeStockSharpModel req)
    {
        CountryCode = req.CountryCode;
        Name = req.Name;
    }
}