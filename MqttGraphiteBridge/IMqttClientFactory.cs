using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace MqttGraphiteBridge
{
    public interface IMqttClientFactory
    {
        IMqttClient CreateSourceClient(Endpoint sourceConfiguration, Endpoint targetConfiguration);
        IMqttClientOptions CreateSourceOptions(Endpoint sourceConfiguration, string clientId);
    }
}