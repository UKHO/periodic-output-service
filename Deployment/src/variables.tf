variable "location" {
  type    = string
  default = "uksouth"
}

variable "resource_group_name" {
  type    = string
  default = "pos"
}

locals {
  env_name               = lower(terraform.workspace)
  service_name           = "pos"
  web_app_name           = "${local.service_name}-${local.env_name}-lxs-webapp"
  web_app_slot_name      = "staging"
  mock_web_app_name      = "${local.service_name}-${local.env_name}-mock-webapp"
  pks_mock_web_app_name  = "${local.service_name}-${local.env_name}-pks-mock-webapp"
  key_vault_name         = "${local.service_name}-ukho-${local.env_name}-kv"
  service_name_bess      = "bess"
  container_name         = "bess-configs"

  tags = {
    SERVICE                   = "Periodic Output Service"
    ENVIRONMENT               = local.env_name
    SERVICE_OWNER             = "UKHO"
    RESPONSIBLE_TEAM          = "Abzu"
    CALLOUT_TEAM              = "On-Call_N/A"
    COST_CENTRE               = local.env_name == "dev" || local.env_name == "qa" || local.env_name == "prod" ? "A.008.02" : "A.011.08"
  }
}

variable "spoke_rg" {
  type = string
}

variable "spoke_vnet_name" {
  type = string
}

variable "spoke_subnet_name" {
  type = string
}

variable "agent_rg" {
  type = string
}

variable "agent_vnet_name" {
  type = string
}

variable "mock_spoke_subnet_name" {
  type = string
}

variable "agent_subscription_id" {
  type = string
}

variable "allowed_ips" {
  type = list
}

variable "elastic_apm_server_url" {
}

variable "elastic_apm_api_key" {
}

variable "permitdecryptionhardwareid" {
  type = string
}

variable "agent_2204_subnet" {
  type = string
}

variable "agent_prd_subnet" {
  type = string
}

variable "BessContainerName" {
  type = string
}

variable "aio_config_table_name" {
  type = string
  default = "aiojobconfiguration"
}
