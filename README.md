# BeyondTrust Secret Safe Terraform Provider

A Terraform provider for [BeyondTrust Secret Safe](https://www.beyondtrust.com/secrets-safe), implemented in **C# (.NET 10)** using **Native AOT** compilation.

## Features

- 🔐 Retrieve secrets from BeyondTrust Secret Safe
- 📦 Native AOT compiled for minimal dependencies (~15 MB)
- 🔄 Full support for Terraform Plugin Protocol v5.2
- 🛡️ mTLS with self-signed certificates
- ⚡ Fast, lightweight binary

## Requirements

- Terraform >= 0.12
- BeyondTrust Secret Safe instance with API access

## Installation

```hcl
terraform {
  required_providers {
    secretsafe = {
      source = "beyondtrust/secretsafe"
    }
  }
}
```

## Usage

```hcl
provider "secretsafe" {
  key     = var.api_key
  runas   = var.service_account
  baseUrl = var.secret_safe_url
}

data "secretsafe_credential_data" "example" {
  secret_id = "2e22e1b1-d5c2-4a17-bc90-1234567890ab"
}

output "username" {
  value = data.secretsafe_credential_data.example.username
}

output "password" {
  value     = data.secretsafe_credential_data.example.password
  sensitive = true
}
```

## Data Sources

### `secretsafe_credential_data`

Retrieves username and password from a Secret Safe secret.

**Arguments:**
- `secret_id` (Required) - UUID of the secret in BeyondTrust Secret Safe

**Attributes:**
- `username` - Username from the secret
- `password` - Password from the secret (sensitive)
- `secret_id` - The secret UUID

## Development

See [CLAUDE.md](./CLAUDE.md) for development instructions.

## License

Mozilla Public License 2.0 - see [LICENSE](./LICENSE)
