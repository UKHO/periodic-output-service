data "azurerm_app_service_plan" "essft_asp" {
  name                = "essft-${local.env_name}-yh3r1-asp"
  resource_group_name = "essft-${local.env_name}-webapp-rg"
}

module "webapp_service" {
  source              = "./Modules/Webapp"
  service_name        = local.service_name
  env_name            = local.env_name
  resource_group_name = azurerm_resource_group.webapp_rg.name
  service_plan_id     = data.azurerm_app_service_plan.essft_asp.id
  location            = azurerm_resource_group.webapp_rg.location
  app_settings = {
    "ASPNETCORE_ENVIRONMENT"                               = local.env_name
    "WEBSITE_RUN_FROM_PACKAGE"                             = "1"
    "WEBSITE_ENABLE_SYNC_UPDATE_SITE"                      = "true"
    "APPINSIGHTS_INSTRUMENTATIONKEY"                       = "NOT_CONFIGURED"
  }
  tags = local.tags

}


 
