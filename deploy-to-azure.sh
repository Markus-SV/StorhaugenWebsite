#!/bin/bash

# Azure Deployment Script for Storhaugen Eats API
# Usage: ./deploy-to-azure.sh

set -e  # Exit on error

echo "🚀 Deploying Storhaugen Eats API to Azure..."

# Configuration
RESOURCE_GROUP="storhaugen-eats-rg"
APP_NAME="storhaugen-eats-api"
LOCATION="westeurope"
PLAN_NAME="storhaugen-eats-plan"
SKU="B1"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}❌ Azure CLI is not installed${NC}"
    echo "Install from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

echo -e "${BLUE}📋 Step 1: Logging in to Azure...${NC}"
az login

echo -e "${BLUE}📋 Step 2: Creating resource group...${NC}"
az group create --name $RESOURCE_GROUP --location $LOCATION || echo "Resource group already exists"

echo -e "${BLUE}📋 Step 3: Creating App Service Plan...${NC}"
az appservice plan create \
  --name $PLAN_NAME \
  --resource-group $RESOURCE_GROUP \
  --sku $SKU \
  --is-linux || echo "App Service Plan already exists"

echo -e "${BLUE}📋 Step 4: Creating Web App...${NC}"
az webapp create \
  --resource-group $RESOURCE_GROUP \
  --plan $PLAN_NAME \
  --name $APP_NAME \
  --runtime "DOTNETCORE:8.0" || echo "Web App already exists"

echo -e "${BLUE}📋 Step 5: Configuring application settings...${NC}"
echo "Please configure these settings in Azure Portal:"
echo "- ConnectionStrings__DefaultConnection"
echo "- Supabase__Url"
echo "- Supabase__AnonKey"
echo "- Supabase__ServiceRoleKey"
echo "- Supabase__JwtSecret"
echo ""
echo "Or run the configuration command manually after reviewing the credentials."
echo ""

read -p "Have you configured the application settings? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${RED}⚠️  Please configure application settings before deploying${NC}"
    echo "Visit: https://portal.azure.com → $APP_NAME → Configuration"
    exit 1
fi

echo -e "${BLUE}📋 Step 6: Building application...${NC}"
cd StorhaugenEats.API
dotnet publish -c Release -o ./publish

echo -e "${BLUE}📋 Step 7: Creating deployment package...${NC}"
cd publish
zip -r ../deploy.zip . > /dev/null
cd ..

echo -e "${BLUE}📋 Step 8: Deploying to Azure...${NC}"
az webapp deployment source config-zip \
  --resource-group $RESOURCE_GROUP \
  --name $APP_NAME \
  --src deploy.zip

echo -e "${BLUE}📋 Step 9: Cleaning up...${NC}"
rm -rf publish deploy.zip

echo -e "${GREEN}✅ Deployment complete!${NC}"
echo ""
echo "Your API is now available at:"
APP_URL=$(az webapp show --resource-group $RESOURCE_GROUP --name $APP_NAME --query defaultHostName --output tsv)
echo -e "${GREEN}https://$APP_URL${NC}"
echo ""
echo "Next steps:"
echo "1. Visit https://$APP_URL/swagger to verify the API"
echo "2. Check logs: az webapp log tail --resource-group $RESOURCE_GROUP --name $APP_NAME"
echo "3. Configure CORS for your frontend domain"
echo "4. Update frontend API URL to https://$APP_URL"
