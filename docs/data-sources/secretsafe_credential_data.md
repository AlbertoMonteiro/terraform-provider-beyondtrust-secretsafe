# Data Source: secretsafe_credential_data

Retrieve username and password credentials from BeyondTrust Secret Safe.

~> **Warning:** Terraform stores the password in the state in plain text. With Terraform 1.10 or later, prefer the [`secretsafe_credential_data` ephemeral resource](../ephemeral-resources/secretsafe_credential_data.md) wherever the value can be ephemeral.

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
- `title` - (String) The title of the credential
- `description` - (String) The description of the credential
- `folder_id` - (String) The ID of the parent folder
- `owner_id` - (Number) ID of the secret owner
- `created_on` - (String) Timestamp when the credential was created (RFC3339 format)
- `created_by` - (String) User who created the credential
- `modified_on` - (String) Timestamp when the credential was last modified (RFC3339 format)
- `modified_by` - (String) User who last modified the credential
- `owner` - (String) Name of the secret owner
- `folder_path` - (String) Full path to the parent folder
- `owner_type` - (String) Type of owner (e.g., "User", "Group")
- `notes` - (String) Additional notes about the credential
