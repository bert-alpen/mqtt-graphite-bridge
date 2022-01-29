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
        private readonly ILogger<MqttClientFactory> _logger;

        public MqttClientFactory(ILogger<MqttClientFactory> logger)
        {
            _logger = logger;
        }
        public IMqttClient CreateSourceClient(Endpoint sourceConfiguration)
        {
            var mqttClient = new MqttFactory().CreateMqttClient();

            mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(args =>
            {
                _logger.Log(LogLevel.Information, $"Publisher  {sourceConfiguration.Host}:{sourceConfiguration.Port} Connected");
            });

            mqttClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(args =>
            {
                _logger.Log(LogLevel.Information, $"Message received for topic {args.ApplicationMessage.Topic}: {System.Text.Encoding.UTF8.GetString(args.ApplicationMessage.Payload)}");
            });

            mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(args =>
            {
                if (args.ClientWasConnected)
                {
                    _logger.Log(LogLevel.Information, $"Publisher {sourceConfiguration.Host}:{sourceConfiguration.Port} disconnected. Reason: {args.Reason}");
                }
                else
                {
                    _logger.Log(LogLevel.Error, $"Connection to publisher {sourceConfiguration.Host}:{sourceConfiguration.Port} failed. Reason: {args.Reason}");
                }
            });

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

        private GraphiteClient _targetClient;
        private object _lockObject = new object();


        private GraphiteClient GetTargetClient(Endpoint targetConfiguration)
        {
            lock (_lockObject)
            {
                if (_targetClient != null) return _targetClient;

                _targetClient = new GraphiteClient(targetConfiguration.Host, new PlaintextGraphiteFormatter())
                {
                    HttpApiPort = (ushort)targetConfiguration.Port
                };
            }

            return _targetClient;
        }

        
        private Datapoint CreateDatapointFromMessage(MqttApplicationMessage message)
        {
            var payload = System.Text.Encoding.UTF8.GetString(message.Payload);
            if (!double.TryParse(payload, NumberStyles.AllowDecimalPoint, new NumberFormatInfo(), out double value))
            {
                if (payload.Length == 1)
                {
                    value = payload[0];
                }
                else
                {
                    return MqttClientExtension.EmptyDatapoint;
                }
            }
            return new Datapoint(message.Topic.Replace('/', '.'), value, DateTime.UtcNow);
        }
    }
}
