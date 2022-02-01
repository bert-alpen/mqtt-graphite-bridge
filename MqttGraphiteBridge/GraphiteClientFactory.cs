using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ahd.Graphite;
using MQTTnet;

namespace MqttGraphiteBridge
{
    public class GraphiteClientFactory : IGraphiteClientFactory
    {
        private GraphiteClient _targetClient;
        private object _lockObject = new object();

        public GraphiteClient GetTargetClient(Endpoint targetConfiguration)
        {
            lock (_lockObject)
            {
                if (_targetClient != null) return _targetClient;

                _targetClient = new GraphiteClient(targetConfiguration.Host, new PlaintextGraphiteFormatter())
                {
                    HttpApiPort = (ushort)targetConfiguration.Port
                };
            }

            return _targetClient;
        }

        public Datapoint CreateDatapointFromMessage(MqttApplicationMessage message)
        {
            var payload = System.Text.Encoding.UTF8.GetString(message.Payload);
            if (!double.TryParse(payload, NumberStyles.AllowDecimalPoint, new NumberFormatInfo(), out double value))
            {
                if (payload.Length == 1)
                {
                    value = payload[0];
                }
                else
                {
                    return MqttClientExtension.EmptyDatapoint;
                }
            }
            return new Datapoint(message.Topic.Replace('/', '.'), value, DateTime.UtcNow);
        }
    }
}
