# BeyondTrust Secret Safe Provider

Terraform provider for **BeyondTrust Secret Safe** using **.NET Native AOT**, **Slim Builder**, and **Terraform Plugin Protocol v5.10**.

## Overview

The BeyondTrust Secret Safe provider allows you to manage and retrieve secrets from BeyondTrust Secret Safe in your Terraform configurations.

## Supported Data Sources

- `secretsafe_credential_data` — Retrieve username/password credentials from a secret
- `secretsafe_download_file_data` — Download file content from a secret as base64

## Supported Ephemeral Resources

Same reads as the data sources, but the value stays out of the state and plan. Requires Terraform 1.10 or later.

- `secretsafe_credential_data` — Retrieve username/password credentials from a secret
- `secretsafe_download_file_data` — Download file content from a secret as base64

## Supported Resources

- `secretsafe_folder` — Create, read, update, and delete folders in Secret Safe
- `secretsafe_folder_credential` — Create and manage credentials within Secret Safe folders
- `secretsafe_folder_file` — Upload and manage files within Secret Safe folders

## Provider Configuration

The provider supports two authentication modes. Use one or the other: combining them is an error.

- `baseUrl` - (Required, String) Base URL of BeyondTrust Secret Safe instance

**OAuth client credentials:**

- `client_id` - (Optional, String, Sensitive) OAuth client ID of the API registration
- `client_secret` - (Optional, String, Sensitive) OAuth client secret of the API registration

```hcl
provider "secretsafe" {
  client_id     = var.secret_safe_client_id
  client_secret = var.secret_safe_client_secret
  baseUrl       = "https://secretsafe.example.com"
}
```

With OAuth, `owner_id` defaults to the user behind the OAuth client, read from the `_clientUserId` claim of the token.

**API key:**

- `runas` - (Optional, String) User to authenticate in BeyondTrust Secret Safe. Required with `key`
- `key` - (Optional, String, Sensitive) The API key of BeyondTrust Secret Safe. Required when `client_id` and `client_secret` are not set
- `pwd` - (Optional, String, Sensitive) Domain password for authentication (if using domain credentials)

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
  pwd     = var.secret_safe_password  # Optional: domain password
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

# Retrieve credential data without storing it in the state
ephemeral "secretsafe_credential_data" "example" {
  secret_id = "12345678-1234-1234-1234-123456789abc"
}

# Download file content
data "secretsafe_download_file_data" "example" {
  secret_id = "87654321-4321-4321-4321-abcdefgh1234"
}

output "file_content" {
  value     = data.secretsafe_download_file_data.example.file_content_base64
  sensitive = true
}

# Create and manage folders
resource "secretsafe_folder" "example" {
  name           = "My Secure Folder"
  description    = "Folder for storing secrets"
  user_group_id  = 1
}

# Create credentials in a folder
resource "secretsafe_folder_credential" "db_cred" {
  folder_id   = secretsafe_folder.example.id
  title       = "Database Admin"
  username    = "dbadmin"
  password    = var.db_password
  owner_id    = 5
}

# Upload files to a folder
resource "secretsafe_folder_file" "ssl_cert" {
  folder_id           = secretsafe_folder.example.id
  title               = "SSL Certificate"
  file_name           = "server.crt"
  file_content_base64 = filebase64("${path.module}/certs/server.crt")
  owner_id            = 5
}
```
