using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Demo.DAL;
using Microsoft.Extensions.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Configuration;
using Azure.Identity;

namespace Demo.FunctionApp
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            string accountSid = Environment.GetEnvironmentVariable("TwilioSID");
            string authToken = Environment.GetEnvironmentVariable("TwilioToken");

            NotifyEntities db = new NotifyEntities(config["ConnectionStrings:NotifyEntities"]);
            var customer = db.Customers.Find(1);

            return new OkObjectResult("Done");
        }
    }
}
