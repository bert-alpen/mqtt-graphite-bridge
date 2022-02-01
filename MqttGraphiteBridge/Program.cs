using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

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
                    .Configure<WorkerConfiguration>(hostContext.Configuration.GetSection("Worker"))
                    .AddTransient<IMqttClientFactory, MqttClientFactory>()
                    .AddTransient<IGraphiteClientFactory, GraphiteClientFactory>();
            })
            .ConfigureHostConfiguration(configHost =>
            {
            });
    }
}