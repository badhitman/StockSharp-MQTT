## StockSharp - integration (over mqtt) - starter case

Client-server solution compatible with BlankCRM, but has its own/native (local: win/android/ios/macos/tizen) client. The license for the  [StockSharp](https://stocksharp.ru/?rf=202744) is purchased separately.

Trade
![trade view](./StockSharpMauiApp/img/trade-manage.png)

Connection management
![init](./StockSharpMauiApp/img/init-clear.png)
- Before connecting, you should configure the adapters

Adapters
![adapters](./StockSharpMauiApp/img/adapters-view.png)

Instruments (aka Securities)
![instruments view](./StockSharpMauiApp/img/instruments-view.png)
Manage
![instruments view](./StockSharpMauiApp/img/instrument-manage.png)

Rubrics (segments)
![rubrics view](./StockSharpMauiApp/img/rubrics-view.png)

System (configs)
![logs](./StockSharpMauiApp/img/system.png)

Logs (imported of BlankCRM)
![logs](./StockSharpMauiApp/img/logs.png)

- Dreiver (BackEnd service): net6 solution interacts with StockSharp, logging, broadcasts events in MQTT and responds to incoming requests (from the outside). +TelegramBot, as well as a built-in MQTT server (in case there is no separate/autonomous mqtt service).
- MAUI-Blazor client: net 9 GUI solution that communicates with the driver via MQTT

#### StockSharpDriver +TelegramBot
Built-in MQTT server, but you can use any MQTT v5. By default, localhost:1883 is used, but you can configure it as you wish.
In terms of events - broadcasts them all in MQTT so that any client can listen to it. Isolates "unwanted" dependencies from the original StockSharp build, but is also not compatible with the original StockSharp solutions (Hydra, Designer, etc.).
Built-in TelegramBot for access to the service and notifications.

#### MAUI-Blazor client
Demonstration of client interaction with StockSharp driver (over MQTT). Clean project without StockSharp dependencies (net6, wpf, etc ...).
Open source code for a trading bot template via StockSharp connector.