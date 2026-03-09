# BeyondTrust Secret Safe Provider

Terraform provider for **BeyondTrust Secret Safe** using **.NET Native AOT**, **Slim Builder**, and **Terraform Plugin Protocol v5.2**.

## Overview

The BeyondTrust Secret Safe provider allows you to manage and retrieve secrets from BeyondTrust Secret Safe in your Terraform configurations.

## Supported Data Sources

- `secretsafe_credential_data` — Retrieve username/password credentials from a secret
- `secretsafe_download_file_data` — Download file content from a secret as base64

## Supported Resources

- `secretsafe_folder` — Create, read, update, and delete folders in Secret Safe
- `secretsafe_folder_credential` — Create and manage credentials within Secret Safe folders
- `secretsafe_folder_file` — Upload and manage files within Secret Safe folders

## Provider Configuration

The provider requires the following arguments:

- `runas` - (Required, String) User to authenticate in BeyondTrust Secret Safe
- `key` - (Required, String, Sensitive) The API key of BeyondTrust Secret Safe
- `baseUrl` - (Required, String) Base URL of BeyondTrust Secret Safe instance

## Example Usage

```hcl
terraform {
  required_providers {
    secretsafe = {
      source = "albertomonteiro/beyondtrust-secretsafe"
    }
  }
}

provider "secretsafe" {
  runas   = "myuser"
  key     = var.secret_safe_api_key
  baseUrl = "https://secretsafe.example.com"
}

# Retrieve credential data
data "secretsafe_credential_data" "example" {
  secret_id = "12345678-1234-1234-1234-123456789abc"
}

output "username" {
  value = data.secretsafe_credential_data.example.username
}

output "password" {
  value     = data.secretsafe_credential_data.example.password
  sensitive = true
}

# Download file content
data "secretsafe_download_file_data" "example" {
  secret_id = "87654321-4321-4321-4321-abcdefgh1234"
}

output "file_content" {
  value     = data.secretsafe_download_file_data.example.file_content_base64
  sensitive = true
}
```
