# Resource: secretsafe_folder_credential

Create and manage username/password credentials within BeyondTrust Secret Safe folders.

## Example

```hcl
resource "secretsafe_folder" "credentials" {
  name          = "Database Credentials"
  user_group_id = 1
}

resource "secretsafe_folder_credential" "db_admin" {
  folder_id   = secretsafe_folder.credentials.id
  title       = "Production Database Admin"
  description = "Admin credentials for production PostgreSQL database"
  username    = "dbadmin"
  password    = "SecurePassword123!@#"
  owner_id    = 5
}

output "credential_id" {
  value     = secretsafe_folder_credential.db_admin.id
  sensitive = true
}

output "credential_username" {
  value = secretsafe_folder_credential.db_admin.username
}
```

## Arguments

- `folder_id` - (Required) The ID of the folder where the credential will be stored.
- `title` - (Required) A descriptive name for the credential (e.g., "Production DB Admin").
- `description` - (Optional) Additional details about what this credential is used for.
- `username` - (Required) The username for the credential.
- `password` - (Required, Sensitive) The password for the credential. **Marked as sensitive to prevent accidental exposure in logs.**
- `owner_id` - (Required) The ID of the user who owns this credential.
- `owners` - (Optional) A list of additional owner IDs. Each entry should contain `owner_id`.

## Attributes

- `id` - (Computed) The unique identifier of the credential in Secret Safe.

## Sensitive Attributes

The `password` attribute is marked as sensitive and will be redacted from logs and state files.

## Example with Multiple Owners

```hcl
resource "secretsafe_folder_credential" "shared_credential" {
  folder_id   = secretsafe_folder.credentials.id
  title       = "Shared Service Account"
  description = "Shared credentials for CI/CD pipeline"
  username    = "ci_service_account"
  password    = "ComplexPassword456!@#"
  owner_id    = 5
  owners = [
    {
      owner_id = 6
    },
    {
      owner_id = 7
    }
  ]
}
```

## Import

Credentials can be imported using the credential ID:

```bash
terraform import secretsafe_folder_credential.db_admin credential-id-here
```

## Notes

- Passwords should be managed carefully. Consider using Terraform variables and `.tfvars` files to keep sensitive data out of version control.
- The `owner_id` field is required and must correspond to a valid user ID in BeyondTrust Secret Safe.
- Credential titles must be unique within their parent folder.
