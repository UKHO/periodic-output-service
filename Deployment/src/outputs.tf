output "mock_webappname" {
  value = local.mock_web_app_name
}

output "pks_mock_webappname" {
  value = local.pks_mock_web_app_name
}

output "fm_mock_web_app_url" {
value = length(module.mock_webapp_service) > 0 ? "https://${module.mock_webapp_service[0].default_site_hostname}" : null
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

output "pos_storage_name" {
value = module.storage.pos_storage_name
}

output pos_storage_access_key {
  value = module.storage.pos_storage_access_key
  sensitive = true
}

output "pos_storage_connection_string"{
  value = module.storage.pos_storage_connection_string
  sensitive = true
}

output "bess_storage_connection_string"{
  value = module.storage.bess_storage_connection_string
  sensitive = true
}

output "log_primary_connection_string" {
  value     = module.eventhub.log_primary_connection_string
  sensitive = true
}

output "entity_path" {
  value = module.eventhub.entity_path
  sensitive = true
}

output "webjob_username" {
  value     = module.webapp_service.username
  sensitive = true
}

output "webjob_password" {
  value = module.webapp_service.password
  sensitive = true
}

output "connection_string" {
  value = module.app_insights.connection_string
  sensitive = true
}

output "keyvault_uri"{
  value = module.key_vault.keyvault_uri
}
