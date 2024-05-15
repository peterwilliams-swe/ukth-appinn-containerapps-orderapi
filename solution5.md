# Challenge 5: Solution

## Solution steps
Until now we have worked with docker images that others have created, now we are going to do a code change on our own code base and push the changes to Container Apps using _GitHub Actions_.
We are now going to use a Azure CLI command to create a GitHub Action that builds the _Queue Reader_ C# project and pushes the image to _Azure Container Registry_ and deploys it to our Container App.

First, we need to create a service principal that can be used from the GitHub action to deploy the changes we are introducing.

> [!NOTE] 
> If you are not able to create a service principal in Microsoft Entra ID that is connected to your subscription, you will unfortunately not be able to do this part of the lab. You can also leverage a pre-created service principal. In many enterprises there is a separate procedure to order a service principal that you need to follow.



### Create a Microsoft Entra ID service principal
<details>
  <summary>Azure CLI using Bash</summary>

```bash
az ad sp create-for-rbac \
  --name <SERVICE_PRINCIPAL_NAME> \
  --role "contributor" \
  --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/<RESOURCE_GROUP_NAME> \
  --sdk-auth
```
The return value from this command is a JSON payload, which includes the service principal's `tenantId`, `clientId`, and `clientSecret`.
Set the variables in bash.

```bash
spClientid=[Replace with the clientId of the service principal]
spClientSecret=[Replace with the clientSecret of the service principal]
tenantid=[Replace with the tenantId of the service principal]
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
$sp = New-AzADServicePrincipal -DisplayName <SERVICE_PRINCIPAL_NAME>

New-AzRoleAssignment -ApplicationId $sp.ApplicationId -RoleDefinitionName 'Contributor' -ResourceGroupName <RESOURCE_GROUP_NAME>

$spClientId = $sp.ApplicationId
$spClientSecret = $sp.PasswordCredentials.SecretText
$tenantId = (Get-AzContext | Select-Object -ExpandProperty Tenant).Id
```

  </summary>
</details>
<br>

### Create a Personal Access Token (PAT) in GitHub
Now we need a GitHub _Personal Access Token (PAT)_ so we can authenticate against GitHub from Azure CLI.

Go to _github.com --> Settings --> Developer Settings --> Personal access tokens_ and click on _Generate new token_ button.
 
Password prompt might appear. Enter password.

In the _Note_ textbox enter a name for the PAT, such as _ca-pat_.
Give the PAT the following scopes: 
-	_repo (Full control of private repositories)_ 
-	_workflows (Update GitHub Action workflows)_

![pat](images/pat.png)

Click _Generate token_, copy the generated token and assign the variable. 

<details>
  <summary>Bash</summary>

```bash
ghToken=[Replace with the PAT]
```
Set the "repoUrl" variable, replace <OWNER> with the GitHub account name. 
```bash
repoUrl=https://github.com/<OWNER>/ukth-appinn-containerapps-orderapi
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
$ghToken=[Replace with the PAT]
```
Set the "repoUrl" variable, replace <OWNER> with the GitHub account name. 

```PowerShell
$repoUrl="https://github.com/<your repo url path>"

```

  </summary>
</details>
<br>

### Add a GitHub Actions workflow to your repository to deploy a container app
Now we need to get information about the Azure Container Registry that we created in the beginning.

<details>
  <summary>Bash</summary>


```bash
acrUrl=$(az acr show -n $acr -g $resourceGroup --query 'loginServer' -o tsv)
acrUsername=$(az acr show -n $acr -g $resourceGroup --query 'name' -o tsv)
acrSecret=$(az acr credential show -n $acr -g $resourceGroup --query passwords[0].value -o tsv)
```


  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
$acrUrl = Get-AzContainerRegistry -Name $acr -ResourceGroupName $resourceGroup | Select-Object -ExpandProperty LoginServer
$acrCreds = Get-AzContainerRegistryCredential -Name $acr -ResourceGroupName $resourceGroup
$acrUsername=$acrCreds.Username
$acrSecret=$acrCreds.Password
```

  </summary>
</details>
<br>

Now all the variables are set so we can run the Azure CLI command, make sure you are located at the root of the repo and run the following command.

<details>
  <summary>Azure CLI using Bash</summary>


```bash
az containerapp github-action add \
  --repo-url $repoUrl \
  --context-path "./queuereaderapp/Dockerfile" \
  --branch main \
  --name queuereader \
  --resource-group $resourceGroup \
  --registry-url $acrUrl \
  --registry-username $acrUsername \
  --registry-password $acrSecret \
  --service-principal-client-id $spClientid \
  --service-principal-client-secret $spClientSecret \
  --service-principal-tenant-id $tenantid \
  --token $ghToken

