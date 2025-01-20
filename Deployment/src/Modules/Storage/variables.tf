
variable "resource_group_name" {
  type = string
}

variable "location" {
  type = string
}

variable "env_name" {
  type  = string
}

variable "tags" {
}

variable "service_name" {
  type = string
}

variable "service_name_bess" {
  type = string
}

variable "container_name" {
  type = string
}

variable "m_spoke_subnet" {
  type = string
}

variable "mock_spoke_subnet" {
  type = string
}

variable "allowed_ips" {
}

variable "agent_2204_subnet" {
  type = string
}

variable "agent_prd_subnet" {
  type = string
}

variable "table_name" {
  type = string
  default = "aiojobconfiguration"
}

variable "is_enabled" {
  type = string
  default = true
}

variable "aio_weekly_configuration" {
  type = map(string)
}

variable "aio_printing_configuration" {
  type = map(string)
}
