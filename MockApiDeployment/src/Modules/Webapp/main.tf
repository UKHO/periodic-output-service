resource "random_string" "unique_string" {
  length  = 5
  special = false
  upper   = false
}

resource "azurerm_windows_web_app" "fm_mock_webapp" {
  name                = "${var.service_name}-${var.env_name}-mock-${random_string.unique_string.result}-webapp"
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = var.service_plan_id
  tags                = var.tags

  site_config {
     application_stack {    
     current_stack  = "dotnet"
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


