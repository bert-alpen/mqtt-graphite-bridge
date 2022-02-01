using ahd.Graphite;
using MQTTnet;

namespace MqttGraphiteBridge
{
    public interface IGraphiteClientFactory
    {
        GraphiteClient GetTargetClient(Endpoint targetConfiguration);
        Datapoint CreateDatapointFromMessage(MqttApplicationMessage message);
    }
}