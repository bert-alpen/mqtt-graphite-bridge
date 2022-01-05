using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MqttGraphiteBridge
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .UseSystemd()
            .ConfigureServices((hostContext, services) =>
            {
                services
                    .AddHostedService<Worker>()
                    .AddOptions()
                    .Configure<WorkerConfiguration>(hostContext.Configuration.GetSection("Worker"));
            })
            .ConfigureHostConfiguration(configHost =>
            {
            });
    }
}