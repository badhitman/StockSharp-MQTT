```
Enable-Migrations -ProjectName StockSharpDriver -StartUpProjectName StockSharpDriver -Verbose
```

```
EntityFrameworkCore\Add-Migration AppStockSharpContext001 -Context StockSharpAppContext -Project StockSharpDriver -StartupProject StockSharpDriver
EntityFrameworkCore\Update-Database -AppContext StockSharpAppContext -Project StockSharpDriver -StartupProject StockSharpDriver
```
```
EntityFrameworkCore\Add-Migration PropertiesStorageContext001 -Context PropertiesStorageContext -Project StockSharpDriver -StartupProject StockSharpDriver
EntityFrameworkCore\Update-Database -Context PropertiesStorageContext -Project StockSharpDriver -StartupProject StockSharpDriver
```

```
EntityFrameworkCore\Add-Migration NLogsContext001 -Context NLogsContext -Project StockSharpDriver -StartupProject StockSharpDriver
Update-Database -Context NLogsContext -Project StockSharpDriver -StartupProject StockSharpDriver
```

```
EntityFrameworkCore\Add-Migration AppTelegramBotContext001 -Context TelegramBotAppContext -Project StockSharpDriver -StartupProject StockSharpDriver
EntityFrameworkCore\Update-Database -AppContext TelegramBotAppContext -Project StockSharpDriver -StartupProject StockSharpDriver
```
