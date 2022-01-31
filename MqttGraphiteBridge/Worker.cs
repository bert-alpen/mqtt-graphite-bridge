using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet.Client.Connecting;
using System.Threading;
using System.Threading.Tasks;

namespace MqttGraphiteBridge
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly WorkerConfiguration _config;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IMqttSourceFactory _mqttClientFactory;
        private readonly IGraphiteClientFactory _graphiteClientFactory;

        public Worker(
            ILogger<Worker> logger,
            IOptions<WorkerConfiguration> options,
            IHostApplicationLifetime lifetime,
            IMqttSourceFactory mqttClientFactory,
            IGraphiteClientFactory graphiteClientFactory)
        {
            _logger = logger;
            _config = options.Value;
            _lifetime = lifetime;
            _mqttClientFactory = mqttClientFactory;
            _graphiteClientFactory = graphiteClientFactory;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                using (var mqttClient = _mqttClientFactory.CreateSourceClient(_config.Source, _config.Target))
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
