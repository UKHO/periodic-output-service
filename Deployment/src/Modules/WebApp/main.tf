resource "azurerm_windows_web_app" "webapp_service" {
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
    use_32_bit_worker = false

    ip_restriction {
      virtual_network_subnet_id = var.subnet_id
    }

    dynamic "ip_restriction" {
      for_each = var.allowed_ips
      content {
          ip_address  = length(split("/",ip_restriction.value)) > 1 ? ip_restriction.value : "${ip_restriction.value}/32"
      }
    }
  }

  app_settings = var.app_settings

  sticky_settings {
    app_setting_names = [ "WEBJOBS_STOPPED" ]
  }

  identity {
    type = "SystemAssigned"
  }

  https_only                = true
  virtual_network_subnet_id = var.subnet_id
}

resource "azurerm_windows_web_app_slot" "staging" {
  name                = var.slot_name
  app_service_id      = azurerm_windows_web_app.webapp_service.id
  tags                = azurerm_windows_web_app.webapp_service.tags

  site_config {
    application_stack {    
      current_stack = "dotnet"
      dotnet_version = "v8.0"
    }
    always_on  = true
    ftps_state = "Disabled"
    use_32_bit_worker = false

    ip_restriction {
      virtual_network_subnet_id = var.subnet_id
    }

    dynamic "ip_restriction" {
      for_each = var.allowed_ips
      content {
          ip_address  = length(split("/",ip_restriction.value)) > 1 ? ip_restriction.value : "${ip_restriction.value}/32"
      }
    }
  }

  app_settings = merge(azurerm_windows_web_app.webapp_service.app_settings, { "WEBJOBS_STOPPED" = "1" })

  identity {
    type = "SystemAssigned"
  }

  https_only                = azurerm_windows_web_app.webapp_service.https_only
  virtual_network_subnet_id = var.subnet_id
}
