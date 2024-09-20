data "azurerm_subnet" "main_subnet" {
  name                 = var.spoke_subnet_name
  virtual_network_name = var.spoke_vnet_name
  resource_group_name  = var.spoke_rg
}

data "azurerm_subnet" "mock_main_subnet" {
  name                 = var.mock_spoke_subnet_name
  virtual_network_name = var.spoke_vnet_name
  resource_group_name  = var.spoke_rg
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

data "azurerm_app_service_plan" "essft_asp" {
  name                = "ess-${local.env_name}-sxs-2-asp"
  resource_group_name = "ess-${local.env_name}-rg"
}

data "azurerm_app_service_plan" "ess_asp" {
  name                = "ess-${local.env_name}-lxs-1-asp"
  resource_group_name = "ess-${local.env_name}-rg"
}

module "mock_webapp_service" {
  source              = "./Modules/MockWebApp"
  name                = local.mock_web_app_name
  env_name            = local.env_name
  resource_group_name = azurerm_resource_group.mock_webapp_rg.name
  service_plan_id     = data.azurerm_app_service_plan.essft_asp.id
  location            = azurerm_resource_group.mock_webapp_rg.location
  subnet_id           = data.azurerm_subnet.mock_main_subnet.id
  main_subnet_id      = data.azurerm_subnet.main_subnet.id
  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                               = local.env_name
    "WEBSITE_RUN_FROM_PACKAGE"                             = "1"
    "WEBSITE_ENABLE_SYNC_UPDATE_SITE"                      = "true"
    "APPINSIGHTS_INSTRUMENTATIONKEY"                       = "NOT_CONFIGURED"
  }
  tags                                                     = local.tags
  allowed_ips                                              = var.allowed_ips
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
    "FSSApiConfiguration:AIOBusinessUnit"                      = "AVCSData"
    "FSSApiConfiguration:AIOReadGroups"                        = "public"
    "FSSApiConfiguration:AIOReadUsers"                         = ""
    "ELASTIC_APM_SERVER_URL"                                   = var.elastic_apm_server_url
    "ELASTIC_APM_TRANSACTION_SAMPLE_RATE"                      = "1"
    "ELASTIC_APM_ENVIRONMENT"                                  = local.env_name
    "ELASTIC_APM_SERVICE_NAME"                                 = "POS Web Job"
    "ELASTIC_APM_API_KEY"                                      = var.elastic_apm_api_key
  }
  tags                                                         = local.tags
  allowed_ips                                                  = var.allowed_ips
}

module "storage" {
  source              = "./Modules/Storage"
  resource_group_name = azurerm_resource_group.webapp_rg.name
  location            = azurerm_resource_group.webapp_rg.location
  allowed_ips         = var.allowed_ips
  m_spoke_subnet      = data.azurerm_subnet.main_subnet.id
  agent_2204_subnet   = var.agent_2204_subnet
  agent_prd_subnet    = var.agent_prd_subnet
  env_name            = local.env_name
  service_name        = local.service_name
  service_name_bess   = local.service_name_bess
  container_name      = local.container_name
  tags                = local.tags
}

module "key_vault" {
  source              = "./Modules/KeyVault"
  name                = local.key_vault_name
  resource_group_name = azurerm_resource_group.webapp_rg.name
  env_name            = local.env_name
  tenant_id           = module.webapp_service.web_app_tenant_id
  allowed_ips         = var.allowed_ips
  allowed_subnet_ids  = [data.azurerm_subnet.main_subnet.id, var.agent_2204_subnet, var.agent_prd_subnet]
  location            = azurerm_resource_group.webapp_rg.location
  read_access_objects = {
     "webapp_service" = module.webapp_service.web_app_object_id
  }
  secrets = {
      "EventHubLoggingConfiguration--ConnectionString"       = module.eventhub.log_primary_connection_string
      "EventHubLoggingConfiguration--EntityPath"             = module.eventhub.entity_path
      "ApplicationInsights--ConnectionString"                = module.app_insights.connection_string
      "BessStorageConfiguration--ConnectionString"           = module.storage.bess_storage_connection_string
      "AzureWebJobsStorage"                                  = module.storage.bess_storage_connection_string
      "PKSApiConfiguration--PermitDecryptionHardwareId"      = var.permitdecryptionhardwareid
 }
  tags                                                       = local.tags
}
