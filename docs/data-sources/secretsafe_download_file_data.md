# Data Source: secretsafe_download_file_data

Download file content from BeyondTrust Secret Safe as base64-encoded data.

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
