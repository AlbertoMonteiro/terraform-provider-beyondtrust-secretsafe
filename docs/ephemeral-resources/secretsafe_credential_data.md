# Ephemeral Resource: secretsafe_credential_data

Retrieve username and password credentials from BeyondTrust Secret Safe without storing them in the Terraform state or plan.

Requires Terraform 1.10 or later.

## Example

```hcl
ephemeral "secretsafe_credential_data" "database" {
  secret_id = "12345678-1234-1234-1234-123456789abc"
}

# In another provider's configuration
provider "postgresql" {
  username = ephemeral.secretsafe_credential_data.database.username
  password = ephemeral.secretsafe_credential_data.database.password
}

# In a write-only argument, which needs Terraform 1.11 or later
resource "aws_db_instance" "example" {
  username            = "admin"
  password_wo         = ephemeral.secretsafe_credential_data.database.password
  password_wo_version = 1
}
```

Ephemeral values can only be used where Terraform accepts them, such as write-only arguments, `provider` blocks, `locals`, other `ephemeral` blocks and ephemeral variables or child module outputs. They cannot be used in root module outputs or ordinary resource arguments. For those, use the [`secretsafe_credential_data` data source](../data-sources/secretsafe_credential_data.md), which stores the value in the state.

## Arguments

- `secret_id` - (Required, String) The UUID of the secret in BeyondTrust Secret Safe

## Attributes

- `secret_id` - (String) The secret ID
- `username` - (String) The username from the credential secret
- `password` - (String, Sensitive) The password from the credential secret
