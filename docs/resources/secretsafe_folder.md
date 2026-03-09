# Resource: secretsafe_folder

Manage folders in BeyondTrust Secret Safe — create, read, update, and delete folders for organizing secrets.

## Example

```hcl
resource "secretsafe_folder" "example" {
  name           = "My Secure Folder"
  description    = "Folder for storing database credentials"
  user_group_id  = 1
  parent_id      = null
}

output "folder_id" {
  value = secretsafe_folder.example.id
}
```

## Arguments

- `name` - (Required, String) The name of the folder. Must be unique within the parent folder.
- `description` - (Optional, String) A description of the folder's purpose.
- `parent_id` - (Optional, String) The ID of the parent folder. If not specified, the folder is created at the root level.
- `owner_id` - (Optional, Number) The ID of the user who owns this folder. If not specified, defaults to the authenticated user.
- `user_group_id` - (Required, Number) The ID of the user group that owns this folder. This determines access permissions.

## Attributes

- `id` - (String, Computed) The unique identifier of the folder in Secret Safe format.
- `name` - (String) The folder name
- `description` - (String) The folder description
- `owner_id` - (Number) The ID of the folder owner
- `parent_id` - (String) Parent folder UUID
- `user_group_id` - (Number) The user group ID

## Notes

- Attempting to delete a folder that contains secrets will result in a `409 Conflict` error. You must delete all secrets within the folder before deleting the folder itself.
- Folder names must be unique within their parent folder.
- If `owner_id` is not specified, it defaults to the authenticated user (the user running the provider).
- The `user_group_id` is required and determines access control for the folder.

## Example with Parent Folder

```hcl
resource "secretsafe_folder" "parent" {
  name          = "Parent Folder"
  description   = "Top-level folder"
  user_group_id = 1
}

resource "secretsafe_folder" "child" {
  name           = "Child Folder"
  description    = "Nested folder under parent"
  parent_id      = secretsafe_folder.parent.id
  user_group_id  = 1
}
```
