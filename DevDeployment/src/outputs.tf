output "mock_webappname" {
  value = local.mock_web_app_name
}

output "fm_mock_web_app_url" {
value = "https://${module.mock_webapp_service.default_site_hostname}"
}

output "mock_webapp_rg" {
value = azurerm_resource_group.mock_webapp_rg.name
}

output "web_app_name" {
  value = local.web_app_name
}

output "env_name" {
  value = local.env_name
}

output "webapp_rg" {
  value = azurerm_resource_group.webapp_rg.name
}

output "Website_Url" {
  value = "https://${module.webapp_service.default_site_hostname}/"
}

output "kv_name" {
  value = local.key_vault_name
}
