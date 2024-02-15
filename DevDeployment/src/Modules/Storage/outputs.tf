output pos_storage_name {
  value = azurerm_storage_account.pos_storage.name
}

output pos_storage_connection_string {
  value = azurerm_storage_account.pos_storage.primary_connection_string
  sensitive = true
}

output bess_configuration_storage_connection_string {
  value = azurerm_storage_account.bess_configuration_storage.primary_connection_string
  sensitive = true
}
