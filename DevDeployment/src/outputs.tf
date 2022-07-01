output "mock_webappname" {
value = module.mock_webapp_service.fm_mock_webapp
}

output "fm_mock_web_app_url" {
value = "https://${module.mock_webapp_service.default_site_hostname_fm_mock}"
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
  value = azurerm_resource_group.rg.name
}

output "Website_Url" {
  value = "https://${module.webapp_service.default_site_hostname}/"
}

