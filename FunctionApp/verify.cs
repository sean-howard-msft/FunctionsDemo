using Demo.DAL;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PhoneNumbers;
using System;
using System.Data.Entity.Core.EntityClient;
using System.Text;
using System.Text.RegularExpressions;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Verify.V2.Service;

namespace Demo.FunctionApp
{
    public static class Verify
    {
        [FunctionName("Verify")]
        [return: ServiceBus("generate", Connection = "ConnectionStrings:ServiceBusConnection")]
        public static Message Run(
            [ServiceBusTrigger("verify", Connection = "ConnectionStrings:ServiceBusConnection")] Message myQueueItem,
            ILogger log, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(context.FunctionAppDirectory)
               .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .Build();

            var customerMessageBody = JsonConvert.DeserializeObject<Customer>(Encoding.UTF8.GetString(myQueueItem.Body));

            try
            {
                System.Threading.Thread.Sleep(500);

                string accountSid = Environment.GetEnvironmentVariable("TwilioSID");
                string authToken = Environment.GetEnvironmentVariable("TwilioToken");

                TwilioClient.Init(accountSid, authToken);

                //var verification = VerificationResource.Create(
                //    to: customerMessageBody.Phone,
                //    channel: "sms",
                //    pathServiceSid: "VAXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX"
                //);

                PhoneNumberUtil _phoneUtil = PhoneNumberUtil.GetInstance();
                PhoneNumber phoneNumber = _phoneUtil.Parse(customerMessageBody.Phone, "US");
                customerMessageBody.Phone = _phoneUtil.Format(phoneNumber, PhoneNumberFormat.E164);

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
