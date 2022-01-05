using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Adapter;

namespace MqttGraphiteBridge
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WorkerConfiguration _config;
        private readonly IHostApplicationLifetime _lifetime;

        public Worker(ILogger<Worker> logger, IOptions<WorkerConfiguration> options, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _config = options.Value;
            _lifetime = lifetime;
        }

        private IMqttClient CreateSourceClient(Endpoint sourceConfiguration)
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
        private IMqttClientOptions CreateSourceOptions(Endpoint sourceConfiguration, string clientId)
        {
            return new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTcpServer(sourceConfiguration.Host, sourceConfiguration.Port)
                .WithCredentials(sourceConfiguration.UserName, sourceConfiguration.Password)
                .WithCleanSession()
                //.WithCommunicationTimeout(new TimeSpan(0, 0, 5))
                .Build();
        }

        private async Task<MqttClientConnectResultCode> ConnectSourceAsync(IMqttClient client, IMqttClientOptions sourceOptions,
            CancellationToken cancellationToken)
        {
            var resultCode = MqttClientConnectResultCode.UnspecifiedError;
            try
            {
                var result = await client.ConnectAsync(sourceOptions, cancellationToken);
                resultCode = result.ResultCode;
            }
            catch (MqttConnectingFailedException e)
            {
                _logger.LogError($"Connection to publisher failed. Reason: {e.ResultCode}");
            }

            return resultCode;
        }

        private async void SubscribeToTopic(IMqttClient client, string topic)
        {
            var sr = await client.SubscribeAsync(topic);
            _logger.Log(LogLevel.Information, "Subscribed");
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                using (var mqttClient = CreateSourceClient(_config.Source))
                {
                    var options = CreateSourceOptions(_config.Source, _config.ClientId);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var connectionResult = await ConnectSourceAsync(mqttClient, options, stoppingToken);

                        if (connectionResult == MqttClientConnectResultCode.Success)
                        {
                            SubscribeToTopic(mqttClient, _config.Source.Topic);
                        }
                        else
                        {
                            // If error is recoverable re-try to connect
                            if (!ConnectionFailureIsRecoverable(connectionResult))
                            {
                                _logger.LogError($"Unrecoverable error connecting to publisher. Terminating.");
                                _lifetime.StopApplication();
                                return;
                            }
                        }

                        await Task.Delay(5000, stoppingToken);
                    }
                }
            }
        }

        private bool ConnectionFailureIsRecoverable(MqttClientConnectResultCode resultCode)
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
