output storage_account_name {
  value = azurerm_storage_account.pos_storage.name
}

output storage_connection_string {
  value = azurerm_storage_account.pos_storage.primary_connection_string
  sensitive = true
}



