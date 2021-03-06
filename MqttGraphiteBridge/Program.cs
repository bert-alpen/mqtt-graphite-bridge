using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MqttGraphiteBridge
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host
                .CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureAppConfiguration((hostingContext, configuration) =>
                {
                    // Configuration is only accepted from a json file.
                    // The path to the configuration file can be set as an environment variable. If that isn't set the default is appSettings.json
                    // in the current directory. If hostingContext.HostingEnvironment.EnvironmentName is set, an environment specific overload can be 
                    // added in the same directory.
                    var configFile = Environment.GetEnvironmentVariable("MQTT_GRAPHITE_BRIDGE_CONFIG_FILE") != null
                        ? Environment.GetEnvironmentVariable("MQTT_GRAPHITE_BRIDGE_CONFIG_FILE")
                        : "appSettings.json";

                    var fileInfo = new FileInfo(configFile);
                    if (!fileInfo.Exists) throw new Exception($"The configuration file could not be found: {fileInfo.FullName}");

                    var environmentConfigFile = GetEnvironmentSpecificConfigFileName(fileInfo, hostingContext.HostingEnvironment.EnvironmentName);

                    configuration.Sources.Clear();
                    configuration
                        .AddJsonFile(configFile, false, true)
                        .AddJsonFile(environmentConfigFile, true, true)
                        ;

                    configuration.Build();

                })
                .ConfigureServices((hostContext, services) =>
                {
                    services
                        .AddHostedService<Worker>()
                        .AddOptions()
                        .Configure<WorkerConfiguration>(hostContext.Configuration.GetSection("Worker"))
                        .AddTransient<IMqttClientFactory, MqttClientFactory>()
                        .AddTransient<IGraphiteClientFactory, GraphiteClientFactory>()
                        ;
                })
                ;

        private static string GetEnvironmentSpecificConfigFileName(FileInfo fileInfo, string environment)
        {
            var fileName = fileInfo.Name.Substring(0, fileInfo.Name.Length - fileInfo.Extension.Length);
            return Path.Combine(
                fileInfo.DirectoryName ?? Directory.GetCurrentDirectory(),
                $"{fileName}.{environment}{fileInfo.Extension}");
        }
    }
}