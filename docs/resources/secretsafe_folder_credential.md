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
  password    = var.db_password
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

- `folder_id` - (Required, String) The ID of the folder where the credential will be stored.
- `title` - (Required, String) A descriptive name for the credential (e.g., "Production DB Admin").
- `description` - (Optional, String) Additional details about what this credential is used for.
- `username` - (Required, String) The username for the credential.
- `password` - (Required, String, Sensitive) The password for the credential. **Marked as sensitive to prevent accidental exposure in logs.**
- `owner_id` - (Optional, Number) The ID of the user who owns this credential (auto-populated from authenticated user if not provided).
- `owners` - (Optional, List) A list of additional owner IDs. Each entry should contain `owner_id`.

## Attributes

- `id` - (String, Computed) The unique identifier of the credential in Secret Safe.
- `folder_id` - (String) Parent folder UUID
- `title` - (String) The secret title
- `description` - (String) The secret description
- `username` - (String) The username
- `password` - (String, Sensitive) The password
- `owner_id` - (Number, Computed) ID of the secret owner (auto-populated from authenticated user)
- `created_on` - (String, Computed) Timestamp when the credential was created (RFC3339 format)
- `created_by` - (String, Computed) User who created the credential
- `modified_on` - (String, Computed) Timestamp when the credential was last modified (RFC3339 format)
- `modified_by` - (String, Computed) User who last modified the credential
- `owner` - (String, Computed) Name of the secret owner
- `folder_path` - (String, Computed) Full path to the parent folder
- `owner_type` - (String, Computed) Type of owner (e.g., "User", "Group")
- `notes` - (String, Computed) Additional notes about the credential

## Sensitive Attributes

The `password` attribute is marked as sensitive and will be redacted from logs and state files.

## Example with Multiple Owners

```hcl
resource "secretsafe_folder_credential" "shared_credential" {
  folder_id   = secretsafe_folder.credentials.id
  title       = "Shared Service Account"
  description = "Shared credentials for CI/CD pipeline"
  username    = "ci_service_account"
  password    = var.shared_password
}
```

## Notes

- Passwords should be managed carefully. Consider using Terraform variables and `.tfvars` files to keep sensitive data out of version control.
- The `owner_id` field is required and must correspond to a valid user ID in BeyondTrust Secret Safe.
- Credential titles must be unique within their parent folder.
