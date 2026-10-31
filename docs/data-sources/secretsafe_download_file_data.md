# Data Source: secretsafe_download_file_data

Download file content from BeyondTrust Secret Safe as base64-encoded data.

~> **Warning:** Terraform stores the file content in the state in plain text. With Terraform 1.10 or later, prefer the [`secretsafe_download_file_data` ephemeral resource](../ephemeral-resources/secretsafe_download_file_data.md) wherever the value can be ephemeral.

## Example

```hcl
data "secretsafe_download_file_data" "example" {
  secret_id = "12345678-1234-1234-1234-123456789abc"
}

output "file_name" {
  value = data.secretsafe_download_file_data.example.file_name
}

output "file_content_base64" {
  value     = data.secretsafe_download_file_data.example.file_content_base64
  sensitive = true
}

# Decode base64 to get the actual file content
locals {
  file_content = base64decode(data.secretsafe_download_file_data.example.file_content_base64)
}
```

## Arguments

- `secret_id` - (Required, String) The UUID of the secret file in BeyondTrust Secret Safe

## Attributes

- `secret_id` - (String) The secret ID
- `file_name` - (String) The name of the file as stored in Secret Safe
- `file_content_base64` - (String, Sensitive) The file content encoded as base64 (base64 encoding is used because Terraform does not support binary values)
- `title` - (String) The title of the file secret
- `description` - (String) The description of the file secret
- `folder_id` - (String) The ID of the parent folder
- `file_hash` - (String) SHA-256 hash of the file content
- `owner_id` - (Number) ID of the secret owner
- `created_on` - (String) Timestamp when the file was created (RFC3339 format)
- `created_by` - (String) User who created the file secret
- `modified_on` - (String) Timestamp when the file was last modified (RFC3339 format)
- `modified_by` - (String) User who last modified the file secret
- `owner` - (String) Name of the secret owner
- `folder_path` - (String) Full path to the parent folder
- `owner_type` - (String) Type of owner (e.g., "User", "Group")
- `notes` - (String) Additional notes about the file secret
