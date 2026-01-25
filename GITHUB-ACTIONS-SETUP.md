# GitHub Actions CI/CD Setup

## ✅ Files Created

- `.github/workflows/deploy-container-app.yml` - Deployment workflow

## 📋 Setup Instructions

### 1. Add Azure Credentials to GitHub

1. Go to your GitHub repository: **Settings** → **Secrets and variables** → **Actions**

2. Click **New repository secret**

3. Create a secret named: `AZURE_CREDENTIALS`

4. Paste this JSON as the value (copy the entire block):

```json
{
  "clientId": "f280f90f-6003-4db2-8a68-917bca9e7cac",
  "clientSecret": "QsN8Q~XQGQtQrLronqoJbfd_Waqxnps8-frd1bx4",
  "subscriptionId": "02c42fff-434d-4832-ae32-0c443c8140c8",
  "tenantId": "ba473e14-d5b0-458f-8560-86ee220d71b6",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

5. Click **Add secret**

### 2. Commit and Push

```bash
git add .github/
git commit -m "Add GitHub Actions deployment workflow"
git push origin main
```

### 3. Automatic Deployment

The workflow will automatically run when:

- You push changes to the `main` branch in the `Api/` folder
- You can also manually trigger it from GitHub Actions tab

### 4. Monitor Deployment

Go to your repository → **Actions** tab to see the deployment progress.

## How It Works

1. **Trigger**: Push to main branch or manual trigger
2. **Build**: Builds Docker image in Azure Container Registry
3. **Deploy**: Updates Container App with new image
4. **Tags**: Each deployment is tagged with the git commit SHA for traceability

## Benefits

✅ Automatic deployments on every push  
✅ Version tracking with git commit SHAs  
✅ Build in the cloud (no Docker Desktop needed)  
✅ Rollback capability by deploying previous commit SHAs  
✅ Deployment history in GitHub Actions

## Manual Deployment (Fallback)

If you need to deploy manually:

```bash
az acr build --registry acrapi0404 --image api:latest --file Dockerfile .
az containerapp update --name api-app --resource-group RG1 --image acrapi0404.azurecr.io/api:latest
```

---

**⚠️ IMPORTANT:** Keep the Azure credentials secure! They are now saved in your GitHub repository secrets. Never commit them to your code.
