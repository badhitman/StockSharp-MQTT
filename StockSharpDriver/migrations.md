```
Enable-Migrations -ProjectName StockSharpDriver -StartUpProjectName StockSharpDriver -Verbose
```

```
EntityFrameworkCore\Add-Migration StockSharpAppContext002 -Context StockSharpAppContext -Project StockSharpDriver -StartupProject StockSharpDriver
EntityFrameworkCore\Update-Database -Context StockSharpAppContext -Project StockSharpDriver -StartupProject StockSharpDriver
```

```
EntityFrameworkCore\Add-Migration NLogsContext002 -Context NLogsContext -Project StockSharpDriver -StartupProject StockSharpDriver
Update-Database -Context NLogsContext -Project StockSharpDriver -StartupProject StockSharpDriver
```