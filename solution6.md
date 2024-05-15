# Challenge 6: Solution

## Solution steps
We will provision _API Management_ with a self hosted gateway and create a new Container App with internal ingress. The self hosted gateway will be created as a new Container App _apim_ and expose the API with external egress.

### Create an API Management service with self hosted gateway
First API Management must be created using the _Developer_ SKU (_Consumption_ SKU doesn't support SHGW). **This takes 30-45 minutes**. 

<details>
  <summary>Azure CLI using Bash</summary>

```bash
az deployment group create -g $resourceGroup -f apim.bicep -p apiManagementName=${name}-apim

```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
New-AzResourceGroupDeployment -ResourceGroupName $resourceGroup -Name 'apim_deployment' -TemplateFile .\apim.bicep -apiManagementName "$name-apim"

```

  </summary>
</details>
<br>

After the script has finished an API Management instance and a SHGW has been created.  

### Deploy Container Apps and create API Management configuration

Go to the API Management instance in Azure portal and click on _Gateways_ in the menu. A gateway called _gw-01_ has been created. Click on the gateway name --> _Deployment_ --> Copy everything in the field called _Token_ and set the variable "gwtoken", the value must be inside "" double quotes. 

Example: gwtoken="GatewayKey gw-01&202206230....."

<details>
  <summary>Bash</summary>

```bash
gwtoken="[Paste value from the Token field]"

```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
$gwtoken="[Paste value from the Token field]"
```

  </summary>
</details>
<br>

In the Azure portal, go to the resource group you have been working with and locate the name of the storageaccount that has been created. Set the storageaccount variable.  

<details>
  <summary>Bash</summary>

```bash
storageaccount=[Enter the name of the storageaccount]
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
$storageaccount="[Enter the name of the storageaccount]"
```

  </summary>
</details>
<br>

Deploy Container Apps and create API Management configuration. 

<details>
  <summary>Azure CLI using Bash</summary>

```bash
az deployment group create -g $resourceGroup -f v5_template.bicep -p apiManagementName=${name}-apim containerAppsEnvName=$containerAppEnv storageAccountName=$storageaccount selfHostedGatewayToken="$gwtoken" AppInsights_Name=$appInsights
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
New-AzResourceGroupDeployment -ResourceGroupName $resourceGroup -Name 'v5_deployment' -TemplateFile .\v5_template.bicep -apiManagementName "$name-apim" -containerAppsEnvName $containerAppEnv -storageAccountName $storageAccount -selfHostedGatewayToken ""$gwToken"" -AppInsights_Name=$appInsights 
```

  </summary>
</details>
<br>

### Verify external access to new Container App

Now API Management SHGW has been deployed as a Container App inside of Container Apps and a new Container App called _httpapi2_ has been created with an internal ingress which means that is not exposed to the internet.

API Management has protected the API using an API key so this needs to be retrieved. Got to the Azure portal --> _Subscriptions_ --> Choose the bottom row with the scope _Service_ --> on the right click the three dots --> Show/hide keys --> Copy the _Primary Key_ value

<details>
  <summary>Bash</summary>

```bash
apikey=[Paste the value of the primary key]
```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
$apikey="[Paste the value of the primary key]"
```

  </summary>
</details>
<br>

Retrieve the url of the SHGW in Container Apps. 

<details>
  <summary>Bash</summary>

```bash
apimURL=https://apim.$(az containerapp env show -g $resourceGroup -n ${name}-env --query 'properties.defaultDomain' -o tsv)/api/data

```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell
$apimURL="https://apim.$((Get-AzContainerAppManagedEnv -ResourceGroupName $resourceGroup -EnvName $containerAppEnv).DefaultDomain)/api/data"
```

  </summary>
</details>
<br>

Add a new order by using HTTP POST and add a header used for authenticate against API Management. 

<details>
  <summary>bash</summary>

```bash
curl -X POST -H "X-API-Key:$apikey" $apimURL?message=apimitem1

```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell

Invoke-RestMethod "$($apimURL)?message=apimitem1" -Method Post -Headers @{'X-API-Key' = $apikey}

```

  </summary>
</details>
<br>


Verify that it works in Log Analytics.

```kusto
ContainerAppConsoleLogs_CL
| where ContainerAppName_s has "queuereader" and ContainerName_s has "queuereader"
| where Log_s has "Message"
| project TimeGenerated, Log_s
| order by TimeGenerated desc
```

You have now protected _HTTP API_ Container App behind API Management.

Next step is to enhance security by protecting our _Dashboard App_ with Microsoft Entra ID Authentication. 

That will be covered in [Challenge 7](challenge7.md)

## The challenges

- [Challenge 1: Setup the environment](challenge1.md)
- [Challenge 2: Deploy Container Apps Environment and troubleshoot Container Apps](challenge2.md)
- [Challenge 3: Split traffic for controlled rollout](challenge3.md)
- [Challenge 4: Scale Container Apps](challenge4.md)
- [Challenge 5: Configure CI/CD for Container Apps](challenge5.md)
- [Challenge 6: Protect Container App with API Management](challenge6.md)
- [Challenge 7: Enable Container App authentication](challenge7.md)
