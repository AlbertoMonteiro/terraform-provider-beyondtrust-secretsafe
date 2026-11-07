# Data Source: secretsafe_folder_credentials_data

Retrieve the username and password of every credential secret in a BeyondTrust Secret Safe folder.

~> **Warning:** Terraform stores every password of the folder in the state in plain text. With Terraform 1.10 or later, prefer the [`secretsafe_folder_credentials_data` ephemeral resource](../ephemeral-resources/secretsafe_folder_credentials_data.md) wherever the values can be ephemeral. Use this data source only where Terraform does not accept ephemeral values, such as ordinary resource arguments or outputs, and protect the state accordingly.

## Example

```hcl
data "secretsafe_folder_credentials_data" "app" {
  folder_name = "app-production"
}

resource "kubernetes_secret" "db" {
  metadata {
    name = "db-admin"
  }

  data = {
    username = data.secretsafe_folder_credentials_data.app.credentials["db-admin"].username
    password = data.secretsafe_folder_credentials_data.app.credentials["db-admin"].password
  }
}
```

Credentials are keyed by the title of the secret. Text and file secrets in the folder are ignored, and so are subfolders.

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

The whole map is sensitive, so usernames are too. Wrap a value in `nonsensitive()` to show it.

Reading fails if two credentials in the folder have the same title.
