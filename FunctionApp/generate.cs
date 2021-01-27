using Demo.DAL;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Data.Entity.Core.EntityClient;
using System.Text;
using System.Text.RegularExpressions;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
namespace Demo.FunctionApp
{
    public static class Generate
    {
        [FunctionName("Generate")]
        [return: ServiceBus("notify", Connection = "ConnectionStrings:ServiceBusConnection")]
        public static Message Run([ServiceBusTrigger("generate", Connection = "ConnectionStrings:ServiceBusConnection")] Message myQueueItem, 
            ILogger log, 
            ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(context.FunctionAppDirectory)
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .Build();

            var customerMessageBody = JsonConvert.DeserializeObject<Customer>(Encoding.UTF8.GetString(myQueueItem.Body));

            try
            {
                // Generate a code for the message.
                System.Threading.Thread.Sleep(500);
                customerMessageBody.NotificationCode = Guid.NewGuid().ToString();

                // Put a new message on the next queue
                var nextQueueItem = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(customerMessageBody))) { MessageId = customerMessageBody.Phone };
                return nextQueueItem;
            }
            catch (Exception ex)
            {
                NotifyEntities db = new NotifyEntities(NotifyEntities.GetConnectionString(config["ConnectionStrings:SQLDBConnString"]));
                var customer = db.Customers.Find(customerMessageBody.CustomerID);                
                customer.NotificationStatus = 3; // error
                //customer.NotificationError = ex.Message;
                db.SaveChanges();

                log.LogError(ex, ex.Message);
                throw;
            }
        }
    }
}
