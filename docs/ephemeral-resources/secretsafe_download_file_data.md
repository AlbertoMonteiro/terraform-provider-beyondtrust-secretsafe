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
