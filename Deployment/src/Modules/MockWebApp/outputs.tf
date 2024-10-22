output "web_app_object_id" {
  value = azurerm_app_service.mock_webapp_service.identity.0.principal_id
}

output "web_app_tenant_id" {
  value = azurerm_app_service.mock_webapp_service.identity.0.tenant_id
}

output "default_site_hostname" {
  value = azurerm_app_service.mock_webapp_service.default_hostname
}
