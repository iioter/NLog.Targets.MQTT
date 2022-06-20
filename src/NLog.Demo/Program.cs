using MQTTnet.AspNetCore;
using MQTTnet.AspNetCore.Extensions;
using NLog;
using NLog.Web;

//Warnning 这里的NLog.Targets.MQTT是通过nuget引用，调试可以用项目引用
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // NLog: Setup NLog for Dependency injection
    builder.Logging.ClearProviders();
    builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
    builder.Host.UseNLog();

    //AddHostedMqttServer
    builder.Services.AddHostedMqttServer(mqttServer =>
        {
            mqttServer.WithoutDefaultEndpoint();
        })
        .AddMqttConnectionHandler()
        .AddConnections();

    //Config Port
    builder.WebHost.UseKestrel(option =>
    {
        option.ListenAnyIP(1883, l => l.UseMqtt());
        option.ListenAnyIP(80);
    });
    var app = builder.Build();

    //UseStaticFiles html js etc.
    app.UseStaticFiles();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthorization();

    app.UseRouting();

    //Websocket Mqtt
    app.UseEndpoints(endpoints =>
    {
        //MqttServerWebSocket
        endpoints.MapConnectionHandler<MqttConnectionHandler>("/mqtt", options =>
        {
            options.WebSockets.SubProtocolSelector = MqttSubProtocolSelector.SelectSubProtocol;
        });
    });

    app.MapControllers();

    app.Run();
}
catch (Exception e)
{
    // NLog: catch setup errors
    logger.Error(e, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}