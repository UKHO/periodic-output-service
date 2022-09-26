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

output "pos_storage_connection_string"{
  value = module.storage.pos_storage_connection_string
  sensitive = true
}

output "log_primary_connection_string" {
  value     = module.eventhub.log_primary_connection_string
  sensitive = true
}

output "entity_path" {
  value = module.eventhub.logstash_primary_connection_string
  sensitive = true
}

output "webjob_username" {
  value = module.webapp_service.username
}

output "webjob_password" {
  value = module.webapp_service.password
  sensitive = true
}
