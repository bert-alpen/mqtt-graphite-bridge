﻿using System;
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
        
        public IMqttClient CreateSourceClient(Endpoint sourceConfiguration, Endpoint targetConfiguration)
        {
            var mqttClient = new MqttFactory().CreateMqttClient();

            mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(args =>
            {
                _logger.Log(LogLevel.Information, $"Publisher  {sourceConfiguration.Host}:{sourceConfiguration.Port} Connected");
            });

            mqttClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(args =>
            {
                _logger.Log(LogLevel.Information, $"Message received for topic {args.ApplicationMessage.Topic}: {System.Text.Encoding.UTF8.GetString(args.ApplicationMessage.Payload)}");

                var target = GetTargetClient(targetConfiguration);

                var dataPoint = CreateDatapointFromMessage(args.ApplicationMessage);
                if (dataPoint.IsEmpty())
                {
                    _logger.Log(LogLevel.Debug, $"Could not extract value from payload. Skipping.");
                    return;
                }

                try
                {
                    target.Send(new[] { dataPoint });
                    _logger.Log(LogLevel.Debug, $"Data sent to target");
                }
                catch (Exception e)
                {
                    _logger.Log(LogLevel.Warning, "Target is not receiving data, discarding.");
                }

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
}
