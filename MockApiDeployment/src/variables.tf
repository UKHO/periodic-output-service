variable "location" {
  type    = string
  default = "uksouth"
}


locals {
  env_name				= lower(terraform.workspace)
  service_name			= "posft"
  tags = {
    SERVICE          = "test"
    ENVIRONMENT      = "functionaltest-${local.env_name}"
    SERVICE_OWNER    = "UKHO"
    RESPONSIBLE_TEAM = "Mastek"
    CALLOUT_TEAM     = "On-Call_N/A"
    COST_CENTRE      = "P.431"
  }
}
