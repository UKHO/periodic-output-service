resource "azurerm_storage_account" "pos_storage" {
  name                              = lower("${var.service_name}${var.env_name}storageukho")
  resource_group_name               = var.resource_group_name
  location                          = var.location
  account_tier                      = "Standard"
  account_replication_type          = "LRS"
  account_kind                      = "StorageV2"
  
  tags                              = var.tags
    network_rules {
    default_action                 = "Deny"
    ip_rules                       = var.allowed_ips
    bypass                         = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids     = [var.m_spoke_subnet, var.agent_2204_subnet, var.agent_prd_subnet]
}
}

resource "azurerm_storage_account" "bess_storage" {
  name                              = lower("${var.service_name_bess}${var.env_name}storageukho")
  resource_group_name               = var.resource_group_name
  location                          = var.location
  account_tier                      = "Standard"
  account_replication_type          = "LRS"
  account_kind                      = "StorageV2"
  
  tags                              = var.tags
    network_rules {
    default_action                 = "Deny"
    ip_rules                       = var.allowed_ips
    bypass                         = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids     = [var.m_spoke_subnet,var.mock_spoke_subnet,var.agent_2204_subnet, var.agent_prd_subnet]
}
}

resource "azurerm_storage_account" "bess_storage" {
  name                              = lower("${var.service_name_bess}${var.env_name}storageukho")
  resource_group_name               = var.resource_group_name
  location                          = var.location
  account_tier                      = "Standard"
  account_replication_type          = "LRS"
  account_kind                      = "StorageV2"
  
  tags                              = var.tags
    network_rules {
    default_action                 = "Deny"
    ip_rules                       = var.allowed_ips
    bypass                         = ["Logging", "Metrics", "AzureServices"]
    virtual_network_subnet_ids     = [var.m_spoke_subnet,var.mock_spoke_subnet,var.agent_subnet]
}
}

resource "azurerm_storage_container" "bess_config_container" {
  name                  = var.container_name
  storage_account_name  = azurerm_storage_account.bess_storage.name  
}
