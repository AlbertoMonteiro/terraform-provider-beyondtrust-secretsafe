# Resource: secretsafe_folder_file

Upload and manage encrypted file secrets within BeyondTrust Secret Safe folders.

## Example

```hcl
resource "secretsafe_folder" "secure_files" {
  name          = "Encrypted Files"
  user_group_id = 1
}

resource "secretsafe_folder_file" "ssl_cert" {
  folder_id            = secretsafe_folder.secure_files.id
  title                = "Production SSL Certificate"
  description          = "SSL/TLS certificate for production web server"
  file_name            = "prod-server.crt"
  file_content_base64  = filebase64("${path.module}/certs/prod-server.crt")
}

output "file_secret_id" {
  value     = secretsafe_folder_file.ssl_cert.id
  sensitive = true
}
```

## Arguments

- `folder_id` - (Required, String) The ID of the folder where the file will be stored.
- `title` - (Required, String) A descriptive name for the file secret (e.g., "Production SSL Certificate").
- `description` - (Optional, String) Additional details about the file's purpose or content.
- `file_name` - (Required, String) The original name of the file (e.g., "certificate.crt", "backup.sql").
- `file_content_base64` - (Required, String, Sensitive) The file content encoded in base64. Use the `filebase64()` function or `base64encode()` to encode file contents.
- `owner_id` - (Optional, Number) The ID of the user who owns this file secret (auto-populated from authenticated user if not provided).
- `owners` - (Optional, List) A list of additional owner IDs. Each entry should contain `owner_id`.

## Attributes

- `id` - (String, Computed) The unique identifier of the file secret in Secret Safe.
- `folder_id` - (String) Parent folder UUID
- `title` - (String) The secret title
- `description` - (String) The secret description
- `file_name` - (String) The file name
- `file_content_base64` - (String, Sensitive) The file content as base64
- `owner_id` - (Number) ID of the secret owner (auto-populated from authenticated user)

## Sensitive Attributes

The `file_content_base64` attribute is marked as sensitive to prevent file contents from being exposed in logs or state files.

## Example with Multiple Owners

```hcl
resource "secretsafe_folder_file" "private_key" {
  folder_id           = secretsafe_folder.secure_files.id
  title               = "API Private Key"
  description         = "Private key for third-party API authentication"
  file_name           = "api-key.pem"
  file_content_base64 = filebase64("${path.module}/keys/api-key.pem")
}
```

## Example with Inline Content

```hcl
resource "secretsafe_folder_file" "config" {
  folder_id           = secretsafe_folder.secure_files.id
  title               = "Database Configuration"
  description         = "Encrypted database configuration file"
  file_name           = "database-config.json"
  file_content_base64 = base64encode(jsonencode({
    host     = "db.example.com"
    port     = 5432
    username = "dbuser"
  }))
}
```

## Notes

### Base64 Encoding

- Use `filebase64()` function to read and encode files: `filebase64("${path.module}/path/to/file")`
- Use `base64encode()` function for inline content: `base64encode("content here")`
- Binary files and text files both require base64 encoding

### File Size Considerations

- Large files increase the size of the Terraform state file
- Consider the network bandwidth when uploading large files
- Keep encrypted backups of your files for disaster recovery

### Security Best Practices

- **Never commit raw file contents** to your repository; use local file references with `.gitignore`
- **Use `terraform plan`** before applying to verify file paths and contents
- **Store sensitive files** outside your Terraform working directory
- **Rotate old file secrets** by replacing them with new versions
- **Monitor access logs** in Secret Safe for unauthorized access attempts

## Example with Sensitive Local File

```hcl
variable "ssl_cert_path" {
  type        = string
  description = "Path to SSL certificate file"
  sensitive   = true
}

resource "secretsafe_folder_file" "ssl_from_var" {
  folder_id           = secretsafe_folder.secure_files.id
  title               = "Dynamic SSL Certificate"
  file_name           = "dynamic.crt"
  file_content_base64 = filebase64(var.ssl_cert_path)
}
```

## Import

File secrets can be imported using the secret ID:

```bash
terraform import secretsafe_folder_file.ssl_cert secret-id-here
```

## Technical Details

### Upload Process

1. File is encoded to base64 in Terraform
2. Provider decodes base64 content
3. File is uploaded to Secret Safe via `multipart/form-data`
4. Secret Safe encrypts and stores the file
5. File ID is returned and stored in state

### Supported File Types

- **Certificates & Keys**: `.crt`, `.key`, `.pem`, `.pfx`
- **Archives**: `.zip`, `.tar`, `.gz`, `.7z`
- **Backups**: `.sql`, `.bak`, `.backup`
- **Configuration**: `.json`, `.yaml`, `.yml`, `.toml`, `.xml`
- **Documents**: `.pdf`, `.docx`, `.xlsx`
- **Any binary or text file**
