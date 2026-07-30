terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=4.81.0"
    }
  }

  required_version = "=1.15.8"
  backend "azurerm" {
    container_name = "tfstate"
    key            = "posterraform.deployment.tfplan"
  }
}

provider "azurerm" {
  features {}
}

provider "azurerm" {
  features {}
  alias = "build_agent"
  subscription_id = var.agent_subscription_id
}
