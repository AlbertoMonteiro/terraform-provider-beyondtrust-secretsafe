# Data Source: secretsafe_credential_data

Retrieve credential data from BeyondTrust Secret Safe.

## Example

```hcl
data "secretsafe_credential_data" "example" {
  # Configuration
}

output "username" {
  value = data.secretsafe_credential_data.example.username
}

output "password" {
  value     = data.secretsafe_credential_data.example.password
  sensitive = true
}
```

## Arguments

- `id` - (Required) The credential ID in BeyondTrust Secret Safe

## Attributes

- `username` - The credential username
- `password` - The credential password
- `domain` - The credential domain (if applicable)
