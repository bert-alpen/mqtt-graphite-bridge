using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Client.Connecting;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Receiving;

namespace MqttGraphiteBridge
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WorkerConfiguration _config;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IMqttClientFactory _mqttClientFactory;
        private readonly IGraphiteClientFactory _graphiteClientFactory;

        public Worker(
            ILogger<Worker> logger,
            IOptions<WorkerConfiguration> options,
            IHostApplicationLifetime lifetime,
            IMqttClientFactory mqttClientFactory,
            IGraphiteClientFactory graphiteClientFactory)
        {
            _logger = logger;
            _config = options.Value;
            _lifetime = lifetime;
            _mqttClientFactory = mqttClientFactory;
            _graphiteClientFactory = graphiteClientFactory;
            sourceConnectedHandler = new MqttClientConnectedHandlerDelegate(args =>
            {
                _logger.Log(LogLevel.Information, $"Publisher  {_config.Source.Host}:{_config.Source.Port} Connected");
            });
            messageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(args =>
            {
                _logger.Log(LogLevel.Information, $"Message received for topic {args.ApplicationMessage.Topic}: {System.Text.Encoding.UTF8.GetString(args.ApplicationMessage.Payload)}");

                var target = _graphiteClientFactory.GetTargetClient(_config.Target);

                var dataPoint = _graphiteClientFactory.CreateDatapointFromMessage(args.ApplicationMessage);
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
            sourceDisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(args =>
            {
                if (args.ClientWasConnected)
                {
                    _logger.Log(LogLevel.Information, $"Publisher {_config.Source.Host}:{_config.Source.Port} disconnected. Reason: {args.Reason}");
                }
                else
                {
                    _logger.Log(LogLevel.Error, $"Connection to publisher {_config.Source.Host}:{_config.Source.Port} failed. Reason: {args.Reason}");
                }
            });
        }

        private MqttClientConnectedHandlerDelegate sourceConnectedHandler;
        private MqttApplicationMessageReceivedHandlerDelegate messageReceivedHandler;
        private MqttClientDisconnectedHandlerDelegate sourceDisconnectedHandler;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                using (var mqttClient = _mqttClientFactory.CreateSourceClient(
                    _config.Source,
                    _config.Target,
                    sourceConnectedHandler,
                    messageReceivedHandler,
                    sourceDisconnectedHandler))
                {
                    var options = _mqttClientFactory.CreateSourceOptions(_config.Source, _config.ClientId);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        if (mqttClient.IsConnected)
                        {
                            await Task.Delay(5000, stoppingToken);
                            continue;
                        }

                        var connectionResult = await mqttClient.ConnectSourceAsync(options, stoppingToken, _logger);

                        if (connectionResult == MqttClientConnectResultCode.Success)
                        {
                            mqttClient.SubscribeToTopicAsync(_config.Source.Topic, _logger);
                        }
                        else
                        {
                            // If error is recoverable re-try to connect
                            if (!mqttClient.ConnectionFailureIsRecoverable(connectionResult))
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
    }
}
