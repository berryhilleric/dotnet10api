# GitHub Actions CI/CD Setup

## ✅ Files Created

- `.github/workflows/deploy-container-app.yml` - Deployment workflow

## 📋 Setup Instructions

### 1. Add Azure Credentials to GitHub

1. Go to your GitHub repository: **Settings** → **Secrets and variables** → **Actions**

2. Click **New repository secret**

3. Create a secret named: `AZURE_CREDENTIALS`

4. Get the Azure credentials by running this command in PowerShell:

```powershell
az ad sp list --display-name "github-actions-api-deploy" --query "[0]" -o json
```

Or retrieve the full credentials from your terminal output when you ran the `az ad sp create-for-rbac` command.

5. Paste the JSON output as the secret value in GitHub

6. Click **Add secret**

### 2. Commit and Push

**Note:** The actual Azure credentials should NOT be committed to your repository. They should only be added as a GitHub Secret (step 1 above).

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
