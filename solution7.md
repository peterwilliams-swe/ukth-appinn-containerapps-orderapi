# Challenge 7: Solution

## Solution steps
We will enable Microsoft Entra ID authentication for _Dashboard App_ Container App using Azure Portal.

### Enable Microsoft Entra ID authentication
Navigate to the Container Dashboard App in [Azure Portal](https://portal.azure.com) and select the _Authentication_ blade.

![](/images/easyauth-authentication.png)

Select _Add Identity provider_ and select _Microsoft_ as the identity provider

![](/images/easyauth-identityprovider.png)

> [!NOTE]
> Review the extensive list of identity provider options available

In the _Add identity provider_ page change the name of the identity provider to be prefixed with the unique name generated earlier in the lab (stored in variable `$name`)

Leave the other options with default values
![](/images/easyauth-identityprovideroptions.png)


Select _Next: Permissions_.

![](/images/easyauth-permission.png)

Accept the default values and click _Add_

The _Dashboard App_ is now configured with Azure AD Authentication.


### Verify authentication

Get the Dashboard URL from the variable (`$dashboardURL`) created in a previous challenge

If you don't have that variable available you can get it via the following command:


<details>
  <summary>Bash</summary>

```bash
dashboardURL=https://dashboardapp.$(az containerapp env show -g $resourceGroup -n $containerAppEnv --query 'properties.defaultDomain' -o tsv)
echo 'Open the URL in your browser of choice:' $dashboardURL

```

  </summary>
</details>

<details>
  <summary>PowerShell</summary>

```PowerShell

$dashboardURL="https://dashboardapp.$((Get-AzContainerAppManagedEnv -ResourceGroupName $resourceGroup -EnvName $containerAppEnv).DefaultDomain)/"
Write-Host "Open the URL in your browser of choice: $dashboardURL"
```

  </summary>
</details>
<br>

Open the Url in a browser. You will be prompted for login similar to this

![](images/easyauth-login.png)

Make sure to select your organizational account if you have several accounts. 
Select login.

Next you will be presented with a consent dialog. 

![](images/easyauth-consent.png)

Accept the consent and you will be redirected to the _Dashboard App_

![](/images/easyauth-dashboardapp.png)


You have now enabled Microsoft Entra ID authentication for  _Dashboard App_ Container App.

This concludes the series of challenge. You can follow the instructions in [Clean up](cleanup.md) to remove Azure resources created in this hackaton.

## The challenges

- [Challenge 1: Setup the environment](challenge1.md)
- [Challenge 2: Deploy Container Apps Environment and troubleshoot Container Apps](challenge2.md)
- [Challenge 3: Split traffic for controlled rollout](challenge3.md)
- [Challenge 4: Scale Container Apps](challenge4.md)
- [Challenge 5: Configure CI/CD for Container Apps](challenge5.md)
- [Challenge 6: Protect Container App with API Management](challenge6.md)
- [Challenge 7: Enable Container App authentication](challenge7.md)
