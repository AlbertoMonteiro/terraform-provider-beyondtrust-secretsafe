# BeyondTrust Secret Safe Provider

The BeyondTrust Secret Safe provider is used to interact with BeyondTrust Secret Safe secrets.

## Authentication

Configure the provider with your Secret Safe credentials:

```hcl
provider "secretsafe" {
  # Configuration options
}
```

## Example

```hcl
terraform {
  required_providers {
    secretsafe = {
      source = "albertomonteiro/beyondtrust-secretsafe"
    }
  }
}

provider "secretsafe" {
  # Configure provider
}

data "secretsafe_credential_data" "example" {
  # Data source configuration
}

output "username" {
  value = data.secretsafe_credential_data.example.username
}

output "password" {
  value     = data.secretsafe_credential_data.example.password
  sensitive = true
}
```
