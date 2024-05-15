# Challenge 2: Solution

## Solution steps
We'll deploy the first version of the application to Azure and invoke some APIs to test the application. We will then use _Log Analytics_ to troubleshoot the application.

### Deploy a Container Apps environment with related resources
We'll deploy an initial version of the application to Azure. Review the [V1 Bicep template](v1_template.bicep) that contains IaC definitions for Azure Container Apps Environment and other related services such as Log Analytics and a Storage account for the queue. Notice the individual container app resources. 


Let's start by setting some variables that we will use for creating Azure resources in this lab.

Use the `name` and `resourceGroup`  variables you created in [challenge 1](challenge1.md). 

Make sure you navigate to the root folder of the lab files in your shell (example _c:\repos\mylab\ukth-appinn-containerapps-orderapi_)

<details>
  <summary>Bash</summary>

```bash
# Reuse the random name created in previous challenge
# name = should be created in challenge 1

# Set variables for the rest of the lab
resourceGroup=${name}-rg
location=northeurope
containerAppEnv=${name}-env
logAnalytics=${name}-la
appInsights=${name}-ai
acr=${name}acr

# Deploy Bicep template.
az deployment group create \
  -g $resourceGroup \
  --template-file v1_template.bicep \
  --parameters @v1_parametersbicep.json \
  --parameters \
    ContainerApps_Environment_Name=$containerAppEnv \
    LogAnalytics_Workspace_Name=$logAnalytics \
    AppInsights_Name=$appInsights \
    Container_Registry_Name=$acr \
    Location=$location
```

  </summary>
</details>

> [!NOTE]
> There might be some warnings of unused variables and syntax during the deployment. You can safely ignore those warnings for now.

<details>
  <summary>PowerShell</summary>

```PowerShell
# Reuse the random name created in previous challenge
# $name = should be created in challenge 1

# Set variables for the rest of the lab
$resourceGroup="$name-rg"
$location="northeurope"
$containerAppEnv="$name-env"
$logAnalytics="$name-la"
$appInsights="$name-ai"
$acr="$($name)acr"

New-AzResourceGroupDeployment -ResourceGroupName $resourceGroup -Name 'v1_deployment' -TemplateFile .\v1_template.bicep -TemplateParameterFile .\v1_parametersbicep.json -Location $location -ContainerApps_Environment_Name $containerAppEnv -LogAnalytics_Workspace_Name $logAnalytics -AppInsights_Name $appInsights -Container_Registry_Name $acr
```

  </summary>
</details>
<br>

The deployment typically takes around 3 to 5 minutes to complete.

