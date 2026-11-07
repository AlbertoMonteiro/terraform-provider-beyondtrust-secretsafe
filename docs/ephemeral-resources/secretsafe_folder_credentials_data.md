# Ephemeral Resource: secretsafe_folder_credentials_data

Retrieve the username and password of every credential secret in a BeyondTrust Secret Safe folder without storing them in the Terraform state or plan.

Requires Terraform 1.10 or later.

## Example

```hcl
ephemeral "secretsafe_folder_credentials_data" "app" {
  folder_name = "app-production"
}

# In another provider's configuration
provider "postgresql" {
  username = ephemeral.secretsafe_folder_credentials_data.app.credentials["db-admin"].username
  password = ephemeral.secretsafe_folder_credentials_data.app.credentials["db-admin"].password
}
```

Credentials are keyed by the title of the secret. Text and file secrets in the folder are ignored, and so are subfolders.

Ephemeral values can only be used where Terraform accepts them, such as write-only arguments, `provider` blocks, `locals`, other `ephemeral` blocks and ephemeral variables or child module outputs. They cannot be used in root module outputs or ordinary resource arguments.

## Arguments

Set exactly one of these:

- `folder_id` - (Optional, String) The UUID of the folder
- `folder_name` - (Optional, String) The name of the folder. The match is exact but ignores case, and it must find exactly one folder. If two folders share the name, use `folder_id`

## Attributes

- `folder_id` - (String) The folder ID, also set when the folder is found by `folder_name`
- `credentials` - (Map of Object, Sensitive) The credential secrets of the folder, keyed by title. Each object has:
  - `secret_id` - (String) The secret ID
  - `username` - (String) The username from the credential secret
  - `password` - (String) The password from the credential secret
  - `created_on` - (String) When the credential was created, in RFC 3339 UTC as returned by Secret Safe (for example `2026-10-14T18:57:26.7Z`)
  - `created_by` - (String) User who created the credential
  - `modified_on` - (String) When the credential was last modified, in the same format as `created_on`
  - `modified_by` - (String) User who last modified the credential

The whole map is sensitive, so usernames are too. Wrap a value in `nonsensitive()` to show it.

Opening fails if two credentials in the folder have the same title.
