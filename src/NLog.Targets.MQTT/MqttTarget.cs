using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using NLog.Config;

namespace NLog.Targets.MQTT
{
    [Target("Mqtt")]
    public class MqttTarget : TargetWithLayout
    {
        private IMqttClient mqttClient;
        private IMqttClientOptions mqttClientOptions;
        public MqttTarget()
        {
            Host = "localhost";
            Port = 1883;
            Topic = "iotgateway/log/";
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
        public string UserName { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        [RequiredParameter]
        public string Password { get; set; }

        public string Topic { get; set; }
        #endregion

        protected override void Write(LogEventInfo logEvent)
        {
            string logMessage = RenderLogEvent(this.Layout, logEvent);
            string hostName = RenderLogEvent(Host, logEvent);
            SendTheMessageToRemoteHost(hostName, logMessage);
        }

        private void SendTheMessageToRemoteHost(string hostName, string message)
        {
            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(Topic)
                .WithPayload(message)
                .Build();
            mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
        }

        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            mqttClient = new MqttFactory().CreateMqttClient();
            mqttClientOptions = new MqttClientOptionsBuilder()
                .WithClientId("NLog.Targets.MQTT")
                .WithTcpServer(Host, Port)
                .WithCredentials(UserName, Password)
                .Build();

            mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(x => OnDisconnectedAsync());
            mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
        }

        protected override void CloseTarget()
        {
            base.CloseTarget();
            mqttClient.DisconnectAsync(CancellationToken.None);
        }

        private async Task OnDisconnectedAsync()
        {
            try
            {
                await mqttClient.ConnectAsync(mqttClientOptions);
            }
            catch (Exception exception)
            {
                //"MQTT CONNECTING FAILED
            }
        }
    }
}