Once deployment has completed, verify the resources created by navigating to [Azure Portal](https://portal.azure.com). The following resources should be created. 

![](images/resources.png)
> [!NOTE]
> The resource name prefix will differ in your environment.
>

Select the Container Apps Environment and review the apps listed in the Apps blade

![](images/containerapps.png)


### Test APIs deployed as Container Apps
Now the application is deployed, let's verify that it works correctly. First determine the URL we'll use to access the store application and save that in a variable for convenience

<details>
  <summary>Bash</summary>

```bash
storeURL=https://storeapp.$(az containerapp env show -g $resourceGroup -n $containerAppEnv --query 'properties.defaultDomain' -o tsv)/store
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
$storeURL="https://storeapp.$((Get-AzContainerAppManagedEnv -ResourceGroupName $resourceGroup -EnvName $containerAppEnv).DefaultDomain)/store"
```

  </summary>
</details>
<br>

Let's see what happens if we call the URL

<details>
  <summary>Bash</summary>
  
```bash
curl $storeURL
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
Invoke-RestMethod $storeUrl
```

  </summary>
</details>
<br>


The response you should see is `[]` which means no data was returned. Either there has been no orders submitted or something's not working correctly.

Try adding a new order to the order data API and verify that it is stored correctly. First grab the order data API URL using a similar approach as in previous step.

<details>
  <summary>Bash</summary>
  
```bash
dataURL=https://httpapi.$(az containerapp env show -g $resourceGroup -n $containerAppEnv --query 'properties.defaultDomain' -o tsv)/data
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
$dataURL="https://httpapi.$((Get-AzContainerAppManagedEnv -ResourceGroupName $resourceGroup -EnvName $containerAppEnv).DefaultDomain)/data"
```

  </summary>
</details>
<br>

Add a new order test item using a HTTP Post

<details>
  <summary>Bash</summary>
  
```bash
curl -X POST $dataURL?message=item1
```
Verify that the store API returns the order

```bash
curl $storeURL
```

Still no orders are returned. 

Finally, check the queue length using the data API
```bash
curl $dataURL
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
Invoke-RestMethod "$($dataURL)?message=item1" -Method Post
```
Verify that the store API returns the order

```PowerShell
Invoke-RestMethod $storeURL
```

Still no orders are returned. 

Finally, check the queue length using the data API
```PowerShell

Invoke-RestMethod $dataURL
```

  </summary>
</details>
<br>


You should see the following output indicating that the queue is not read correctly.
> `Queue 'demoqueue' has 1 message`

Let's do some troubleshooting.

### Troubleshoot and redeploy application
ContainerApps integrates with _Application Insights_ and _Log Analytics_. In the Azure Portal, go to the Log Analytics workspace in the resource group we're using for this lab and run the following query to view the logs for the _queuereader_ application.

```kusto
ContainerAppConsoleLogs_CL
| where ContainerAppName_s has "queuereader" and ContainerName_s has "queuereader"
| top 100 by TimeGenerated
```
> If you don't see any results from the query you need to wait a couple of minutes for logs to be populated.


You should see a some log entries that will likely contain the same information about the error. Drill down on one of them to reveal more. You should see something like the following:
![](images/loganalytics-queue-error.png)

> "Log_s": "      Queue 'foo' does not exist. Waiting..",

Looks like we have configured the wrong name for the queue. 

Go through the [V1 Bicep template](v1_template.bicep) and find where the wrong queue configuration is located.
You will find it in the _queuereader_ container app configuration section.

```bicep
        name: 'queuereader'
          env: [
            {
              name: 'QueueName'
              value: 'foo'
            }
            {
              name: 'QueueConnectionString'
              secretRef: 'queueconnection'
            }
            {
              name: 'TargetApp'
              value: 'storeapp'
            }
```

> **Note**<br>
> The needed changes to the Bicep code is already made for you in [V2 Bicep template](v2_template.bicep).
> You don't need to update any template code at this stage.

Go ahead and deploy a new version of the solution by repeating the same command from earlier but with the version 2 of the configuration


<details>
  <summary>Bash</summary>

```bash
# Deploy Bicep template.
az deployment group create \
  -g $resourceGroup \
  --template-file v2_template.bicep \
  --parameters @v2_parametersbicep.json \
  --parameters \
    ContainerApps_Environment_Name=$containerAppEnv \
    LogAnalytics_Workspace_Name=$logAnalytics \
    AppInsights_Name=$appInsights \
    Location=$location
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
New-AzResourceGroupDeployment -ResourceGroupName $resourceGroup -Name 'v2_deployment' -TemplateFile .\v2_template.bicep -TemplateParameterFile .\v2_parametersbicep.json -Location $location -ContainerApps_Environment_Name $containerAppEnv -LogAnalytics_Workspace_Name $logAnalytics -AppInsights_Name $appInsights
```

  </summary>
</details>
<br>

Let's see what happens when we access the queue application using the data URL

<details>
  <summary>Bash</summary>

> As before, you can type `echo $dataURL` to get the URL of the HTTP API and then open it in a browser if you prefer

```bash
curl $dataURL

```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
Invoke-RestMethod $dataUrl
```

  </summary>
</details>
<br>

The result tells us that `demoqueue` has no messages:

> `Queue 'demoqueue' has 0 messages`

This indicates that the messages are now processed. Now add another test message.

<details>
  <summary>Bash</summary>
  
```bash
curl -X POST $dataURL?message=item2
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
Invoke-RestMethod "$($dataURL)?message=item2" -Method Post
```


  </summary>
</details>
<br>



Ok, let's check our Store URL and see what happens this time

<details>
  <summary>Bash</summary>
  
```bash
curl $storeURL

```
> `[{"id":"a85b038a-a01f-4f25-b468-238d0c8a3676","message":"24a1f5ed-2407-4f9d-a6f9-5664436f1c28"},{"id":"f2b4c93a-63e5-4a4d-8a66-1fa4d4b958fe","message":"5940cf24-8c55-4b38-938a-10d9351d5d2b"}]`
  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
Invoke-RestMethod $storeUrl
```

```
id                                   message
--                                   -------
a62d0fa5-26dd-449a-8c16-2e897c6ac4c1 9b4d6594-0c06-476f-81dd-1c9a7120d60b
a2be1546-7290-49df-9f1b-9dd567b7ce3b f5a52f7a-67db-4ada-bdab-baa8189af700
```

  </summary>
</details>
<br>


Ok, that's some progress but not the messages we sent in the query string. 

Let's take a look at the application code of the _HTTP API_:

[Program.cs](httapiapp\Program.cs)
```c#
app.MapPost("/data", async (MessageQueueClient client, [FromQuery]string message) =>
{
    var messageSent = await client.SendMessage(message);
    return messageSent ? 
        Results.Ok() : 
        new UnavailableResult();
})
```
Here we can see that the api sends the query parameter to the message client. But on closer inspection the we can see that the method on the client does not use the message.

[MessageQueueClient.cs](httpapiapp\MessageQueueClient.cs)

```c#
...
public async Task<bool> SendMessage(string message) => await SendMessageToQueue(Guid.NewGuid().ToString());
...
```

It looks like the code is set to send a GUID, not the message itself. Must have been something the developer left in to test things out. The correct code should look like this:

[MessageQueueClient.cs](httpapiapp\MessageQueueClient.cs) (version 2)

```c#
...
public async Task<bool> SendMessage(string message) => await SendMessageToQueue($"{Guid.NewGuid()}--{message}");
...
```
After we talked to the developer, we discovered that this has already been fixed and it has been pushed as an *v2* version of the *httpapi*. 

> [!NOTE]
> You don't need to do the code change - it has already been done by a developer.

But maybe we should be cautious and make sure this new change is working as expected and therefore perform a controlled rollout of the new version so only a subset of the incoming requests hit the new version.

That will be done as part of [Challenge 3](challenge3.md)

## The challenges

- [Challenge 1: Setup the environment](challenge1.md)
- [Challenge 2: Deploy Container Apps Environment and troubleshoot Container Apps](challenge2.md)
- [Challenge 3: Split traffic for controlled rollout](challenge3.md)
- [Challenge 4: Scale Container Apps](challenge4.md)
- [Challenge 5: Configure CI/CD for Container Apps](challenge5.md)
- [Challenge 6: Protect Container App with API Management](challenge6.md)
- [Challenge 7: Enable Container App authentication](challenge7.md)
