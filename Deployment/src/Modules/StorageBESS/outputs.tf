output bess_storage_connection_string {
  value = azurerm_storage_account.bess_storage.primary_connection_string
  sensitive = true
}
