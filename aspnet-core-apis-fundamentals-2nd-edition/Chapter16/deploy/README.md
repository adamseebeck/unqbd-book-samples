# Deploying the Warehouse API to Azure App Service

These commands create the Azure resources that `ci/dotnet.yml` deploys to: a container registry, a Linux App Service plan with a staging slot, a key vault for the secrets, and the identity GitHub Actions signs in with. Nothing in the companion code runs them. Run them yourself with the Azure CLI (`az login` first), in a subscription where you may create resources; the plan, the registry and the key vault are charged while they exist. Delete the resource group when you no longer need it.

The examples use Bash. Replace the placeholder values; registry, web app and key vault names must be unique across Azure.

```bash
RG=warehouse-rg
LOCATION=eastus
ACR=warehouseacr12345
PLAN=warehouse-plan
APP=warehouse-api-12345
VAULT=warehouse-kv-12345
REPO=your-github-account/your-repository
```

## 1. Registry, plan, web app and staging slot

```bash
az group create --name $RG --location $LOCATION
az acr create --resource-group $RG --name $ACR --sku Basic

# Deployment slots need the Standard tier or higher
az appservice plan create --resource-group $RG --name $PLAN --is-linux --sku S1

# A public sample image until the pipeline deploys the Warehouse API
az webapp create --resource-group $RG --plan $PLAN --name $APP \
  --container-image-name mcr.microsoft.com/dotnet/samples:aspnetapp
az webapp deployment slot create --resource-group $RG --name $APP --slot staging

# HTTPS Only: the front ends redirect plain HTTP to HTTPS, so no request reaches the API over HTTP
az webapp update --resource-group $RG --name $APP --https-only true
az webapp update --resource-group $RG --name $APP --slot staging --https-only true
```

The container listens on plain HTTP behind the App Service front ends, which terminate TLS. With HTTPS Only, a request to an `http://` address receives a redirect from the front end and never reaches the API. The redirect comes too late for a client that sent a token with it, so give every client an `https://` address.

## 2. Managed identities: pulling images and reading secrets

Each slot has its own system-assigned identity. Both identities pull from the registry and read the key vault; the API's `ManagedIdentityCredential` uses the identity of the slot it runs in.

```bash
az keyvault create --resource-group $RG --name $VAULT --location $LOCATION --enable-rbac-authorization true
ACR_ID=$(az acr show --name $ACR --query id --output tsv)
VAULT_ID=$(az keyvault show --name $VAULT --query id --output tsv)

for SLOT in production staging; do
  SLOT_ARG=""
  if [ "$SLOT" = "staging" ]; then SLOT_ARG="--slot staging"; fi

  PRINCIPAL=$(az webapp identity assign --resource-group $RG --name $APP $SLOT_ARG --query principalId --output tsv)
  az role assignment create --assignee-object-id $PRINCIPAL --assignee-principal-type ServicePrincipal \
    --role AcrPull --scope $ACR_ID
  az role assignment create --assignee-object-id $PRINCIPAL --assignee-principal-type ServicePrincipal \
    --role "Key Vault Secrets User" --scope $VAULT_ID

  # Pull with the identity instead of registry credentials
  az webapp config set --resource-group $RG --name $APP $SLOT_ARG \
    --generic-configurations '{"acrUseManagedIdentityCreds": true}'
done
```

## 3. Secrets in the key vault

Key Vault secret names cannot contain colons; `--` stands for the `:` of a configuration key. You need permission to write secrets (for example the Key Vault Secrets Officer role) to run these.

```bash
az keyvault secret set --vault-name $VAULT --name "ConnectionStrings--WarehouseDb" \
  --value "<the production SQL Server connection string>"
az keyvault secret set --vault-name $VAULT --name "ExternalShipmentService--ApiKey" \
  --value "<the key your ERP issued>"
```

A connection string that signs in with Microsoft Entra ID (`Authentication=Active Directory Managed Identity`) needs the Microsoft.Data.SqlClient.Extensions.Azure package in the API project: with EF Core 11, Microsoft.Data.SqlClient 7 moved Entra ID authentication into that package.

## 4. Settings for both slots

```bash
for SLOT_ARG in "" "--slot staging"; do
  az webapp config appsettings set --resource-group $RG --name $APP $SLOT_ARG --settings \
    WEBSITES_PORT=8080 \
    WEBSITES_CONTAINER_STOP_TIME_LIMIT=60 \
    WEBSITE_SWAP_WARMUP_PING_PATH=/health \
    WEBSITE_SWAP_WARMUP_PING_STATUSES=200 \
    KeyVault__Uri="https://$VAULT.vault.azure.net/" \
    Authentication__Schemes__Bearer__Authority="https://login.example.com/warehouse/" \
    ExternalShipmentService__BaseUrl="https://erp.example.com/" \
    ForwardedHeaders__KnownNetworks__0="::ffff:10.0.0.0/104" \
    ForwardedHeaders__KnownNetworks__1="::ffff:192.168.0.0/112" \
    ForwardedHeaders__KnownNetworks__2="::ffff:172.16.0.0/108" \
    ForwardedHeaders__KnownNetworks__3="10.0.0.0/8" \
    ForwardedHeaders__KnownNetworks__4="192.168.0.0/16" \
    ForwardedHeaders__KnownNetworks__5="172.16.0.0/12"
done

# Slot settings stay with their slot when the slots swap. Only production sends shipment requests to the ERP,
# and each slot accepts tokens for its own audience, so production clients' tokens do nothing in staging
az webapp config appsettings set --resource-group $RG --name $APP --slot-settings \
  Fulfillment__ProcessorEnabled=true Authentication__Schemes__Bearer__ValidAudiences__0="api://warehouse-api"
az webapp config appsettings set --resource-group $RG --name $APP --slot staging --slot-settings \
  Fulfillment__ProcessorEnabled=false Authentication__Schemes__Bearer__ValidAudiences__0="api://warehouse-api-staging"
```

