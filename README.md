# Azure Functions Demo
This project is a self-contained deployment of a solution for integrating a set of records with any number of external dependencies. The basic design of the solution is to use a WebJob to get batches of records from a DB and put them on a Service Bus Message Queue. Then, Azure Functions will process the queue using input and output bindings that will pipe the record from one step of an overall algorithm to the next.

The requirements for this solution are to:
- demonstrate performant scale-out without duplication of messaging
- utilize Service Bus, Azure Functions, and WebJobs to build microservices
- explain why Azure Functions should be built to process single units of work on single records.

To run the demo, please take the following steps in order:
1. Deploy the resource group project to your Azure subscription. The deployment wizard will prompt you for a Resource Group name, and a set of connection strings. You can leave the connection strings blank the first time you deploy. You'll do it a second time and enter the values for these params later.
2. Deploy the DB project. It will migrate schema changes to support the project. The project uses a flag on the record to manage the batch.
3. Run an update query to set the Notification Status to 1.

At this point the project should run locally. Start the Function App project and then start the WebJob. 
You should see the WebJob process the messages onto the queue, and then see the Function App pick them up and process them. If you want to let this run on Azure, publish the Function App and the WebJob to Azure using the supplied publish profiles. 
Fun fact: Resource Group projects in Visual Studio cannot deploy .Net Core projects.

The included YAML pipeline will deploy the whole solution to Azure as well. 
It utilizes a variable groups, so here's the complete list of expected variables.
The Key Vault Linked secrets means that you must deploy the Key Vault (ideally the whole) template before you pull these secrets as variables.
Make sure to 'Authorize' your Service Connection to Key Vault. 
This will add an access policy with Get and List permission on Secrets. 

### Demo-Dev

Name|Value
-|-
AppName|[Your App Name]
azureResourceManagerConnection|[The name of your Service Connection]
Environment|[Environment Name]
location|[A Location]
my_IP_address|[Your IP Address]
resourceGroupName|[Your Resource Group Name]
ServiceConnectionId|[The Object ID of the Enterprise App]
SQLAdminPassword|********
SQLAdminUser|********
subscriptionId|[Your subscription ID]
UserObjectId|[Object ID of your SPN]

### Demo-Dev-KV

Secret Name|Status|Expiration Date
-|-|-
TwilioPhone|Enabled|Never
TwilioSID|Enabled|Never
TwilioToken|Enabled|Never
ConnectionStrings--AzureWebJobsStorage|Enabled|Never
ConnectionStrings--ServiceBusConnection|Enabled|Never
ConnectionStrings--SQLDBConnString|Enabled|Never
ConnectionStrings--StorageConnString|Enabled|Never
