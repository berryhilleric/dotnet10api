# Azure Blob Storage Setup for Product Images

This guide explains how to set up Azure Blob Storage for your product images.

## Option 1: Quick Setup (Recommended)

Run these Azure CLI commands to create and configure storage:

```powershell
# Set variables (replace with your values)
$RESOURCE_GROUP = "your-resource-group-name"
$STORAGE_ACCOUNT = "yourstorageaccount"  # Must be globally unique, lowercase, no hyphens
$LOCATION = "eastus"  # Or your preferred region
$CONTAINER_NAME = "product-images"

# Create storage account
az storage account create `
  --name $STORAGE_ACCOUNT `
  --resource-group $RESOURCE_GROUP `
  --location $LOCATION `
  --sku Standard_LRS `
  --kind StorageV2

# Create blob container with public read access
az storage container create `
  --name $CONTAINER_NAME `
  --account-name $STORAGE_ACCOUNT `
  --public-access blob `
  --auth-mode login

# Enable CORS for your frontend
az storage cors add `
  --services b `
  --methods GET POST PUT DELETE OPTIONS `
  --origins "http://localhost:5173" "https://victorious-wave-0202d810f.1.azurestaticapps.net" `
  --allowed-headers "*" `
  --exposed-headers "*" `
  --max-age 200 `
  --account-name $STORAGE_ACCOUNT
```

## Option 2: Using Azure Portal

1. **Create Storage Account**
   - Navigate to Azure Portal → Create Resource → Storage Account
   - Choose your resource group
   - Enter a unique storage account name (lowercase, no special chars)
   - Select region and performance tier (Standard is fine)
   - Click "Review + Create"

2. **Create Container**
   - Open your storage account
   - Go to "Containers" in the left menu
   - Click "+ Container"
   - Name: `product-images`
   - Public access level: **Blob** (allows direct image access)
   - Click "Create"

3. **Configure CORS** (if frontend needs direct uploads)
   - In storage account, go to "Resource sharing (CORS)"
   - Click "Blob service"
   - Add these rules:
     - Allowed origins: `http://localhost:5173`, your production URL
     - Allowed methods: GET, POST, PUT, DELETE, OPTIONS
     - Allowed headers: `*`
     - Exposed headers: `*`
     - Max age: 200

## Configure Your Application

### For Local Development

Add connection string to `appsettings.Development.json`:

```json
{
  "BlobStorage": {
    "AccountName": "yourstorageaccount",
    "ContainerName": "product-images",
    "ConnectionString": "DefaultEndpointsProtocol=https;AccountName=yourstorageaccount;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net"
  }
}
```

Get connection string:

```powershell
az storage account show-connection-string `
  --name $STORAGE_ACCOUNT `
  --resource-group $RESOURCE_GROUP `
  --output tsv
```

### For Production (Azure Container Apps)

Use Managed Identity (no keys needed!):

1. **Enable Managed Identity on Container App**

   ```powershell
   az containerapp identity assign `
     --name your-api-app-name `
     --resource-group $RESOURCE_GROUP `
     --system-assigned
   ```

2. **Grant Storage Access**

   ```powershell
   # Get the principal ID
   $PRINCIPAL_ID = az containerapp identity show `
     --name your-api-app-name `
     --resource-group $RESOURCE_GROUP `
     --query principalId -o tsv

   # Assign Storage Blob Data Contributor role
   az role assignment create `
     --assignee $PRINCIPAL_ID `
     --role "Storage Blob Data Contributor" `
     --scope "/subscriptions/{subscription-id}/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Storage/storageAccounts/$STORAGE_ACCOUNT"
   ```

3. **Update Container App Environment Variables**
   ```powershell
   az containerapp update `
     --name your-api-app-name `
     --resource-group $RESOURCE_GROUP `
     --set-env-vars `
       "BlobStorage__AccountName=$STORAGE_ACCOUNT" `
       "BlobStorage__ContainerName=$CONTAINER_NAME"
   ```

## Update appsettings.json

Replace `YOUR_STORAGE_ACCOUNT_NAME` in `appsettings.json`:

```json
{
  "BlobStorage": {
    "AccountName": "yourstorageaccount",
    "ContainerName": "product-images"
  }
}
```

## Testing

### Upload an image via API:

```powershell
# First get a product ID from your API
$PRODUCT_ID = "your-product-id"
$IMAGE_PATH = "C:\path\to\your\image.jpg"

# Upload image (requires authentication token)
curl -X POST "https://your-api-url/api/products/$PRODUCT_ID/image" `
  -H "Authorization: Bearer YOUR_JWT_TOKEN" `
  -F "image=@$IMAGE_PATH"
```

### Verify in Products:

```powershell
curl https://your-api-url/api/products `
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

The product should now have an `imageUrl` field with the blob storage URL.

## Cost Optimization

- **Storage Tiers**: Use Cool or Archive tier for rarely accessed images
- **CDN**: Add Azure CDN for faster global delivery and lower egress costs
- **Lifecycle Policies**: Automatically move old images to cheaper tiers

## Security Best Practices

✅ **Current Setup**: Public blob access (images publicly accessible by URL)

- Good for public product catalogs
- Images load directly in browser without auth

🔒 **For Private Images**: Change container to private and use SAS tokens

- Update `BlobStorageService` to generate SAS tokens
- Images require valid token to access

## Next Steps

1. Run the Azure CLI commands above
2. Update `appsettings.json` with your storage account name
3. Rebuild and restart your API
4. Test image upload using the Products API

## Troubleshooting

**401 Unauthorized**:

- Verify Managed Identity is enabled and has Storage Blob Data Contributor role
- Check that container name matches configuration

**CORS errors**:

- Verify CORS is configured on storage account
- Check that your frontend URL is in allowed origins

**Images not loading**:

- Verify container public access level is "Blob"
- Check image URL format: `https://{account}.blob.core.windows.net/{container}/{blob}`
