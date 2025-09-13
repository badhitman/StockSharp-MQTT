////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;
using SharedLib;

namespace DbcLib;

/// <inheritdoc/>
public abstract partial class TelegramBotAppLayerContext : DbContext
{
    /// <summary>
    /// FileName
    /// </summary>
    private static readonly string _ctxName = nameof(StockSharpAppContext);

    /// <summary>
    /// db Path
    /// </summary>
    public static string DbPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _ctxName, $"{(AppDomain.CurrentDomain.FriendlyName.Equals("ef", StringComparison.OrdinalIgnoreCase) ? "TelegramData" : AppDomain.CurrentDomain.FriendlyName)}.TelegramBot.db3");


    /// <inheritdoc/>
    public TelegramBotAppLayerContext(DbContextOptions options)
        : base(options)
    {
        FileInfo _fi = new(DbPath);
        if (_fi.Directory?.Exists != true)
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);

        Database.Migrate();
    }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
#if DEBUG
        options.EnableSensitiveDataLogging(true);
        // options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
#endif
        base.OnConfiguring(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoleUserTelegramModelDB>().HasKey(ol => new { ol.Role, ol.UserId });

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Chats
    /// </summary>
    public DbSet<ChatTelegramModelDB> Chats { get; set; } = default!;

    /// <summary>
    /// Users
    /// </summary>
    public DbSet<UserTelegramModelDB> Users { get; set; } = default!;

    /// <summary>
    /// JoinsUsersToChats
    /// </summary>
    public DbSet<JoinUserChatModelDB> JoinsUsersToChats { get; set; } = default!;

    /// <summary>
    /// Roles for users
    /// </summary>
    public DbSet<RoleUserTelegramModelDB> RolesUsers { get; set; } = default!;

    /// <summary>
    /// Messages
    /// </summary>
    public DbSet<MessageTelegramModelDB> Messages { get; set; } = default!;
}