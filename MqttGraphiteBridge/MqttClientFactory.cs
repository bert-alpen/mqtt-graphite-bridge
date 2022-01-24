using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;

namespace MqttGraphiteBridge
{
    public class MqttClientFactory
    {
        private readonly ILogger<Worker> _logger;
        public MqttClientFactory(ILogger<Worker> logger)
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
    }

    public static class MqttClientExtension
    {
        public static async Task<MqttClientConnectResultCode> ConnectSourceAsync(
            this IMqttClient client, 
            IMqttClientOptions sourceOptions,
            CancellationToken cancellationToken,
            ILogger logger)
        {
            try
            {
                var result = await client.ConnectAsync(sourceOptions, cancellationToken);
                return result.ResultCode;
            }
            catch (MqttConnectingFailedException e)
            {
                logger?.LogError($"Connection to publisher failed. Reason: {e.ResultCode}");
                return e.ResultCode;
            }
            catch (Exception e)
            {
                logger?.LogError($"Connection to publisher failed with exception: {e}");
                return MqttClientConnectResultCode.UnspecifiedError;
            }
        }

        public static async void SubscribeToTopicAsync(this IMqttClient client, string topic, ILogger logger)
        {
            var sr = await client.SubscribeAsync(topic);
            logger?.Log(LogLevel.Information, "Subscribed");
        }

        public static bool ConnectionFailureIsRecoverable(this IMqttClient client, MqttClientConnectResultCode resultCode)
        {
            if (resultCode == MqttClientConnectResultCode.Success)
            {
                throw new ArgumentOutOfRangeException(nameof(resultCode), "Status code 'Success' is not a connection failure");
            }
            switch (resultCode)
            {
                case MqttClientConnectResultCode.ServerUnavailable:
                case MqttClientConnectResultCode.ServerBusy:
                {
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }
    }
}
