resource "azurerm_storage_account" "pos_storage" {
  name                              = lower("${var.service_name}${var.env_name}storageukho")
  resource_group_name               = var.resource_group_name
  location                          = var.location
  account_tier                      = "Standard"
  account_replication_type          = "LRS"
  account_kind                      = "StorageV2"
  
  tags                              = var.tags
}
