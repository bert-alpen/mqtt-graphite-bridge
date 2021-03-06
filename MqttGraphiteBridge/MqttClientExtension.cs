using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ahd.Graphite;
using Microsoft.Extensions.Logging;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Exceptions;

namespace MqttGraphiteBridge
{
    public static class MqttClientExtension
    {
        public static async Task<MqttClientConnectResultCode> ConnectSourceAsync(
            this IMqttClient client,
            IMqttClientOptions sourceOptions,
            CancellationToken cancellationToken,
            ILogger logger)
        {
            try
            {
                var result = await client.ConnectAsync(sourceOptions, cancellationToken);
                return result.ResultCode;
            }
            catch (MqttConnectingFailedException e)
            {
                logger?.LogError($"Connection to publisher failed. Reason: {e.ResultCode}");
                return e.ResultCode;
            }
            catch (MqttProtocolViolationException e)
            {
                logger?.LogError($"Connection to publisher failed with protocol error. Exception: {e}");
                return MqttClientConnectResultCode.ProtocolError;
            }
            catch (OperationCanceledException e)
            {
                logger?.LogError($"Connection to publisher failed: Operation was cancelled. Exception: {e}");
                return MqttClientConnectResultCode.ServerUnavailable;
            }
            catch (MqttCommunicationTimedOutException e)
            {
                logger?.LogError($"Connection to publisher failed: Operation timed out. Exception: {e}");
                return MqttClientConnectResultCode.ServerUnavailable;
            }
            catch (InvalidOperationException e)
            {
                return e.Message.Contains("connect/disconnect") 
                    ? MqttClientConnectResultCode.ServerBusy 
                    : MqttClientConnectResultCode.UnspecifiedError;
            }
            catch (Exception e)
            {
                logger?.LogError($"Connection to publisher failed with exception: {e}");
                return MqttClientConnectResultCode.UnspecifiedError;
            }
        }

        public static async void SubscribeToTopicAsync(this IMqttClient client, string topic, ILogger logger)
        {
            var sr = await client.SubscribeAsync(topic);
            logger?.Log(LogLevel.Information, $"Subscribed to topic {topic}");
        }

        public static bool ConnectionFailureIsRecoverable(this IMqttClient client, MqttClientConnectResultCode resultCode)
        {
            if (resultCode == MqttClientConnectResultCode.Success)
            {
                throw new ArgumentOutOfRangeException(nameof(resultCode), "Status code 'Success' is not a connection failure");
            }
            switch (resultCode)
            {
                case MqttClientConnectResultCode.ServerUnavailable:
                case MqttClientConnectResultCode.ServerBusy:
                case MqttClientConnectResultCode.ConnectionRateExceeded:
                case MqttClientConnectResultCode.QuotaExceeded:
                {
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }

        public static Datapoint EmptyDatapoint => new Datapoint("", 0, 0);

        public static bool IsEmpty(this Datapoint datapoint)
        {
            return datapoint.UnixTimestamp <= 0;
        }
    }
}
