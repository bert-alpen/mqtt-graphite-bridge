using System;
using System.Globalization;
using System.Threading.Tasks;
using ahd.Graphite;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;

namespace MqttGraphiteBridge
{
    public class MqttClientFactory : IMqttClientFactory
    {
        public IMqttClient CreateSourceClient(
            Endpoint sourceConfiguration, 
            Endpoint targetConfiguration,
            MqttClientConnectedHandlerDelegate connectedHandler,
            MqttApplicationMessageReceivedHandlerDelegate messageReceivedHandler,
            MqttClientDisconnectedHandlerDelegate disconnectedHandler)
        {

            var mqttClient = new MqttFactory().CreateMqttClient();

            mqttClient.ConnectedHandler = connectedHandler;
            mqttClient.ApplicationMessageReceivedHandler = messageReceivedHandler;
            mqttClient.DisconnectedHandler = disconnectedHandler;

            return mqttClient;
        }

        public IMqttClientOptions CreateSourceOptions(Endpoint sourceConfiguration, string clientId)
        {
            return new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTcpServer(sourceConfiguration.Host, sourceConfiguration.Port)
                .WithCredentials(sourceConfiguration.UserName, sourceConfiguration.Password)
                .WithCleanSession()
                //.WithCommunicationTimeout(new TimeSpan(0, 0, 5))
                .Build();
        }
    }
}
