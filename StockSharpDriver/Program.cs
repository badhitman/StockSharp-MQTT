////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using Telegram.Bot.Services;
using StockSharpService;
using StorageService;
using RemoteCallLib;
using Telegram.Bot;
using SharedLib;
using DbcLib;
using NLog;
using HelpDeskService;

namespace StockSharpDriver;

/// <summary>
/// Program
/// </summary>
public class Program
{
    /// <summary>
    /// Main
    /// </summary>
    public static void Main(string[] args)
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        StockSharpClientConfigModel _conf = StockSharpClientConfigModel.BuildEmpty();
        IHostBuilder builderH = Host.CreateDefaultBuilder(args);

        string curr_dir = Directory.GetCurrentDirectory();
        string _environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        string appName = typeof(Program).Assembly.GetName().Name ?? "StockSharpDriverAssemblyNameDemo";

        builderH
            .ConfigureAppConfiguration((bx, builder) =>
            {
                builder.SetBasePath(curr_dir);

                string path_load = Path.Combine(curr_dir, "appsettings.json");
                if (File.Exists(path_load))
                    builder.AddJsonFile(path_load, optional: true, reloadOnChange: true);

                path_load = Path.Combine(curr_dir, $"appsettings.{_environmentName}.json");
                if (File.Exists(path_load))
                    builder.AddJsonFile(path_load, optional: true, reloadOnChange: true);

                // Secrets
                void ReadSecrets(string dirName)
                {
                    string secretPath = Path.Combine("..", dirName);
                    DirectoryInfo di = new(secretPath);
                    for (int i = 0; i < 5 && !di.Exists; i++)
                    {
                        secretPath = Path.Combine("..", secretPath);
                        di = new(secretPath);
                    }

                    if (Directory.Exists(secretPath))
                    {
                        foreach (string secret in Directory.GetFiles(secretPath, $"*.json"))
                        {
                            path_load = Path.GetFullPath(secret);
                            builder.AddJsonFile(path_load, optional: true, reloadOnChange: true);
                        }
                    }
                }
                ReadSecrets($"secrets_{nameof(StockSharpDriver)}");

                builder.AddEnvironmentVariables();
                builder.AddCommandLine(args);
                builder.Build();
                //_conf.Reload(bx.Configuration.GetValue<StockSharpClientConfigModel>("StockSharpDriverConfig"));
            })
            .ConfigureServices((bx, services) =>
            {
                IConfigurationRoot config = new ConfigurationBuilder()
                 .SetBasePath(basePath: Directory.GetCurrentDirectory()) //From NuGet Package Microsoft.Extensions.Configuration.Json
                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                 .Build();

                services.AddLogging(loggingBuilder =>
                {
                    // configure Logging with NLog
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog(config);
                });
                services.AddDbContextFactory<StockSharpAppContext>(opt =>
                {
#if DEBUG
                    opt.EnableSensitiveDataLogging(true);
                    //opt.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
#endif
                })
                .AddDbContextFactory<PropertiesStorageContext>(opt =>
                {
#if DEBUG
                    opt.EnableSensitiveDataLogging(true);
                    //opt.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
#endif
                })
                .AddDbContextFactory<NLogsContext>();

                // Register Bot configuration
                services.Configure<BotConfiguration>(bx.Configuration.GetSection(BotConfiguration.Configuration));

                // Register named HttpClient to benefits from IHttpClientFactory
                // and consume it with ITelegramBotClient typed client.
                // More read:
                //  https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0#typed-clients
                //  https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
                services.AddHttpClient("telegram_bot_client")
                        .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                        {
                            BotConfiguration botConfig = sp.GetConfiguration<BotConfiguration>();
                            TelegramBotClientOptions options = new(botConfig.BotToken);
                            return new TelegramBotClient(options, httpClient);
                        });
                //
                services
                    .AddSingleton<ITelegramBotStandardService, TelegramBotStandardService>()
                    .AddSingleton<IFlushStockSharpService, FlushStockSharpService>()
                    .AddSingleton<IDataStockSharpService, DataStockSharpService>()
                    .AddSingleton<IParametersStorage, ParametersStorage>()
                    .AddSingleton<IDriverStockSharpService, DriverStockSharpService>()
                    .AddSingleton<IManageStockSharpService, ManageStockSharpService>()
                    .AddSingleton<IRubricsService, RubricsService>()
                    .AddScoped<ILogsService, LogsNavigationImpl>()
                    .AddScoped<UpdateHandler>()
                    .AddScoped<ReceiverService>()
                    .AddHostedService<PollingService>()
                ;
                services.AddMemoryCache();

                ConnectionLink _connector = new();
                _conf.Reload(bx.Configuration.GetSection("StockSharpDriverConfig").Get<StockSharpClientConfigModel>());

                services
                    .AddSingleton(sp => _conf)
                    .AddSingleton(sp => _connector)
                ;

                services
                    .AddHostedService<MqttServerWorker>()
                    .AddHostedService<ConnectionStockSharpWorker>()
                ;

                #region MQ Transmission (remote methods call)
                services
                    .AddSingleton<IMQTTClient>(x => new MQttClient(x.GetRequiredService<StockSharpClientConfigModel>(), x.GetRequiredService<ILogger<MQttClient>>(), appName))

                    ;
                //
                services
                    .AddSingleton<IEventsStockSharp, EventsStockSharpTransmission>()
                ;

                services.StockSharpRegisterMqListeners();
                #endregion                 
            });

        IHost host = builderH.Build();
        //IDbContextFactory<NLogsContext> logsDbFactory
        IDbContextFactory<NLogsContext> _factNlogs = host.Services.GetRequiredService<IDbContextFactory<NLogsContext>>();
        NLogsContext _ctxNlogs = _factNlogs.CreateDbContext();

