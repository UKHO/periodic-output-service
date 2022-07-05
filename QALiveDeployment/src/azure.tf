terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.1.0"
    }
  }

  required_version = "=1.1.9"
  backend "azurerm" {
    container_name = "tfstate"
    key            = "posterraform.deployment.tfplan"
  }
}

provider "azurerm" {
  features {

    resource_group {
       prevent_deletion_if_contains_resources = false
    }

  }
}

provider "azurerm" {
  features {}
  alias = "build_agent"
  subscription_id = var.agent_subscription_id
}
