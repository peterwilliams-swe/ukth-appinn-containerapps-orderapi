# Challenge 3: Solution

## Solution steps
We will perform a controlled rollout of the new version and split the incoming network traffic so that only 20% of requests will be sent to the new version of the application.

We'll deploy the first version of the application to Azure and invoke some APIs to test the application. We will then use _Log Analytics_ to validate that traffic split is working.

### Add traffic split to the HTTP API app by changing existing Bicep template
To implement the traffic split, in [v3_template.bicep](v3_template.bicep) add the traffic section on your _httpapi_ app and save the file.

```bicep
  ingress: {
        external: true
        targetPort: 80
        traffic: [
          {
            revisionName: 'httpapi--${ContainerApps_HttpApi_CurrentRevisionName}'
            weight: 80
          }
          {
            latestRevision: true
            weight: 20
          }
        ]
      }
```

Effectively, we're asking for 80% of traffic to be sent to the current version (revision) of the application and 20% to be sent to the new version that's about to be deployed.

### Deploy updated Bicep template
Once again, let's repeat the deployment command from earlier, now using version 2 of the _HTTP API_ application and with traffic splitting configured


<details>
  <summary>Bash</summary>

```bash
# Deploy Bicep template.
az deployment group create \
  -g $resourceGroup \
  --template-file v3_template.bicep \
  --parameters @v3_parametersbicep.json \
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
New-AzResourceGroupDeployment -ResourceGroupName $resourceGroup -Name 'v3_deployment' -TemplateFile .\v3_template.bicep -TemplateParameterFile .\v3_parametersbicep.json -Location $location -ContainerApps_Environment_Name $containerAppEnv -LogAnalytics_Workspace_Name $logAnalytics -AppInsights_Name $appInsights
```

  </summary>
</details>
<br>

### Add orders via HTTP API

With the third iteration of our applications deployed, let's try and send another order.



<details>
  <summary>Bash</summary>

```bash
curl -X POST $dataURL?message=item3
```

And let's check the Store application again to see if the messages have been received
  
```bash
curl $storeURL | jq

```
```json
[
   {
    "id": "b222d3fd-9776-4631-9f1d-5038055e1541",
    "message": "fa7c4a50-a711-48d5-8d7c-b9a9e9b9056e"
  },
  {
    "id": "807fd951-7213-4fd7-8a6f-df3a8e064ed9",
    "message": "f5a52f7a-67db-4ada-bdab-baa8189af700 -- item3"
  },
]
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
Invoke-RestMethod  "$($dataURL)?message=item3" -Method Post
```

And let's check the Store application again to see if the messages have been received

```PowerShell
Invoke-RestMethod $storeUrl
```
```
id                                   message
--                                   -------
a62d0fa5-26dd-449a-8c16-2e897c6ac4c1 9b4d6594-0c06-476f-81dd-1c9a7120d60b
a2be1546-7290-49df-9f1b-9dd567b7ce3b f5a52f7a-67db-4ada-bdab-baa8189af700--item3
```
</summary>
</details>
<br>


> [!NOTE] 
> The traffic split is 80/20 (80% old api, 20 % new api), so you might need to send a few messages before it hits our new revision of httpapi and appends the provided string to the message.

That's looking better. We can still see the original message, but we can also now see our "item3" message with the date and time appended to it.

We configured traffic splitting, so let's see that in action. First we will need to send multiple messages to the application. We can use the load testing tool _hey_ to do that.

<details>
  <summary>Bash</summary>


```bash
hey -m POST -n 25 -c 1 $dataURL?message=hello

# Verify orders in StoreApp
curl $storeURL | jq
```


  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
hey -m POST -n 25 -c 1 "$($dataURL)?message=hello"

# Verify orders in StoreApp
Invoke-RestMethod $storeURL
```

  </summary>
</details>
<br>

### Verify that traffic is distributed between Container App revisions 
Let's check the application logs for the _Queue Reader_ application

```kusto
ContainerAppConsoleLogs_CL
| where ContainerAppName_s has "queuereader" and ContainerName_s has "queuereader"
| where Log_s has "Message"
| project TimeGenerated, Log_s
| order by TimeGenerated desc
```

Looking through those logs, you should see a mix of messages, with some containing "hello" and others still containing a GUID. It won't be exact, but roughly one out of every five messages should contain "hello".

So, is our app ready for primetime now? Let's change things so that the new app is now receiving all of the traffic, plus we'll also setup some scaling rules. This will allow the container apps to scale up when things are busy, and scale to zero when things are quiet.

That will be done as part of [Challenge 4](challenge4.md)

## The challenges

- [Challenge 1: Setup the environment](challenge1.md)
- [Challenge 2: Deploy Container Apps Environment and troubleshoot Container Apps](challenge2.md)
- [Challenge 3: Split traffic for controlled rollout](challenge3.md)
- [Challenge 4: Scale Container Apps](challenge4.md)
- [Challenge 5: Configure CI/CD for Container Apps](challenge5.md)
- [Challenge 6: Protect Container App with API Management](challenge6.md)
- [Challenge 7: Enable Container App authentication](challenge7.md)
