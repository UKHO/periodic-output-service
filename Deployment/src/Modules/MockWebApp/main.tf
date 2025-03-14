resource "azurerm_windows_web_app" "mock_webapp_service" {
  name                = var.name
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = var.service_plan_id
  tags                = var.tags

  site_config {
    application_stack {    
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    always_on  = true
    ftps_state = "Disabled"

    ip_restriction {
      virtual_network_subnet_id = var.subnet_id
    }

    ip_restriction {
      virtual_network_subnet_id = var.main_subnet_id
    }

    dynamic "ip_restriction" {
      for_each = var.allowed_ips
      content {
          ip_address  = length(split("/",ip_restriction.value)) > 1 ? ip_restriction.value : "${ip_restriction.value}/32"
      }
    }
  }

  app_settings = var.app_settings

  identity {
    type = "SystemAssigned"
  }

  lifecycle {
    ignore_changes = [ virtual_network_subnet_id ]
  }

  https_only = true
} 

resource "azurerm_app_service_virtual_network_swift_connection" "mock_webapp_vnet_integration" {
  app_service_id = azurerm_windows_web_app.mock_webapp_service.id
  subnet_id      = var.subnet_id
}

resource "azurerm_windows_web_app" "pks_mock_webapp_service" {
  name                = var.pks_name
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = var.service_plan_id
  tags                = var.tags

  site_config {
    application_stack {    
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    always_on  = true
    ftps_state = "Disabled"

    ip_restriction {
      virtual_network_subnet_id = var.subnet_id
    }

    ip_restriction {
      virtual_network_subnet_id = var.main_subnet_id
    }

    dynamic "ip_restriction" {
      for_each = var.allowed_ips
      content {
          ip_address  = length(split("/",ip_restriction.value)) > 1 ? ip_restriction.value : "${ip_restriction.value}/32"
      }
    }
  }

  app_settings = var.app_settings

  identity {
    type = "SystemAssigned"
  }

  lifecycle {
    ignore_changes = [ virtual_network_subnet_id ]
  }

  https_only = true
} 

resource "azurerm_app_service_virtual_network_swift_connection" "pks_mock_webapp_vnet_integration" {
  app_service_id = azurerm_windows_web_app.pks_mock_webapp_service.id
  subnet_id      = var.subnet_id
}