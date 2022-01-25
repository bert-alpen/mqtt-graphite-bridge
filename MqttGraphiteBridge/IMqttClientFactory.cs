using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace MqttGraphiteBridge
{
    public interface IMqttClientFactory
    {
        IMqttClient CreateSourceClient(Endpoint sourceConfiguration);
        IMqttClientOptions CreateSourceOptions(Endpoint sourceConfiguration, string clientId);
    }
}