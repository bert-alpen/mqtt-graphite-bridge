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
        private readonly IMqttClientFactory _mqttClientFactory;

        public Worker(ILogger<Worker> logger, IOptions<WorkerConfiguration> options, IHostApplicationLifetime lifetime, IMqttClientFactory mqttClientFactory)
        {
            _logger = logger;
            _config = options.Value;
            _lifetime = lifetime;
            _mqttClientFactory = mqttClientFactory;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                using (var mqttClient = _mqttClientFactory.CreateSourceClient(_config.Source))
                {
                    var options = _mqttClientFactory.CreateSourceOptions(_config.Source, _config.ClientId);

                    while (!stoppingToken.IsCancellationRequested)
                    {
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
