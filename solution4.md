# Challenge 4: Solution

## Solution steps
Our application is now verified using blue-green deployment as part of [challenge 3](challenge3.md). We are now ready for production and will only have the latest revision of our application deployed. In addition we will deploy new configuration with scaling configured. We will also add a simple dashboard for monitoring the messages flow.


### Examine scaling rules
Review scaling rules added to _HTTP API (httpapi)_ and _Queue Reader (queuereader)_ Container Apps by examining existing [Bicep template v4](v4_template.bicep)

```bicep
    ]
      scale: {
        minReplicas: 0
        maxReplicas: 5
        rules: [
          {
            name: 'myqueuerule'
            ...

    scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'httpscalingrule'
```

We are configuring the _Queue Reader_ app to scale from 0 to 5 replicas and _HTTP API_ to scale from 1 to 10 replicas. Also note the additional Container Apps _dashboardapp_ and _dashboardapi_  that are part of this template. They are used for observability and will visualize current order count in the queue and number of orders in our store.

### Deploy updated Bicep template
One additional time, we'll now deploy the new configuration with scaling configured. We will also add a simple dashboard for monitoring the messages flow.


<details>
  <summary>Bash</summary>

```bash
# Deploy Bicep template.
az deployment group create \
  -g $resourceGroup \
  --template-file v4_template.bicep \
  --parameters @v4_parametersbicep.json \
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
New-AzResourceGroupDeployment -ResourceGroupName $resourceGroup -Name 'v4_deployment' -TemplateFile .\v4_template.bicep -TemplateParameterFile .\v4_parametersbicep.json -Location $location -ContainerApps_Environment_Name $containerAppEnv -LogAnalytics_Workspace_Name $logAnalytics -AppInsights_Name $appInsights
```

  </summary>
</details>
<br>

### Review application logs

Let's check the number of orders in the queue

<details>
  <summary>Bash</summary>
  
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

As before, we can check the application log files in Log Analytics to see what messages are being received

```kusto
ContainerAppConsoleLogs_CL
| where ContainerAppName_s has "queuereader" and ContainerName_s has "queuereader"
| where Log_s has "Message"
| project TimeGenerated, Log_s
| order by TimeGenerated desc
```
### Run script to bulk add orders and monitor scaling
Now let's see scaling in action. To do this, we will generate a large amount of messages which should cause the applications to scale up to cope with the demand.

While the scaling script is running, you can also have this operations dashboard open to visually see the messages flowing through queue into the store. Get the dashboard URL and open in a browser.

<details>
  <summary>Bash</summary>

```bash
dashboardURL=https://dashboardapp.$(az containerapp env show -g $resourceGroup -n $containerAppEnv --query 'properties.defaultDomain' -o tsv)
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
$dashboardURL="https://dashboardapp.$((Get-AzContainerAppManagedEnv -ResourceGroupName $resourceGroup -EnvName $containerAppEnv).DefaultDomain)/"
```

  </summary>
</details>
<br>

To demonstrate this, a script that uses the `tmux` command is provided in the _scripts_ folder of this repository. Run the following commands:

```bash
cd scripts
./appwatch.sh $resourceGroup $dataURL
```

> [!NOTE]
> When running the dev container locally it is a risk that the appwatch script has wrong line-endings. Run the following command to fix the issue.

> ```bash
> sed -i -e 's/\r$//' appwatch.sh
> ```

This will split your terminal into four separate views.

* On the left, you will see the output from the _hey_ command. It's going to send 10,000 requests to the application, so there will be a short delay, around 20 to 30 seconds, whilst the requests are sent. Once the _hey_ command finishes, it should report its results.
* On the right at the top, you will see a list of the container app versions (revisions) that we've deployed. One of these will be the latest version that we just deployed. As _hey_ sends more and more messages, you should notice that one of these revisions of the app starts to increase its replica count
* Also on the right, in the middle, you should see the current count of messages in the queue. This will increase to 10,000 and then slowly decrease as the app works it way through the queue.

Once _hey_ has finished generating messages, the number of instances of the _HTTP API_ application should start to scale up and eventually max out at 10 replicas. After the number of messages in the queue reduces to zero, you should see the number of replicas scale down and return to 1.

> **Tip**<br> 
> To exit from tmux when you're finished, type `CTRL-b`, then `:` and then the command `kill-session`

That concludes the deployment of the application. In upcoming challenges we will look at improving the DevOps and security capabilities of our solution. Next up is CI/CD deployment as part of [Challenge 5](challenge5.md)

## The challenges

- [Challenge 1: Setup the environment](challenge1.md)
- [Challenge 2: Deploy Container Apps Environment and troubleshoot Container Apps](challenge2.md)
- [Challenge 3: Split traffic for controlled rollout](challenge3.md)
- [Challenge 4: Scale Container Apps](challenge4.md)
- [Challenge 5: Configure CI/CD for Container Apps](challenge5.md)
- [Challenge 6: Protect Container App with API Management](challenge6.md)
- [Challenge 7: Enable Container App authentication](challenge7.md)
