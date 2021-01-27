using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WebJob
{
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        private static IConfiguration Configuration { get; set; }
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder();
            builder
                .ConfigureWebJobs(b =>
                {
                    b.AddAzureStorageCoreServices();
                    b.AddAzureStorage();
                    b.AddTimers();
                })
                .ConfigureLogging(logging =>
                {
                    string appInsightsKey = Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                    if (!string.IsNullOrEmpty(appInsightsKey))
                    {
                        // This uses the options callback to explicitly set the instrumentation key.
                        logging.AddApplicationInsights(appInsightsKey)
                               .SetMinimumLevel(LogLevel.Information);
                        logging.AddApplicationInsightsWebJobs(o => { o.InstrumentationKey = appInsightsKey; });
                    }
                })
                .ConfigureServices((context, s) => {
                    ConfigureServices(s);
                    s.BuildServiceProvider();

                });
            var tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;
            var host = builder.Build();
            using (host)
            {
                await host.RunAsync(ct);
                tokenSource.Dispose();
            }
                
        }
        private static void ConfigureServices(IServiceCollection services)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables() //this doesnt do anything useful notice im setting some env variables explicitly. 
                .Build();  //build it so you can use those config variables down below.

            services.AddSingleton(Configuration);
        }
    }
}
