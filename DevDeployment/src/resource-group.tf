resource "azurerm_resource_group" "webapp_rg" {
  name     = "${local.service_name}-${local.env_name}-webapp-rg"
  location = var.location
  tags     = local.tags
}

resource "azurerm_resource_group" "mock_webapp_rg" {
  name     = "${local.service_name}-${local.env_name}-mock-webapp-rg"
  location = var.location
  tags     = local.tags
}


