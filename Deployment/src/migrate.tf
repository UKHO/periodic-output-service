# swift
removed {
  from = module.webapp_service.azurerm_app_service_virtual_network_swift_connection.webapp_vnet_integration

  lifecycle {
    destroy = false
  }
}

#storage POS and BESS
removed {
  from = module.storage.azurerm_storage_account.pos_storage

  lifecycle {
    destroy = false
  }
}

import {
  to = module.storagePOS.azurerm_storage_account.pos_storage
  id = "${azurerm_resource_group.webapp_rg.id}/providers/Microsoft.Storage/storageAccounts/${lower("${local.service_name}${local.env_name}storageukho")}"
}

removed {
  from = module.storage.azurerm_storage_account.bess_storage

  lifecycle {
    destroy = false
  }
}

import {
  to = module.storageBESS.azurerm_storage_account.bess_storage
  id = "${azurerm_resource_group.webapp_rg.id}/providers/Microsoft.Storage/storageAccounts/${lower("${local.service_name_bess}${local.env_name}storageukho")}"
}
