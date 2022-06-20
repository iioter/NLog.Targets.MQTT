using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using NLog.Config;
using Newtonsoft.Json;
using NLog.Common;

namespace NLog.Targets.MQTT
{
    [Target("Mqtt")]
    public class MqttTarget : TargetWithLayout
    {
        private IMqttClient? _mqttClient;
        private IMqttClientOptions? _mqttClientOptions;
        public MqttTarget()
        {
            Host = "localhost";
            Port = 1883;
            Topic = "/log";
            ClientName = "NLog.Targets.MQTT.Client";
        }

        #region RequiredParameters
        /// <summary>
        /// Broker Host
        /// </summary>
        [RequiredParameter]
        public string Host { get; set; }

        /// <summary>
        /// Broker Port
        /// </summary>
        [RequiredParameter]
        public int Port { get; set; }

        /// <summary>
        /// UserName
        /// </summary>
        [RequiredParameter]
        public string? UserName { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [RequiredParameter]
        public string? Password { get; set; }

        /// <summary>
        /// Topic
        /// </summary>
        [RequiredParameter]
        public string Topic { get; set; }

        /// <summary>
        /// ClientName
        /// </summary>
        [RequiredParameter]
        public string ClientName { get; set; }

        /// <summary>
        /// Gets or sets a list of additional fields to add to the elasticsearch document.
        /// </summary>
        [ArrayParameter(typeof(Field), "field")]
        public IList<Field> Fields { get; set; } = new List<Field>();
        #endregion

        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            _mqttClient = new MqttFactory().CreateMqttClient();
            _mqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId($"{ClientName}-{Guid.NewGuid()}")
                .WithTcpServer(Host, Port)
                .WithCredentials(UserName, Password)
                .Build();

            _mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(x => 
                OnDisconnectedAsync());
            _mqttClient.ConnectAsync(_mqttClientOptions, CancellationToken.None);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            //default fields
            Dictionary<string, object> logDictionary = new()
            {
                { "timestamp", logEvent.TimeStamp },
                { "level", logEvent.Level.Name },
                { "message", RenderLogEvent(Layout, logEvent) }
            };

            //customer fields
            //这里都处理为字符串了，有优化空间
            foreach (var field in Fields)
            {
                var renderedField = RenderLogEvent(field.Layout, logEvent);

                if (string.IsNullOrWhiteSpace(renderedField))
                    continue;

                logDictionary[field.Name] = renderedField;
            }

            SendTheMessage2MqttBroker(JsonConvert.SerializeObject(logDictionary));
        }

        private void SendTheMessage2MqttBroker(string payload)
        {
            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(Topic)
                .WithPayload(payload)
                .Build();
            _mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
        }

        protected override void CloseTarget()
        {
            base.CloseTarget();
            _mqttClient.DisconnectAsync(CancellationToken.None);
        }

        private async Task OnDisconnectedAsync()
        {
            try
            {
                await _mqttClient.ConnectAsync(_mqttClientOptions);
            }
            catch (Exception exception)
            {
                InternalLogger.Error(exception, "Error reconnecting to MQTT broker.");
            }
        }
    }
}