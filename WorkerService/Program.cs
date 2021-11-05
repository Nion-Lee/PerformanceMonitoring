using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using WorkerService.Workers;

namespace WorkerService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<PerformanceCounter>();
                services.AddHostedService<SocketServer>();
                services.AddLogging(builder => builder.AddConsole());
            })
            .ConfigureHostConfiguration(configHost =>
            {
                configHost.SetBasePath(Directory.GetCurrentDirectory());
                configHost.AddJsonFile("appsettings.json");

                var configRoot = configHost.Build();
                configRoot.GetSection("Socket").Get<SocketConfig>();
                configRoot.GetSection("Threshold").Get<Threshold>();
            });
    }
}