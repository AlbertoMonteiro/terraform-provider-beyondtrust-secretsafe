# Data Source: secretsafe_credential_data

Retrieve username and password credentials from BeyondTrust Secret Safe.

## Example

```hcl
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

resource "aws_db_instance" "example" {
  username = data.secretsafe_credential_data.example.username
  password = data.secretsafe_credential_data.example.password
}
```

## Arguments

- `secret_id` - (Required, String) The UUID of the secret in BeyondTrust Secret Safe

## Attributes

- `secret_id` - (String) The secret ID
- `username` - (String) The username from the credential secret
- `password` - (String, Sensitive) The password from the credential secret