```


  </summary>
</details>

<details>
  <summary>Azure CLI using PowerShell</summary>

> **Note**<br>
> Whilst most Azure tasks can be done using native Azure PowerShell commands this is an exception. 
> Hence we will be using AZ CLI tool for this.
> If you havent logged in to Azure CLI you can run the following commands
> ```PowerShell
> # Login into Azure CLI
> az login --use-device-code
>
> # Check you are logged into the right Azure subscription. Inspect the name field
> az account show
>
> # In case not the right subscription
> az account set -s <subscription-id>
>
>```

```PowerShell

az containerapp github-action add \
  --repo-url $repoUrl \
  --context-path "./queuereaderapp/Dockerfile" \
  --branch main \
  --name queuereader \
  --resource-group $resourceGroup \
  --registry-url $acrUrl \
  --registry-username $acrUsername \
  --registry-password $acrSecret \
  --service-principal-client-id $spClientid \
  --service-principal-client-secret $spClientSecret \
  --service-principal-tenant-id $tenantid \
  --token $ghToken
```

  </summary>
</details>
<br>

The command will create a GitHub Action and run it, it takes a couple of minutes, please check the status at _github.com --> Actions_ and see the progress of the GitHub Action after it has been created by the Azure CLI command.

Dive into the logs and locate the _latestRevisionName_, then go to the Azure portal and verify that the revision name is the same for the _queuereader_ Container App.

![ghaction1](images/ghaction1.png)

![ghaction2](images/ghaction2.png)

![revision](images/revision.png)


### Do a code change in QueueReader App and push changes to GitHub repository
Now it’s time to do a code change and validate that it has been deployed.

Open _VS Code_ --> _queuereaderapp_ folder --> Open _Worker.cs_ and scroll down to line number **58**, where we are writing to the log.  

```c#
logger.LogInformation($"Message ID: '{message.MessageId}', contents: '{message.Body?.ToString()}'");
```
Below this line insert the following code.

```c#
logger.LogInformation("This is a new log message!");
```
Then open the Terminal in VS Code and make sure you are in the _queuereaderapp_ folder. Run this command.

```bash
dotnet build . 
```
Make sure that the build was succeeded.

Commit the change in VS Code.

![commit](images/commit.png)

After the commit, the previous created GitHub Action starts, follow the progress at _github.com --> Actions_.

After the deployment has succeeded, please verify that the revision number has changed using the Azure portal.

### Verify that the code change has been deployed 

Now it’s time to validate that the changes we made has taken affect. Send a message to the API.

<details>
  <summary>Bash</summary>

```bash
curl -X POST $dataURL?message=mynewlogmessage

```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
Invoke-RestMethod "$($dataURL)?message=mynewlogmessage" -Method Post
```

  </summary>
</details>
<br>

Validate the change by looking in _Log Analytics_.

```kusto
ContainerAppConsoleLogs_CL
| where ContainerAppName_s has "queuereader" and ContainerName_s has "queuereader"
| where Log_s has "Message"
| project TimeGenerated, Log_s
| order by TimeGenerated desc
| limit 50
``` 

Here you should see one row with the text "This is a new log message!".

You have now configured a simple CI/CD pipeline to automatically deploy code changes to the application.

Next step is to enhance security by protecting our _HTTP API_ using _API Management_. 

That will be covered in [Challenge 6](challenge6.md)
## The challenges

- [Challenge 1: Setup the environment](challenge1.md)
- [Challenge 2: Deploy Container Apps Environment and troubleshoot Container Apps](challenge2.md)
- [Challenge 3: Split traffic for controlled rollout](challenge3.md)
- [Challenge 4: Scale Container Apps](challenge4.md)
- [Challenge 5: Configure CI/CD for Container Apps](challenge5.md)
- [Challenge 6: Protect Container App with API Management](challenge6.md)
- [Challenge 7: Enable Container App authentication](challenge7.md)
