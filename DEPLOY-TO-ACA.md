# Azure Container Apps Deployment Guide

## Prerequisites

- Azure CLI installed: https://aka.ms/install-azure-cli
- Azure subscription
- **No Docker Desktop needed** - we'll build in Azure!

## Step 1: Login to Azure

```powershell
# Navigate to API folder
cd c:\Users\berry\repos\Api

# Login and select subscription
az login
az account set --subscription "your-subscription-name-or-id"
```

## Step 2: Create Azure Resources

```powershell
# Set variables
$resourceGroup = "rg-api-prod"
$location = "eastus"
$acrName = "acrapiprod"  # Must be globally unique, lowercase, no hyphens
$containerAppEnv = "env-api-prod"
$containerApp = "api-app"

# Create resource group
az group create --name $resourceGroup --location $location

# Create Azure Container Registry
az acr create --resource-group $resourceGroup --name $acrName --sku Basic

# Create Container Apps environment
az containerapp env create `
  --name $containerAppEnv `
  --resource-group $resourceGroup `
  --location $location
```

## Step 3: Build and Push Image to ACR

```powershell
# Build image in Azure (no local Docker needed!)
az acr build --registry $acrName --image api:latest --file Dockerfile .
```

This command uploads your code to Azure and builds the container image in the cloud.

## Step 4: Deploy to Container Apps

```powershell
# Deploy container app
az containerapp create `
  --name $containerApp `
  --resource-group $resourceGroup `
  --environment $containerAppEnv `
  --image "$acrName.azurecr.io/api:latest" `
  --registry-server "$acrName.azurecr.io" `
  --target-port 8080 `
  --ingress external `
  --min-replicas 0 `
  --max-replicas 3 `
  --cpu 0.5 `
  --memory 1Gi `
  --env-vars `
    ASPNETCORE_ENVIRONMENT=Production

# Get the app URL
az containerapp show --name $containerApp --resource-group $resourceGroup --query properties.configuration.ingress.fqdn -o tsv
```

## Step 5: Configure Environment Variables

```powershell
# Add Cosmos DB and Azure AD settings as secrets and environment variables
az containerapp update `
  --name $containerApp `
  --resource-group $resourceGroup `
  --set-env-vars `
    "CosmosDb__AccountEndpoint=https://webappdb0404.documents.azure.com:443/" `
    "CosmosDb__DatabaseName=DB1" `
    "CosmosDb__ContainerName=products" `
    "AzureAd__Instance=https://taskmanagement0404.ciamlogin.com/" `
    "AzureAd__Domain=taskmanagement0404.onmicrosoft.com" `
    "AzureAd__TenantId=2dc45b6b-68fe-452f-a678-da1cd8802509" `
    "AzureAd__ClientId=f0c915ae-4ab8-4de9-a424-0ed7863f276c" `
    "AzureAd__Audience=api://f0c915ae-4ab8-4de9-a424-0ed7863f276c" `
    "AzureAd__Scopes=Tasks.ReadWrite"
```

## Step 6: Configure Managed Identity for Cosmos DB

```powershell
# Enable system-assigned managed identity
az containerapp identity assign `
  --name $containerApp `
  --resource-group $resourceGroup `
  --system-assigned

# Get the managed identity principal ID
$principalId = az containerapp identity show `
  --name $containerApp `
  --resource-group $resourceGroup `
  --query principalId -o tsv

# Grant Cosmos DB access (run this in Azure Portal or use Cosmos CLI)
# Navigate to Cosmos DB > Access Control (IAM) > Add role assignment
# Role: Cosmos DB Built-in Data Contributor
# Assign access to: Managed Identity
# Select your container app's managed identity
```

## Step 7: Update React App CORS

Update [Program.cs](Program.cs#L126) to include your Container App URL:

```csharp
policy.WithOrigins(
    "http://localhost:5173",
    "https://victorious-wave-0202d810f.1.azurestaticapps.net",
    "https://YOUR-CONTAINER-APP-URL.azurecontainerapps.io"  // Add this
)
```

Then rebuild and redeploy.

## Update Deployment (After Code Changes)

```powershell
# Rebuild and push new image
az acr build --registry $acrName --image api:latest --file Dockerfile .

# Update container app with new image
az containerapp update `
  --name $containerApp `
  --resource-group $resourceGroup `
  --image "$acrName.azurecr.io/api:latest"
```

## Monitor and Troubleshoot

```powershell
# View logs
az containerapp logs show --name $containerApp --resource-group $resourceGroup --follow

# Check app status
az containerapp show --name $containerApp --resource-group $resourceGroup

# List revisions
az containerapp revision list --name $containerApp --resource-group $resourceGroup -o table
```

## Cost Optimization

- Container Apps scale to zero when idle (consumption plan)
- Monitor with: `az monitor metrics list`
- Consider Basic SKU for ACR ($0.167/day)
- Delete resources when not needed:
  ```powershell
  az group delete --name $resourceGroup --yes
  ```

## Next Steps

1. Update UI [apiService.ts](../../UI/src/services/apiService.ts) with Container App URL
2. Configure CI/CD with GitHub Actions (optional)
3. Set up Application Insights for monitoring

---

## Optional: Test Locally with Docker

If you want to test locally before deploying:

```powershell
# Install Docker Desktop from: https://www.docker.com/products/docker-desktop/

# Build and test locally
docker build -t api:local .
docker run -p 8080:8080 `
  -e CosmosDb__ConnectionString="your-local-connection-string" `
  -e ASPNETCORE_ENVIRONMENT=Development `
  api:local

# Test at http://localhost:8080/swagger
```
