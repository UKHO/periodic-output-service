resource "azurerm_windows_web_app" "mock_webapp_service" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = var.service_plan_id
  tags                = var.tags

  site_config {
     application_stack {    
     current_stack = "dotnet"
     dotnet_version = "v6.0"
    }
    always_on  = true
    ftps_state = "Disabled"

  }

  app_settings = var.app_settings

  identity {
    type = "SystemAssigned"
  }

  https_only = true
} 
