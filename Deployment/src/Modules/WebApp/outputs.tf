output "web_app_object_id" {
  value = azurerm_windows_web_app.webapp_service.identity.0.principal_id
}

output "web_app_tenant_id" {
  value = azurerm_windows_web_app.webapp_service.identity.0.tenant_id
}

output "default_site_hostname" {
  value = azurerm_windows_web_app.webapp_service.default_hostname
}

output "username" {
  value = azurerm_windows_web_app.webapp_service.site_credential[0].name
}

output "password" {
  value = azurerm_windows_web_app.webapp_service.site_credential[0].password
  sensitive = true
}