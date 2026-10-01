# swift
removed {
  from = module.webapp_service.azurerm_app_service_virtual_network_swift_connection.webapp_vnet_integration

  lifecycle {
    destroy = false
  }
}

# IAT recovery only
#
# aiojobconfiguration exists in Azure but was lost from Terraform state
# during the previous failed module migration.
import {
for_each = local.env_name == "iat" ? toset(["iat"]) : toset([])
 
to = module.storagePOS.azurerm_storage_table.aio_config_table

id = "https://${local.storage_account_pos_name}.table.core.windows.net/Tables('${var.aio_config_table_name}')"
}

#storage POS and BESS
moved {
  from = module.storage.azurerm_storage_account.pos_storage
  to   = module.storagePOS.azurerm_storage_account.pos_storage
}

moved {
  from = module.storage.azurerm_storage_table.aio_config_table
  to   = module.storagePOS.azurerm_storage_table.aio_config_table
}

moved {
  from = module.storage.azurerm_storage_account.bess_storage
  to   = module.storageBESS.azurerm_storage_account.bess_storage
}

moved {
  from = module.storage.azurerm_storage_container.bess_config_container
  to   = module.storageBESS.azurerm_storage_container.bess_config_container
}