`WEBSITES_PORT` tells App Service that the container listens on 8080, and `WEBSITES_CONTAINER_STOP_TIME_LIMIT` gives a stopping container 60 seconds after SIGTERM instead of the default 5, more than the API's 45-second shutdown timeout, so request draining and an ERP call in progress can finish during a restart, a scale-in or a swap. Before a swap, App Service sends a warm-up request to `/health` on the staging instances and swaps only when it answers 200, so an instance that is not ready stops the swap. App Service terminates TLS in its load balancers, which reach the container from private addresses; the `KnownNetworks` entries are the three private ranges Microsoft documents for them, in the IPv4-mapped IPv6 form that the built-in Linux stack reports and in plain IPv4 form, so the forwarded headers middleware accepts their `X-Forwarded-Proto` and `X-Forwarded-For` headers and nobody else's. Replace the authority, the audiences and the ERP address with your own; the staging audience belongs to a second app registration, whose tokens only your testers request.

Both slots use the production database, so every request the staging slot serves changes production data. Restrict who can reach it: it accepts only tokens for the staging audience, and you can also limit its network access with `az webapp config access-restriction add --slot staging` (the readiness check of the pipeline then needs a rule for its runner). Keep its background work off as well: `Fulfillment__ProcessorEnabled=false` keeps its fulfillment processor from calling the ERP; it records shipment requests as usual, and the processor in the production slot sends them with its sweep. As slot settings, these values stay where they are when the slots swap, so the version in production always runs the processor and accepts the production audience.

## 5. The database schema

The API applies migrations at startup only in Development. Before a release, create an idempotent script from the migrations and run it against the production database with the tools your database team uses. Run it before the push to `main` that deploys the release: the staging slot starts the new image against the production database as soon as `deploy-staging` points it there, and every request there that needs a new column or table fails until the schema has it.

```bash
dotnet tool restore
dotnet ef migrations script --idempotent --project Chapter16/WarehouseApi --output migrate.sql
```

Both slots use the same database, so the old version keeps running against the new schema until the swap. Make each migration work with both versions: expand the schema in one release, adding columns and tables, and contract it in a later release, removing the old ones.

The tests run on databases that EF Core creates with READ_COMMITTED_SNAPSHOT ON, and Azure SQL Database has it on by default. A SQL Server database that your team creates has it off: before the first release, run `ALTER DATABASE <database> SET READ_COMMITTED_SNAPSHOT ON` once, while nothing else is connected, because the statement waits until every other connection to the database has closed. Production then runs under the isolation the tests used.

## 6. Signing GitHub Actions in without a stored secret

The workflow signs in with OpenID Connect: GitHub issues a token for the job, and Microsoft Entra ID accepts it for an app registration that trusts your repository's `staging` and `production` environments.

```bash
CLIENT_ID=$(az ad app create --display-name warehouse-api-github --query appId --output tsv)
az ad sp create --id $CLIENT_ID

for ENVIRONMENT in staging production; do
  az ad app federated-credential create --id $CLIENT_ID --parameters "{
    \"name\": \"github-$ENVIRONMENT\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:$REPO:environment:$ENVIRONMENT\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"
done

WEBAPP_ID=$(az webapp show --resource-group $RG --name $APP --query id --output tsv)
az role assignment create --assignee $CLIENT_ID --role AcrPush --scope $ACR_ID
az role assignment create --assignee $CLIENT_ID --role "Website Contributor" --scope $WEBAPP_ID
```

In the GitHub repository, create the environments `staging` and `production`, give `production` required reviewers, and add these repository variables: `AZURE_CLIENT_ID` ($CLIENT_ID), `AZURE_TENANT_ID` (`az account show --query tenantId`), `AZURE_SUBSCRIPTION_ID` (`az account show --query id`), `RESOURCE_GROUP`, `ACR_NAME` and `WEBAPP_NAME`. None of them is a secret.

## 7. A release, step by step

1. Apply the release's idempotent migration script to the production database (section 5).
2. A push to `main` runs the tests and builds the image.
3. The `deploy-staging` job pushes the image, tagged with the commit SHA and built with it as the source revision, points the staging slot at it, then waits until `/health` on the staging slot answers 200 with a `version` that ends in the commit SHA: the previous container can answer 200 as well, but not with this commit.
4. A reviewer approves the `production` environment. The `swap-to-production` job checks that the staging slot is still set to the image of its own commit, then swaps the slots; App Service's warm-up request to `/health` must answer 200 first. The previous version now runs in staging: swap again to roll back.

The two deployment jobs share a concurrency group, so only one of them runs at a time; a later push waits for the deployment job that is running. Whether a swap job that waits for its approval holds the group is GitHub's decision; if it does not, a later push can reach the staging slot meanwhile, and the image check then stops the earlier swap, so the reviewer approves the later run instead. Either way, the slot that is swapped runs the commit that was approved.

Remove everything with `az group delete --name $RG`, and delete the app registration with `az ad app delete --id $CLIENT_ID`.
