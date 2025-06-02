```
Enable-Migrations -ProjectName StockSharpDriver -StartUpProjectName StockSharpDriver -Verbose
```

```
EntityFrameworkCore\Add-Migration AppStockSharpContext001 -Context StockSharpAppContext -Project StockSharpDriver -StartupProject StockSharpDriver
EntityFrameworkCore\Update-Database -AppContext StockSharpContext -Project StockSharpDriver -StartupProject StockSharpDriver
```
```
EntityFrameworkCore\Add-Migration PropertiesStorageContext001 -Context PropertiesStorageContext -Project StockSharpDriver -StartupProject StockSharpDriver
EntityFrameworkCore\Update-Database -Context PropertiesStorageContext -Project StockSharpDriver -StartupProject StockSharpDriver
```

```
EntityFrameworkCore\Add-Migration NLogsContext002 -Context NLogsContext -Project StockSharpDriver -StartupProject StockSharpDriver
Update-Database -Context NLogsContext -Project StockSharpDriver -StartupProject StockSharpDriver
```