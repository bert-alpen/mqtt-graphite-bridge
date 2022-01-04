using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace MqttGraphiteBridge
{
    public class WorkerConfiguration
    {
        public WorkerConfiguration()
        {
        }

        public Endpoint Source =>
            new Endpoint()
            {
                Host = SourceHost,
                Port = SourcePort,
                UserName = SourceUserName,
                Password = SourcePassword,
                Topic = SourceTopic,
            };

        public Endpoint Target =>
            new Endpoint()
            {
                Host = TargetHost,
                Port = TargetPort,
                UserName = TargetUserName,
                Password = TargetPassword,
                Topic = TargetTopic,
            };
        public string SourceHost { get; set; }
        public int SourcePort { get; set; }
        public string SourceTopic { get; set; }
        public string SourceUserName { get; set; }
        public string SourcePassword { get; set; }
        public string TargetHost { get; set; }
        public int TargetPort { get; set; }
        public string TargetTopic { get; set; }
        public string TargetUserName { get; set; }
        public string TargetPassword { get; set; }
        public string ClientId { get; set; }
    }


}