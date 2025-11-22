
@description('The location for all resources')
param location string = resourceGroup().location

@description('Environment name (dev, staging, prod)')
param environment string

@description('Repository URL for the application')
param repositoryUrl string

@description('Git branch to deploy from')
param branch string = 'main'

@description('CPU allocation in cores (e.g., 0.5 for half core). Minimum 0.25 cores')
param cpu string = '0.5'

@description('Memory allocation in Gi (e.g., 1.0 for 1 Gibibyte)')
param memory string = '1.0Gi'

@description('Initial number of replicas')
#disable-next-line no-unused-params
param replicas int = 1

@description('Minimum autoscale replicas')
param scaleMin int = 1

@description('Maximum autoscale replicas')
param scaleMax int = 3

@description('Target port for the container app (default: 8080)')
param targetPort int = 8080

@description('Key Vault name (optional)')
param keyVaultName string = ''

// Generate unique names based on resource group for better isolation
var uniqueSuffix = uniqueString(resourceGroup().id)
var appName = 'app-${environment}-${uniqueSuffix}'
var acrName = 'acr${uniqueSuffix}'
var containerAppEnvName = 'env-${environment}-${uniqueSuffix}'

// Environment-specific ASP.NET Core environment variable
var aspnetcoreEnvironment = environment == 'prod' ? 'Production' : (environment == 'staging' ? 'Staging' : 'Development')

// Common tags for all resources
var commonTags = {
  environment: environment
  repository: repositoryUrl
  branch: branch
  managedBy: 'DeploymentService'
}

resource acr 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: acrName
  location: location
  tags: commonTags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
    publicNetworkAccess: 'Enabled'
  }
}

resource containerAppEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: containerAppEnvName
  location: location
  tags: commonTags
  properties: {
    appLogsConfiguration: {
      destination: 'azure-monitor'
    }
  }
}

resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: appName
  location: location
  tags: commonTags
  properties: {
    managedEnvironmentId: containerAppEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: targetPort
        allowInsecure: false
        transport: 'auto'
        traffic: [
          {
            weight: 100
            latestRevision: true
          }
        ]
      }
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password'
        }
      ]
      secrets: [
        {
          name: 'acr-password'
          value: acr.listCredentials().passwords[0].value
        }
      ]
    }
    template: {
      revisionSuffix: 'v${uniqueString(repositoryUrl, branch)}'
      containers: [
        {
          name: 'main'
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          resources: {
            cpu: json(cpu)
            memory: memory
          }

          env: concat([
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: aspnetcoreEnvironment
            }
            {
              name: 'ASPNETCORE_HTTP_PORTS'
              value: string(targetPort)
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:${targetPort}'
            }
          ], !empty(keyVaultName) ? [
            {
              name: 'KeyVaultName'
              value: keyVaultName
            }
          ] : [])
      scale: {
        minReplicas: scaleMin
        maxReplicas: scaleMax
      }
    }
  }
}

output appUrl string = containerApp.properties.configuration.ingress.fqdn
output acrLoginServer string = acr.properties.loginServer
output appName string = appName
