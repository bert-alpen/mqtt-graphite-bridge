using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;

namespace MqttGraphiteBridge
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WorkerConfiguration _config;

        public Worker(ILogger<Worker> logger, IOptions<WorkerConfiguration> options)
        {
            _logger = logger;
            _config = options.Value;
        }

        private IMqttClient CreateSourceClient(Endpoint sourceConfiguration)
        {
            var mqttClient = new MqttFactory().CreateMqttClient();

            mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(args =>
            {
                _logger.Log(LogLevel.Information, $"Publisher  {sourceConfiguration.Host}:{sourceConfiguration.Port} Connected");

                var sr = mqttClient.SubscribeAsync(sourceConfiguration.Topic);
                _logger.Log(LogLevel.Information, "Subscribed");
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var mqttClient = CreateSourceClient(_config.Source))
                {
                    var options = CreateSourceOptions(_config.Source, _config.ClientId);
                    try
                    {
                        var connectionResult = await mqttClient.ConnectAsync(options, stoppingToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.ToString());
                        return;
                    }


                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
    }
}