#if DEMO_SEED_DB
        IDbContextFactory<StockSharpAppContext> _factContext = host.Services.GetRequiredService<IDbContextFactory<StockSharpAppContext>>();
        StockSharpAppContext _ctxDriver = _factContext.CreateDbContext();
        if (!_ctxDriver.Exchanges.Any() && !_ctxDriver.Boards.Any() && !_ctxDriver.Instruments.Any())
        {
            InstrumentStockSharpModelDB _insSeed = new()
            {
                CreatedAtUTC = DateTime.UtcNow,
                LastUpdatedAtUTC = DateTime.UtcNow,
                Board = new()
                {
                    Code = "board-DEMO_SEED_DB",
                    Exchange = new()
                    {
                        Name = "Exchange DEMO_SEED_DB",
                        CountryCode = (int?)CountryCodesEnum.RU,
                    },
                    CreatedAtUTC = DateTime.UtcNow,
                    LastUpdatedAtUTC = DateTime.UtcNow,
                },
                Code = "code-DEMO_SEED_DB",
                Currency = (int)CurrenciesTypesEnum.RUB,
                IdRemote = "code-DEMO_SEED_DB@board-DEMO_SEED_DB",
                IssueDate = DateTime.UtcNow,
                Name = "instrument DEMO_SEED_DB",
                TypeInstrument = (int)InstrumentsStockSharpTypesEnum.Bond,
                SettlementType = (int)SettlementTypesEnum.Delivery,
                BondTypeInstrumentManual = (int)BondsTypesInstrumentsManualEnum.Floater,
                CouponRate = 1,
                LastFairPrice = 9,
                MaturityDate = DateTime.UtcNow,
                TypeInstrumentManual = (int)TypesInstrumentsManualEnum.Futures,
                 
                ISIN = "RU000A101QE0",
                CashFlows = [],
            };
            _insSeed.CashFlows.Add(new()
            {
                Instrument = _insSeed,
                CashFlowType = (int)CashFlowTypesEnum.Notional,
                PaymentDate = DateTime.UtcNow,
                PaymentValue = 5
            });
            _ctxDriver.Instruments.Add(_insSeed);
            _ctxDriver.SaveChanges();
        }
#endif

        logger.Info($"Program has started (logs count: {_ctxNlogs.Logs.Count()}).");
        host.Run();
    }
}