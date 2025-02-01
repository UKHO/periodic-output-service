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

resource "azurerm_storage_table" "aio_config_table" {
  name                  = var.table_name
  storage_account_name  = azurerm_storage_account.pos_storage.name
}

resource "azurerm_storage_table_entity" "aio_weekly" {
  storage_table_id = azurerm_storage_table.aio_config_table.id
  partition_key    = var.aio_weekly_configuration["job_name"]
  row_key          = var.aio_weekly_configuration["job_id"]

  entity = {
            BusinessUnit    = var.aio_weekly_configuration["business_unit"]
            ReadUsers       = var.aio_weekly_configuration["read_users"]
            ReadGroups      = var.aio_weekly_configuration["read_group"]
            IsEnabled       = var.aio_weekly_configuration["is_enabled"]
    }
}

resource "azurerm_storage_table_entity" "aio_printing" {
storage_table_id = azurerm_storage_table.aio_config_table.id
partition_key    = var.aio_printing_configuration["job_name"]
row_key          = var.aio_printing_configuration["job_id"]

entity = {
            BusinessUnit    = var.aio_printing_configuration["business_unit"]
            ReadUsers       = var.aio_printing_configuration["read_users"]
            ReadGroups      = var.aio_printing_configuration["read_group"]
            IsEnabled       = var.aio_printing_configuration["is_enabled"]
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
    virtual_network_subnet_ids     = compact([var.m_spoke_subnet, var.mock_spoke_subnet, var.agent_2204_subnet, var.agent_prd_subnet])
}
}

resource "azurerm_storage_container" "bess_config_container" {
  name                  = var.container_name
  storage_account_name  = azurerm_storage_account.bess_storage.name  
}

