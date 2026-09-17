# Azure AI Search & Embedding Setup Guide

Follow these step-by-step instructions to create the **Azure AI Search** service and deploy an **Embedding model** in your Azure account.

---

## Step 1: Create Azure AI Search Service (Free Tier - $0/mo)

1. Open your browser and go to the **[Azure Portal](https://portal.azure.com)**.
2. In the top search bar, search for **`Azure AI Search`** (or **Search services**) and select it.
3. Click the **+ Create** button.
4. In the **Basics** tab, configure:
   - **Subscription**: Select your active subscription.
   - **Resource Group**: Select the same resource group where `enterpriceservicedesk` is located.
   - **Service Name**: Give it a unique lowercase name (e.g. `servicedesk-search-01` or `servicedesksearch<yourname>`).
   - **Location**: Select the **same region** as your Azure OpenAI resource (e.g. `East US` or `Sweden Central`).
   - **Pricing Tier**:
     - Click **Change Pricing Tier**.
     - Choose **Free (F)** ($0/month).
     - Click **Confirm**.
5. Click **Review + create** at the bottom, then click **Create**.
6. Wait 1-2 minutes until deployment completes, then click **Go to resource**.

---

## Step 2: Copy Search URL and Primary Admin Key

1. On the **Overview** page of your new Search service:
   - Copy the **Url** (it looks like `https://servicedesk-search-01.search.windows.net`).
2. On the left sidebar menu, look under **Settings** and click **Keys**.
3. Copy the **Primary admin key**.

---

## Step 3: Deploy Embedding Model in Azure OpenAI

1. In the Azure Portal, search for and open your existing Azure OpenAI resource: **`enterpriceservicedesk`**.
2. On the Overview page, click the button **Go to Azure OpenAI Studio** (or **Azure AI Foundry portal**).
3. On the left navigation bar, click **Deployments** (under *Shared resources* or *Management*).
4. Click **+ Deploy model** -> **Deploy base model**.
5. In the model list, search for:
   - **`text-embedding-3-large`** *(or `text-embedding-ada-002`)*
6. Select it and click **Confirm**.
7. In the configuration popup:
   - **Deployment name**: `text-embedding-3-large`
   - **Model version**: default (latest)
   - **Deployment type**: Standard
8. Click **Deploy**.

---

## Step 4: Add Credentials to `appsettings.json`

Once you have completed the above steps, you can paste the values into `ServiceDesk.Web/appsettings.json`:

```json
  "AzureAiSearch": {
    "Endpoint": "https://<your-search-service-name>.search.windows.net",
    "IndexName": "servicedesk-knowledge-index",
    "ApiKey": "<your-search-primary-admin-key>"
  },
  "AzureOpenAI": {
    "Endpoint": "https://enterpriceservicedesk.openai.azure.com/",
    "DeploymentName": "gpt-4o",
    "EmbeddingDeploymentName": "text-embedding-3-large",
    "ApiKey": "<your-azure-openai-key>"
  }
```

Once pasted, let me know and we will proceed with the C# Azure AI Search adapter implementation!
