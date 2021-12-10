using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace mqtt_graphite_bridge
{

    public class ConfigureWorkerOptions : IConfigureOptions<WorkerConfiguration>
    {
        private IConfiguration _configuration;
        public ConfigureWorkerOptions(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void Configure(WorkerConfiguration options)
        {
            throw new NotImplementedException();
        }
    }

    public class WorkerConfiguration
    {
        public WorkerConfiguration()
        {
            Source = new Endpoint();
            Target = new Endpoint();
        }

        public Endpoint Source { get; set; }
        public Endpoint Target { get; set; }
    }

    public class Endpoint
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Topic { get; set; }
    }
}