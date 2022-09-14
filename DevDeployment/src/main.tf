data "azurerm_subnet" "main_subnet" {
  name                 = var.spoke_subnet_name
  virtual_network_name = var.spoke_vnet_name
  resource_group_name  = var.spoke_rg
}

data "azurerm_subnet" "agent_subnet" {
  provider             = azurerm.build_agent
  name                 = var.agent_subnet_name
  virtual_network_name = var.agent_vnet_name
  resource_group_name  = var.agent_rg
}

data "azurerm_app_service_plan" "essft_asp" {
  name                = "essft-qc-yh3r1-asp"
  resource_group_name = "essft-qc-webapp-rg"
}

data "azurerm_app_service_plan" "ess_asp" {
  name                = "ess-${local.env_name}-lxs-1-asp"
  resource_group_name = "ess-${local.env_name}-rg"
}

module "app_insights" {
  source              = "./Modules/AppInsights"
  name                = "${local.service_name}-${local.env_name}-insights"
  resource_group_name = azurerm_resource_group.webapp_rg.name
  location            = azurerm_resource_group.webapp_rg.location
  tags                = local.tags
}

module "eventhub" {
  source              = "./Modules/EventHub"
  name                = "${local.service_name}-${local.env_name}-events"
  resource_group_name = azurerm_resource_group.webapp_rg.name
  location            = azurerm_resource_group.webapp_rg.location
  tags                = local.tags
  env_name            = local.env_name
}

module "mock_webapp_service" {
  source              = "./Modules/MockWebApp"
  name                = local.mock_web_app_name
  env_name            = local.env_name
  resource_group_name = azurerm_resource_group.mock_webapp_rg.name
  service_plan_id     = data.azurerm_app_service_plan.essft_asp.id
  location            = azurerm_resource_group.mock_webapp_rg.location
  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                               = local.env_name
    "WEBSITE_RUN_FROM_PACKAGE"                             = "1"
    "WEBSITE_ENABLE_SYNC_UPDATE_SITE"                      = "true"
    "APPINSIGHTS_INSTRUMENTATIONKEY"                       = "NOT_CONFIGURED"
  }
  tags                                                     = local.tags
}

module "webapp_service" {
  source                    = "./Modules/WebApp"
  name                      = local.web_app_name
  resource_group_name       = azurerm_resource_group.webapp_rg.name
  service_plan_id           = data.azurerm_app_service_plan.ess_asp.id
  env_name                  = local.env_name
  location                  = azurerm_resource_group.webapp_rg.location
  subnet_id                 = data.azurerm_subnet.main_subnet.id
  app_settings = {
    "KeyVaultSettings:ServiceUri"                              = "https://${local.key_vault_name}.vault.azure.net/"
    "EventHubLoggingConfiguration:Environment"                 = local.env_name
    "EventHubLoggingConfiguration:MinimumLoggingLevel"         = "Warning"
    "EventHubLoggingConfiguration:UkhoMinimumLoggingLevel"     = "Information"
    "APPINSIGHTS_INSTRUMENTATIONKEY"                           = module.app_insights.instrumentation_key
    "ASPNETCORE_ENVIRONMENT"                                   = local.env_name
    "WEBSITE_RUN_FROM_PACKAGE"                                 = "1"
    "WEBSITE_ENABLE_SYNC_UPDATE_SITE"                          = "true"
    "FSSApiConfiguration:BusinessUnit"                         = "AVCSData"
    "FSSApiConfiguration:PosReadGroups"                        = "public"
    "FSSApiConfiguration:PosReadUsers"                         = ""
  }
  tags                                                         = local.tags
  allowed_ips                                                  = var.allowed_ips
}

module "key_vault" {
  source              = "./Modules/KeyVault"
  name                = local.key_vault_name
  resource_group_name = azurerm_resource_group.webapp_rg.name
  env_name            = local.env_name
  tenant_id           = module.webapp_service.web_app_tenant_id
  allowed_ips         = var.allowed_ips
  allowed_subnet_ids  = [data.azurerm_subnet.main_subnet.id,data.azurerm_subnet.agent_subnet.id]
  location            = azurerm_resource_group.webapp_rg.location
  read_access_objects = {
     "webapp_service" = module.webapp_service.web_app_object_id
  }
  secrets = {
      "EventHubLoggingConfiguration--ConnectionString"       = module.eventhub.log_primary_connection_string
      "EventHubLoggingConfiguration--EntityPath"             = module.eventhub.entity_path
 }
  tags                                                       = local.tags
}
