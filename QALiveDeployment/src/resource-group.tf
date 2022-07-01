resource "azurerm_resource_group" "webapp_rg" {
  name     = "${var.resource_group_name}-${local.env_name}-rg"
  location = var.location
  tags     = local.tags
}
