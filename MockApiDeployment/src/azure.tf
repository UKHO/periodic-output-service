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
    key            = "posmockapiterraform.deployment.tfplan"
  }
}

provider "azurerm" {
  features {}
}

