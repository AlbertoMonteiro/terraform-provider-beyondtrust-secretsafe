# Ephemeral Resource: secretsafe_download_file_data

Download file content from BeyondTrust Secret Safe as base64-encoded data without storing it in the Terraform state or plan.

Requires Terraform 1.10 or later.

## Example

```hcl
ephemeral "secretsafe_download_file_data" "certificate" {
  secret_id = "12345678-1234-1234-1234-123456789abc"
}

locals {
  certificate = base64decode(ephemeral.secretsafe_download_file_data.certificate.file_content_base64)
}
```

Ephemeral values can only be used where Terraform accepts them, such as write-only arguments, `provider` blocks, `locals`, other `ephemeral` blocks and ephemeral variables or child module outputs. They cannot be used in root module outputs or ordinary resource arguments. For those, use the [`secretsafe_download_file_data` data source](../data-sources/secretsafe_download_file_data.md), which stores the value in the state.

## Arguments

- `secret_id` - (Required, String) The UUID of the secret file in BeyondTrust Secret Safe

## Attributes

- `secret_id` - (String) The secret ID
- `file_name` - (String) The name of the file as stored in Secret Safe
- `file_content_base64` - (String, Sensitive) The file content encoded as base64 (base64 encoding is used because Terraform does not support binary values)
- `file_hash` - (String) Hash of the file content, as returned by Secret Safe
- `title` - (String) The title of the file secret
- `description` - (String) The description of the file secret
- `folder_id` - (String) The ID of the parent folder
- `owner_id` - (Number) ID of the secret owner
- `created_on` - (String) When the file secret was created, in RFC 3339 UTC as returned by Secret Safe (for example `2026-10-14T18:57:26.7Z`)
- `created_by` - (String) User who created the file secret
- `modified_on` - (String) When the file secret was last modified, in the same format as `created_on`
- `modified_by` - (String) User who last modified the file secret
- `owner` - (String) Name of the secret owner
- `folder_path` - (String) Full path to the parent folder
- `owner_type` - (String) Type of owner (e.g., "User", "Group")
- `notes` - (String) Additional notes about the file secret
