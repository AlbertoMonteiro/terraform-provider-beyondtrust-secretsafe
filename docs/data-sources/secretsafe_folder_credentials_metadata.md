# Data Source: secretsafe_folder_credentials_metadata

Retrieve the metadata of every credential secret in a BeyondTrust Secret Safe folder, such as when each one was last modified, without usernames or passwords.

Nothing in it is secret, so the values can go into ordinary arguments and outputs. The typical use is the version of a write-only argument: the password comes from the [`secretsafe_folder_credentials_data` ephemeral resource](../ephemeral-resources/secretsafe_folder_credentials_data.md), and `modified_on` from this data source tells Terraform when to write it again.

## Example

```hcl
ephemeral "secretsafe_folder_credentials_data" "app" {
  folder_name = "app-production"
}

data "secretsafe_folder_credentials_metadata" "app" {
  folder_name = "app-production"
}

resource "google_secret_manager_secret_version" "db" {
  secret = google_secret_manager_secret.db.id

  secret_data_wo         = ephemeral.secretsafe_folder_credentials_data.app.credentials["db-admin"].password
  secret_data_wo_version = tonumber(formatdate("YYYYMMDDhhmmss", data.secretsafe_folder_credentials_metadata.app.credentials["db-admin"].modified_on))
}
```

The keys are the same as in `secretsafe_folder_credentials_data` for the same folder: the titles of the credential secrets. Text and file secrets in the folder are ignored, and so are subfolders.

## Arguments

Set exactly one of these:

- `folder_id` - (Optional, String) The UUID of the folder
- `folder_name` - (Optional, String) The name of the folder. The match is exact but ignores case, and it must find exactly one folder. If two folders share the name, use `folder_id`

## Attributes

- `folder_id` - (String) The folder ID, also set when the folder is found by `folder_name`
- `credentials` - (Map of Object) The metadata of the credential secrets of the folder, keyed by title. It is not sensitive. Each object has:
  - `secret_id` - (String) The secret ID
  - `created_on` - (String) When the credential was created, in RFC 3339 UTC as returned by Secret Safe (for example `2026-10-14T18:57:26.7Z`)
  - `created_by` - (String) User who created the credential
  - `modified_on` - (String) When the credential was last modified, in the same format as `created_on`
  - `modified_by` - (String) User who last modified the credential

Usernames and passwords are never read into this data source, so they do not reach the plan or the state.

`formatdate` keeps whole seconds, so two changes within the same second give the same version.

Reading fails if two credentials in the folder have the same title.
