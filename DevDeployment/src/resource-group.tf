resource "azurerm_resource_group" "webapp_rg" {
  name     = "${local.service_name}-${local.env_name}-rg"
  location = var.location
  tags     = local.tags
}

resource "azurerm_resource_group" "mock_webapp_rg" {
  name     = "${local.service_name}-${local.env_name}-mock-rg"
  location = var.location
  tags     = local.tags
}


