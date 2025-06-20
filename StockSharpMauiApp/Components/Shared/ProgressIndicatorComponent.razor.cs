////////////////////////////////////////////////
// © https://github.com/badhitman - @FakeGov 
////////////////////////////////////////////////

using BlazorLib;
using Microsoft.AspNetCore.Components;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using SharedLib;

namespace StockSharpMauiApp.Components.Shared;

public partial class ProgressIndicatorComponent : BlazorBusyComponentBaseModel
{
    [Inject]
    StockSharpClientConfigModel rabbitConf { get; set; } = default!;

    [Inject]
    IServiceProvider servicesProvider { get; set; } = default!;


    IMqttClient mqttClient = default!;
    MqttFactory mqttFactory = new();

    int rotateDeg;

    long allBytesCount;
    uint allMessagesCount;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        mqttClient = mqttFactory.CreateMqttClient();
        mqttClient.ApplicationMessageReceivedAsync += ApplicationMessageReceived;
        await mqttClient.ConnectAsync(GetMqttClientOptionsBuilder);
        await mqttClient.SubscribeAsync("#");
    }

    Task ApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        //string content = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment).Trim();
        rotateDeg += 5;
        if (rotateDeg >= 360)
        {
            rotateDeg = 0;
        }
        Interlocked.Add(ref allBytesCount, e.ApplicationMessage.PayloadSegment.Count);
        Interlocked.Increment(ref allMessagesCount);
        StateHasChangedCall();
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        mqttClient.ApplicationMessageReceivedAsync -= ApplicationMessageReceived;
        mqttClient.Dispose();
        base.Dispose();
    }

    MqttClientOptions GetMqttClientOptionsBuilder
    {
        get
        {
            return new MqttClientOptionsBuilder()
               .WithTcpServer(rabbitConf.Host, rabbitConf.Port)
               .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
               .Build();
        }
    }
}