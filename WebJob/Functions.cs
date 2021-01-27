using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using Demo.DAL;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Infrastructure.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo.WebJob
{
    public class Functions
    {
        string queueName = "verify";
        IConfiguration _configuration;

        public Functions(IConfiguration config)
        {
            _configuration = config;
        }

        /// <summary>
        /// For more information about Service Bus messaging, please see https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="logger"></param>
        /// <returns>Task</returns>
        public async Task SendMessageBatchAsync([TimerTrigger(scheduleExpression: "0 * * * * *", RunOnStartup = true, UseMonitor = false)] 
            TimerInfo myTimer, 
            ILogger logger)
        {
            NotifyEntities db = new NotifyEntities(NotifyEntities.GetConnectionString(_configuration["ConnectionStrings:SQLDBConnString"]));
            ServiceBusClient sbClient = new ServiceBusClient(_configuration["ConnectionStrings:ServiceBusConnection"]);

            // create a sender for the queue 
            ServiceBusSender sender = sbClient.CreateSender(queueName);

            var batch = db.Customers.Where(c => c.NotificationStatus == 1)
                .Take(10)
                .ToList();

            // get the messages to be sent to the Service Bus queue
            Queue<ServiceBusMessage> messages = new Queue<ServiceBusMessage>();
            batch.ForEach(c => messages.Enqueue(new ServiceBusMessage(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new Customer
            {
                CompanyName = c.CompanyName,
                CustomerID = c.CustomerID,
                Phone = c.Phone,
                FirstName = c.FirstName,
                LastName = c.LastName,
                NotificationStatus = c.NotificationStatus
            })))
            { MessageId = c.Phone }));

            // total number of messages to be sent to the Service Bus queue
            int messageCount = messages.Count;

            // while all messages are not sent to the Service Bus queue
            while (messages.Count > 0)
            {
                // start a new batch 
                ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

                // add the first message to the batch
                if (messageBatch.TryAddMessage(messages.Peek()))
                {
                    // dequeue the message from the .NET queue once the message is added to the batch
                    messages.Dequeue();
                }
                else
                {
                    // if the first message can't fit, then it is too large for the batch
                    throw new Exception($"Message {messageCount - messages.Count} is too large and cannot be sent.");
                }

                // add as many messages as possible to the current batch
                while (messages.Count > 0 && messageBatch.TryAddMessage(messages.Peek()))
                {
                    // dequeue the message from the .NET queue as it has been added to the batch
                    messages.Dequeue();
                }

                // now, send the batch
                await sender.SendMessagesAsync(messageBatch);

                // if there are any remaining messages in the .NET queue, the while loop repeats 
            }

            logger.LogInformation($"Sent a batch of {messageCount} messages to the topic: {queueName}");
        }
    }
}