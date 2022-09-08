variable "location" {
  type    = string
  default = "uksouth"
}
variable "resource_group_name" {
  type    = string
  default = "pos"
}

locals {
  env_name           = lower(terraform.workspace)
  service_name       = "pos"
  web_app_name       = "${local.service_name}-${local.env_name}-lxs-webapp"
  mock_web_app_name  = "${local.service_name}-${local.env_name}-mock-webapp"
  key_vault_name     = "${local.service_name}-ukho-${local.env_name}-kv"

  tags = {
    SERVICE                   = "Periodic Output Service"
    ENVIRONMENT               = local.env_name
    SERVICE_OWNER             = "UKHO"
    RESPONSIBLE_TEAM          = "Mastek"
    CALLOUT_TEAM              = "On-Call_N/A"
    COST_CENTRE               = "A.008.02"
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

variable "mock_spoke_subnet_name" {
  type = string
}

variable "agent_rg" {
  type = string
}

variable "agent_vnet_name" {
  type = string
}

variable "agent_subnet_name" {
  type = string
}

variable "agent_subscription_id" {
  type = string
}

variable "allowed_ips" {
  type = list
}
