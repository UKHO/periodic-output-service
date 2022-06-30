output "mock_webappname" {
value = module.webapp_service.fm_mock_webapp
}

output "fm_mock_web_app_url" {
value = "https://${module.webapp_service.default_site_hostname_fm_mock}"
}

output "webapp_rg" {
value = azurerm_resource_group.webapp_rg.name
}




