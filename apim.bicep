param apiManagementName string
param location string = resourceGroup().location

var selfHostedGatewayName = 'gw-01'

resource apim 'Microsoft.ApiManagement/service@2023-05-01-preview' = {
  name: apiManagementName
  location: location
  sku: {
    name: 'StandardV2'
    capacity: 1
  }
  properties: {
    publisherName: 'Contoso'
    publisherEmail: 'john.doe@nomail.com'
  }
}

resource selfHostedGateway 'Microsoft.ApiManagement/service/gateways@2023-05-01-preview' = {
  name: selfHostedGatewayName
  parent: apim
  properties: {
    description: 'Self-hosted API Gateway on Azure Container Apps'
    locationData: {
      name: 'Azure Container Apps'
      countryOrRegion: 'Cloud'
    }
  }
}
