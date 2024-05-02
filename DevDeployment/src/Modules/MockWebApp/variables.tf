variable "name" {
  type = string
}

variable "pks_name" {
  type = string
}

variable "resource_group_name" {
  type = string
}

variable "service_plan_id" {
  type = string
}

variable "location" {
  type = string
}

variable "app_settings" {
  type = map(string)
}

variable "tags" {

}

variable "env_name" {
  type = string
}

variable "subnet_id" {
  type = string
}

variable "main_subnet_id" {
  type = string
}

variable "allowed_ips" {

}
