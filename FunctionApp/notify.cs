using Demo.DAL;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Data.Entity.Core.EntityClient;
using System.Text;
using Twilio;

namespace Demo.FunctionApp
{
    public static class Notify
    {
        [FunctionName("Notify")]
        public static void Run(
            [ServiceBusTrigger("notify", Connection = "ConnectionStrings:ServiceBusConnection")] 
            Message myQueueItem, 
            ILogger log, 
            ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(context.FunctionAppDirectory)
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .Build();

            var customerMessageBody = JsonConvert.DeserializeObject<Customer>(Encoding.UTF8.GetString(myQueueItem.Body));
            NotifyEntities db = new NotifyEntities(NotifyEntities.GetConnectionString(config["ConnectionStrings:SQLDBConnString"]));
            var customer = db.Customers.Find(customerMessageBody.CustomerID);

            try
            {
                System.Threading.Thread.Sleep(500);

                // The Twilio code works, but don't want to send out unintentional messages
                string accountSid = Environment.GetEnvironmentVariable("TwilioSID");
                string authToken = Environment.GetEnvironmentVariable("TwilioToken");

                TwilioClient.Init(accountSid, authToken);

                //var message = MessageResource.Create(
                //    body: @"Please sign up at http://myapp.somedomain.com/register/" + customer.NotificationCode,
                //    from: new Twilio.Types.PhoneNumber(Environment.GetEnvironmentVariable("TwilioPhone")),
                //    to: new Twilio.Types.PhoneNumber(customerMessageBody.Phone)
                //);

                //if (message.Status == MessageResource.StatusEnum.Delivered)
                {
                    customer.NotificationStatus = 4;
                }
            }
            catch (Exception ex)
            {
                customer.NotificationStatus = 3; // error
                // customer.NotificationError = ex.Message;
                
                log.LogError(ex, ex.Message);
                throw;
            }
            finally
            {
                db.SaveChanges();
            }
        }
    }
}
