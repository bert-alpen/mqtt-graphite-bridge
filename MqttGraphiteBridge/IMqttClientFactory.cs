using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;

namespace MqttGraphiteBridge
{
    public interface IMqttClientFactory
    {
        IMqttClient CreateSourceClient(
            Endpoint sourceConfiguration, 
            Endpoint targetConfiguration,
            MqttClientConnectedHandlerDelegate connectedHandler,
            MqttApplicationMessageReceivedHandlerDelegate messageHandler,
            MqttClientDisconnectedHandlerDelegate disconnectedHandler);
        IMqttClientOptions CreateSourceOptions(Endpoint sourceConfiguration, string clientId);
    }
}