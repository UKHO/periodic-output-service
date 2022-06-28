output "fm_mock_webapp" {
  value = azurerm_windows_web_app.fm_mock_webapp.name
}

output "web_app_object_id_fm_mock" {
  value = azurerm_windows_web_app.fm_mock_webapp.identity.0.principal_id
}

output "default_site_hostname_fm_mock" {
  value = azurerm_windows_web_app.fm_mock_webapp.default_hostname
}



