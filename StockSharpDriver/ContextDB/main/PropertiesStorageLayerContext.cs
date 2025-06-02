////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;
using SharedLib;

namespace DbcLib;

/// <inheritdoc/>
public partial class PropertiesStorageLayerContext : DbContext
{
    /// <summary>
    /// FileName
    /// </summary>
    private static readonly string _ctxName = nameof(StockSharpAppContext);

    /// <summary>
    /// db Path
    /// </summary>
    public static string DbPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _ctxName, $"{(AppDomain.CurrentDomain.FriendlyName.Equals("ef", StringComparison.OrdinalIgnoreCase) ? "StockSharpAppData" : AppDomain.CurrentDomain.FriendlyName)}.Properties.db3");


    /// <inheritdoc/>
    public PropertiesStorageLayerContext(DbContextOptions options)
        : base(options)
    {
        //#if DEBUG
        //        Database.EnsureCreated();
        //#else
        Database.Migrate();
        //#endif
    }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
#if DEBUG
        options.EnableSensitiveDataLogging(true);
        // options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
#endif
    }

    /// <summary>
    /// Параметры
    /// </summary>
    public DbSet<StorageCloudParameterModelDB> CloudProperties { get; set; } = default!;

    /// <summary>
    /// Тэги
    /// </summary>
    public DbSet<TagModelDB> CloudTags { get; set; } = default!;

    /// <summary>
    /// Тэги
    /// </summary>
    public DbSet<RubricModelDB> Rubrics { get; set; } = default!;

    /// <summary>
    /// Блокировщики
    /// </summary>
    public DbSet<LockUniqueTokenModelDB> Lockers { get; set; } = default!;
}