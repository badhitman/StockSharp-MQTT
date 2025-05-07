////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SharedLib;

/// <summary>
/// FixMessageAdapterModelDB
/// </summary>
[Index(nameof(LastUpdatedAtUTC)), Index(nameof(Name)), Index(nameof(IsOffline))]
[Index(nameof(AdapterTypeName), nameof(IsOffline), IsUnique = true)]
public class FixMessageAdapterModelDB : FixMessageAdapterModel, IBaseStockSharpModel
{
    /// <summary>
    /// Идентификатор/Key
    /// </summary>
    [Key]
    public int Id { get; set; }


    /// <inheritdoc/>
    public DateTime LastUpdatedAtUTC { get; set; }

    /// <inheritdoc/>
    public DateTime CreatedAtUTC { get; set; }
}