
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

variable "subnet_id" {
  type = string
}

variable "allowed_ips" {
}

variable "agent_subnet" {
  type = string
}
