# PRB — Product Requirements Backlog

Terraform Provider para **BeyondTrust Secret Safe** utilizando **.NET Native AOT**, **Slim Builder** e **Protocolo V5.2**.

---

## Arquitetura do Provider

O provider implementa o padrão **Strategy** para Data Sources via `IDataSourceHandler`. Cada data source é registrado no DI container (`Program.cs`) e automaticamente exposto no schema do provider via `Terraform5ProviderService`.

### Padrão de implementação para um novo Data Source

1. **Model** (`Models/`) — classe `[MessagePackObject]` com `GetSchema()` estático
2. **Handler** (`Services/DataSources/`) — implementa `IDataSourceHandler` (TypeName, GetSchema, ReadAsync)
3. **Registro no DI** (`Program.cs`) — `builder.Services.AddSingleton<IDataSourceHandler, XxxHandler>()`
4. **Serialização** (`Serialization/Json.cs`) — registrar `[JsonSerializable(typeof(XxxData))]`
5. **WireMock mapping** (`AppHost/__admin/mappings/`) — mock para testes de integração
6. **Testes unitários** — Handler tests + Schema tests (TUnit + Imposter)
7. **Testes de integração** — Via Aspire (GetSchema + ReadDataSource msgpack + JSON)
8. **Documentação** (`docs/`) — Arquivo markdown para Terraform Registry

### Documentação para Terraform Registry

A documentação é essencial para o provider aparecer corretamente no Terraform Registry (https://registry.terraform.io/). Cada data source e resource deve ter um arquivo `.md` correspondente na pasta `docs/`.

**Estrutura de pastas:**
```
docs/
├── index.md                          (descrição geral do provider)
├── data-sources/
│   ├── secretsafe_credential_data.md
│   ├── secretsafe_download_file_data.md
│   └── ... (novos data sources)
└── resources/
    ├── secretsafe_folder.md
    ├── secretsafe_folder_credential.md
    ├── secretsafe_folder_file.md
    └── ... (novos resources)
```

**Formato de documentação — Data Source:**
```markdown
# Data Source: {name}

{Descrição breve}

## Example

\`\`\`hcl
data "{name}" "{example_name}" {
  {argument} = "{value}"
}

output "result" {
  value = data.{name}.{example_name}.{attribute}
}
\`\`\`

## Arguments

- \`{argument1}\` - (Required/Optional) {Descrição}
- \`{argument2}\` - (Optional) {Descrição}

## Attributes

- \`{computed_attr1}\` - (Computed) {Descrição}
- \`{computed_attr2}\` - (Computed, Sensitive) {Descrição}
```

**Formato de documentação — Resource:**
```markdown
# Resource: {name}

{Descrição breve}

## Example

\`\`\`hcl
resource "{name}" "example" {
  {required_arg} = "{value}"
  {optional_arg} = "{value}"
}
\`\`\`

## Arguments

- \`{required_arg}\` - (Required) {Descrição}
- \`{optional_arg}\` - (Optional) {Descrição}

## Attributes

- \`id\` - (Computed) Identificador único do recurso
```

**Padrão de nomeclatura:**
- Data source: `docs/data-sources/{terraform-name}.md`
- Resource: `docs/resources/{terraform-name}.md`

### API BeyondTrust Secret Safe v3

Base: `/public/v3`

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| POST | `/Auth/SignAppin` | Autenticação com Key + RunAs |
| POST | `/Auth/Signout` | Encerrar sessão |
| GET | `/Secrets-Safe/Secrets/{secretId}` | Obter segredo (credential) |
| GET | `/Secrets-Safe/Secrets/{secretId}/file/download` | Download de arquivo secreto |
| GET | `/Secrets-Safe/Secrets/{secretId}/text` | Obter texto secreto |
| GET | `/Secrets-Safe/Secrets/{secretId}/file` | Obter metadados de arquivo secreto |
| GET | `/Secrets-Safe/Secrets` | Listar segredos |
| POST | `/Secrets-Safe/Folders/` | Criar pasta |
| GET | `/Secrets-Safe/Folders/` | Listar pastas |
| GET | `/Secrets-Safe/Folders/{id}` | Obter pasta por ID |
| PUT | `/Secrets-Safe/Folders/{id}` | Atualizar pasta |
| DELETE | `/Secrets-Safe/Folders/{id}` | Deletar pasta |
| POST | `/Secrets-Safe/Folders/{folderId}/secrets` | Criar secret (credencial) em pasta |
| POST | `/Secrets-Safe/Folders/{folderId}/secrets/file` | Criar secret (arquivo) em pasta |
| GET | `/Secrets-Safe/Folders/{folderId}/secrets` | Listar secrets de uma pasta |

---

## Data Sources — Status

### ✅ 1. `secretsafe_credential_data` — COMPLETO

**API:** `GET /Secrets-Safe/Secrets/{secretId}`

**Descrição:** Recupera credenciais (username/password) de um segredo no BeyondTrust Secret Safe.

**Schema Terraform:**
| Atributo | Tipo | Required | Computed | Sensitive |
|----------|------|----------|----------|-----------|
| `secret_id` | string | ✅ | | |
| `username` | string | | ✅ | |
| `password` | string | | ✅ | ✅ |

**Arquivos:**
- `Models/CredentialData.cs`
- `Services/DataSources/CredentialDataSourceHandler.cs`
- `Serialization/Json.cs` (registrado)
- `Program.cs` (registrado no DI)
- `AppHost/__admin/mappings/secrets-get.json` (WireMock)
- ✅ `docs/data-sources/secretsafe_credential_data.md` (documentação para Terraform Registry)

**Testes:**
- ✅ `CredentialDataTests.cs` — 5 testes de schema
- ✅ `CredentialDataSourceHandlerTests.cs` — 4 testes unitários (happy path + erros SignAppin/GetSecret/Signout)
- ✅ `IntegrationTests.cs` — 3 testes (GetSchema + ReadDataSource msgpack + JSON)

---

### ✅ 2. `secretsafe_download_file_data` — COMPLETO

**API:** `GET /Secrets-Safe/Secrets/{secretId:guid}/file/download`

**Descrição:** Faz download do conteúdo binário de um arquivo armazenado como segredo no Secret Safe. A API retorna `Content-Type: application/octet-stream` com o arquivo como attachment. O header `Content-Disposition` contém o nome do arquivo. O conteúdo é retornado em base64 pois o Terraform não suporta valores binários nativamente.

**Refit:** `IBeyondTrustSecretSafe.DownloadSecret(Guid secretId)` → `Task<HttpResponseMessage>`

**Schema Terraform:**
| Atributo | Tipo | Required | Computed | Sensitive |
|----------|------|----------|----------|-----------|
| `secret_id` | string | ✅ | | |
| `file_name` | string | | ✅ | |
| `file_content_base64` | string | | ✅ | ✅ |

**Arquivos:**
- `Models/FileDownloadData.cs`
- `Services/DataSources/FileDownloadDataSourceHandler.cs`
- `Serialization/Json.cs` (registrado)
- `Program.cs` (registrado no DI)
- `AppHost/__admin/mappings/secrets-download.json` (WireMock)
- ✅ `docs/data-sources/secretsafe_download_file_data.md` (documentação para Terraform Registry)

**Testes:**
- ✅ `FileDownloadDataTests.cs` — 5 testes de schema
- ✅ `FileDownloadDataSourceHandlerTests.cs` — 4 testes unitários (happy path + erros SignAppin/DownloadSecret/Signout)
- ✅ `IntegrationTests.cs` — 3 testes (GetSchema + ReadDataSource msgpack + JSON)

---

### ⬜ 3. `secretsafe_text_data` — PENDENTE

**API:** `GET /Secrets-Safe/Secrets/{secretId:guid}/text`

**Descrição:** Recupera o conteúdo de texto de um segredo do tipo "text" no Secret Safe. A API retorna um JSON com o campo `Text` contendo o conteúdo do segredo.

**Schema Terraform proposto:**
| Atributo | Tipo | Required | Computed | Sensitive |
|----------|------|----------|----------|-----------|
| `secret_id` | string | ✅ | | |
| `title` | string | | ✅ | |
| `text` | string | | ✅ | ✅ |

**Artefatos necessários:**

1. **Refit:** Adicionar `GetSecretText(Guid secretId)` em `IBeyondTrustSecretSafe.cs`
   - `[Get("/public/v3/Secrets-Safe/Secrets/{secretId}/text")]`
   - Retorno: `Task<SecretTextValue>` (novo record)

2. **Model response:** `Models/SecretTextValue.cs` — `record SecretTextValue(string Title, string Text)`

3. **Model:** `Models/TextSecretData.cs`
   - Classe `[MessagePackObject]` com `SecretId`, `Title`, `Text`
   - Método estático `GetSchema()`

4. **Handler:** `Services/DataSources/TextSecretDataSourceHandler.cs`
   - `TypeName = "secretsafe_text_data"`
   - Fluxo: SignAppin → GetSecretText → Signout

5. **Serialização:** Registrar `[JsonSerializable(typeof(TextSecretData))]` e `[JsonSerializable(typeof(SecretTextValue))]` em `Serialization/Json.cs`

6. **DI:** Registrar handler em `Program.cs`

7. **WireMock:** `AppHost/__admin/mappings/secrets-text.json`
   - PathPattern: `^/public/v3/Secrets-Safe/Secrets/[0-9a-fA-F\\-]+/text$`
   - Response: `{ "Title": "My Test Text Secret", "Text": "secret-text-content-from-wiremock" }`

8. **Documentação:** `docs/data-sources/secretsafe_text_data.md` — Documentação para Terraform Registry

**Testes unitários necessários:**

8. **`TextSecretDataTests.cs`** — 5 testes de schema
9. **`TextSecretDataSourceHandlerTests.cs`** — 4 testes unitários (happy path + 3 erros)

**Testes de integração necessários:**

10. **`IntegrationTests.cs`** — 3 testes adicionais (GetSchema + ReadDataSource msgpack + JSON)

---

### ⬜ 4. `secretsafe_file_metadata_data` — PENDENTE

**API:** `GET /Secrets-Safe/Secrets/{secretId:guid}/file`

**Descrição:** Recupera os **metadados** de um arquivo secreto (sem o conteúdo binário). Retorna informações como `FileName`, `FileHash`, `Title`, `Description`.

**Schema Terraform proposto:**
| Atributo | Tipo | Required | Computed | Sensitive |
|----------|------|----------|----------|-----------|
| `secret_id` | string | ✅ | | |
| `title` | string | | ✅ | |
| `file_name` | string | | ✅ | |
| `file_hash` | string | | ✅ | |
| `description` | string | | ✅ | |

**Artefatos necessários:**

1. **Refit:** Adicionar `GetSecretFileMetadata(Guid secretId)` em `IBeyondTrustSecretSafe.cs`
   - `[Get("/public/v3/Secrets-Safe/Secrets/{secretId}/file")]`
   - Retorno: `Task<SecretFileMetadata>` (novo record)

2. **Model response:** `Models/SecretFileMetadata.cs` — `record SecretFileMetadata(string Title, string Description, string FileName, string FileHash)`

3. **Model:** `Models/FileMetadataData.cs`
   - Classe `[MessagePackObject]` com `SecretId`, `Title`, `FileName`, `FileHash`, `Description`
   - Método estático `GetSchema()`

4. **Handler:** `Services/DataSources/FileMetadataDataSourceHandler.cs`
   - `TypeName = "secretsafe_file_metadata_data"`
   - Fluxo: SignAppin → GetSecretFileMetadata → Signout

5. **Serialização, DI, WireMock, Testes** — seguir mesmo padrão das tasks anteriores

6. **Documentação:** `docs/data-sources/secretsafe_file_metadata_data.md` — Documentação para Terraform Registry

---

### ⬜ 5. `secretsafe_secrets_list_data` — PENDENTE

**API:** `GET /Secrets-Safe/Secrets` (com query params opcionais: Path, Title, Limit, Offset)

**Descrição:** Lista segredos disponíveis no Secret Safe, com filtros opcionais. Útil para descoberta de segredos em automações Terraform.

**Schema Terraform proposto:**
| Atributo | Tipo | Required | Computed | Sensitive |
|----------|------|----------|----------|-----------|
| `path` | string | | | |
| `title` | string | | | |
| `secrets` | list(object) | | ✅ | |

> Cada objeto em `secrets` contém: `id`, `title`, `username`, `folder_path`

**Complexidade:** Média-alta (requer tipo list/object no schema Terraform)

**Documentação necessária:** `docs/data-sources/secretsafe_secrets_list_data.md`

---

### ⬜ 6. `secretsafe_folder_data` — PENDENTE

**API:** `GET /Secrets-Safe/Folders/{id}`

**Descrição:** Recupera informações de uma pasta do Secret Safe por ID.

**Schema Terraform:**
| Atributo | Tipo | Required | Computed | Sensitive |
|----------|------|----------|----------|-----------|
| `folder_id` | string | ✅ | | |
| `name` | string | | ✅ | |
| `description` | string | | ✅ | |
| `parent_id` | string | | ✅ | |
| `user_group_id` | number | | ✅ | |

**Artefatos necessários:**
1. **Refit:** `GetFolder(string id)` em `IBeyondTrustSecretSafe.cs` — `Task<FolderResponse>`
2. **Model response:** `Models/FolderResponse.cs` — campos: `Id`, `Name`, `Description`, `ParentId`, `UserGroupId`
3. **Model:** `Models/FolderData.cs` — mapeamento para Terraform com método `GetSchema()`
4. **Handler:** `Services/DataSources/FolderDataSourceHandler.cs` — `TypeName = "secretsafe_folder_data"`
5. **Serialização:** Registrar em `Serialization/Json.cs`
6. **DI:** Registrar em `Program.cs`
7. **WireMock:** `AppHost/__admin/mappings/folders-get.json`
8. **Testes:** Schema tests + handler unit tests + integration tests
9. **Documentação:** `docs/data-sources/secretsafe_folder_data.md` — Documentação para Terraform Registry

---

### ⬜ 7. `secretsafe_folders_list_data` — PENDENTE

**API:** `GET /Secrets-Safe/Folders/` (com query params opcionais)

**Descrição:** Lista todas as pastas disponíveis no Secret Safe com suporte a filtros opcionais.

**Schema Terraform:**
| Atributo | Tipo | Required | Computed | Sensitive |
|----------|------|----------|----------|-----------|
| `folder_name` | string | | | |
| `folder_path` | string | | | |
| `root_only` | bool | | | |
| `limit` | number | | | |
| `offset` | number | | | |
| `folders` | list(object) | | ✅ | |

> Cada objeto em `folders` contém: `id`, `name`, `description`, `parent_id`, `user_group_id`

**Artefatos necessários:**
1. **Refit:** `ListFolders(string? folderName = null, string? folderPath = null, bool? rootOnly = null, int? limit = null, int? offset = null)` em `IBeyondTrustSecretSafe.cs`
2. **Model response:** `Models/FolderListResponse.cs` — lista de `FolderResponse`
3. **Model:** `Models/FoldersListData.cs` — com lista de folders e método `GetSchema()`
4. **Handler:** `Services/DataSources/FoldersListDataSourceHandler.cs` — `TypeName = "secretsafe_folders_list_data"`
5. **Serialização, DI, WireMock, Testes** — seguir padrão das data sources anteriores
6. **Documentação:** `docs/data-sources/secretsafe_folders_list_data.md` — Documentação para Terraform Registry

---

### ✅ 8. `secretsafe_folder` — NOVO RESOURCE (CRUD) — COMPLETO

**APIs utilizadas:**
- Create: `POST /Secrets-Safe/Folders/`
- Read: `GET /Secrets-Safe/Folders/{id}`
- Update: `PUT /Secrets-Safe/Folders/{id}`
- Delete: `DELETE /Secrets-Safe/Folders/{id}`

**Descrição:** Gerencia pastas (folders) no Secret Safe — criar, ler, atualizar e deletar.

**Schema Terraform:**
| Atributo | Tipo | Required | Computed | Sensitive |
|----------|------|----------|----------|-----------|
| `id` | string | | ✅ | |
| `name` | string | ✅ | | |
| `description` | string | | | |
| `parent_id` | string | | | |
| `user_group_id` | number | ✅ | | |

**Importante:** A API retorna erro `409 Conflict` ao tentar deletar uma pasta que contém secrets. O provider deve documentar este comportamento.

**Artefatos necessários:**
1. **Refit:** Adicionar em `IBeyondTrustSecretSafe.cs`:
   - `CreateFolder(FolderRequest request)` → `Task<FolderResponse>`
   - `UpdateFolder(string id, FolderRequest request)` → `Task<FolderResponse>`
   - `DeleteFolder(string id)` → `Task<HttpResponseMessage>`
   - `GetFolder(string id)` → `Task<FolderResponse>` (reutiliza da data source #6)

2. **Models:**
   - `Models/FolderRequest.cs` — `Name`, `Description`, `ParentId`, `UserGroupId`
   - `Models/FolderResponse.cs` — `Id`, `Name`, `Description`, `ParentId`, `UserGroupId` (compartilhado com data source #6)

3. **Model Terraform:** `Models/FolderResourceData.cs`
   - Classe `[MessagePackObject]` com todos os campos
   - Método estático `GetSchema()`

4. **Resource Handler:** `Services/Resources/FolderResourceHandler.cs`
   - Implementa resource handler pattern
   - Fluxo Create: SignAppin → CreateFolder → Signout
   - Fluxo Read: SignAppin → GetFolder → Signout
   - Fluxo Update: SignAppin → UpdateFolder → Signout
   - Fluxo Delete: SignAppin → DeleteFolder → Signout
   - Nota: Tratar erro 409 em Delete com mensagem amigável

5. **Serialização:** Registrar em `Serialization/Json.cs`

6. **DI:** Registrar em `Program.cs`

7. **WireMock:** `AppHost/__admin/mappings/`
   - `folders-create.json` — POST /Secrets-Safe/Folders/
   - `folders-get.json` — GET /Secrets-Safe/Folders/{id}
   - `folders-update.json` — PUT /Secrets-Safe/Folders/{id}
   - `folders-delete.json` — DELETE /Secrets-Safe/Folders/{id}
   - `folders-delete-409.json` — DELETE /Secrets-Safe/Folders/{id} (pasta com secrets)

8. **Testes unitários:**
   - `FolderResourceDataTests.cs` — schema tests
   - `FolderResourceHandlerTests.cs` — unit tests (happy path + erros)

9. **Testes de integração:** via Aspire (ApplyResourceChange msgpack + JSON para CRUD)

10. **Documentação:** ⬜ `docs/resources/secretsafe_folder.md` — Documentação para Terraform Registry

---

### ⬜ 9. `secretsafe_folder_secrets_list_data` — PENDENTE

**API:** `GET /Secrets-Safe/Folders/{folderId}/secrets`

**Descrição:** Lista todos os secrets (credenciais e arquivos) armazenados dentro de uma pasta específica do Secret Safe.

**Schema Terraform:**
| Atributo | Tipo | Required | Computed | Sensitive |
|----------|------|----------|----------|-----------|
| `folder_id` | string | ✅ | | |
| `secrets` | list(object) | | ✅ | |

> Cada objeto em `secrets` contém: `id`, `title`, `description`, `username` (opcional), `owner_id`

**Artefatos necessários:**
1. **Refit:** `ListFolderSecrets(string folderId)` em `IBeyondTrustSecretSafe.cs` — `Task<FolderSecretsListResponse>`
2. **Model response:** `Models/FolderSecretsListResponse.cs` — lista de secrets com metadados
3. **Model:** `Models/FolderSecretsListData.cs` — com método `GetSchema()`
4. **Handler:** `Services/DataSources/FolderSecretsListDataSourceHandler.cs` — `TypeName = "secretsafe_folder_secrets_list_data"`
5. **Serialização, DI, WireMock, Testes** — seguir padrão das data sources anteriores
6. **Documentação:** `docs/data-sources/secretsafe_folder_secrets_list_data.md` — Documentação para Terraform Registry

---

### ✅ 10. `secretsafe_folder_credential` — NOVO RESOURCE (CREATE/UPDATE) — COMPLETO

**API:** `POST /Secrets-Safe/Folders/{folderId}/secrets`

**Descrição:** Gerencia credenciais (username/password) dentro de pastas do Secret Safe — criar e atualizar credenciais.

**Schema Terraform:**
| Atributo | Tipo | Required | Computed | Sensitive |
|----------|------|----------|----------|-----------|
| `id` | string | | ✅ | |
| `folder_id` | string | ✅ | | |
| `title` | string | ✅ | | |
| `description` | string | | | |
| `username` | string | ✅ | | |
| `password` | string | ✅ | | ✅ |
| `owner_id` | number | ✅ | | |
| `owners` | list(object) | | | |

> Cada objeto em `owners` contém: `owner_id`

**Artefatos necessários:**
1. **Refit:** Adicionar em `IBeyondTrustSecretSafe.cs`:
   - `CreateCredentialSecret(string folderId, CreateSecretCredentialRequest request)` → `Task<SecretResponse>`
   - `UpdateCredentialSecret(string secretId, CreateSecretCredentialRequest request)` → `Task<SecretResponse>`

2. **Models:**
   - `Models/CreateSecretCredentialRequest.cs` — `Title`, `Description`, `Username`, `Password`, `OwnerId`, `Owners[]`
   - `Models/SecretResponse.cs` — resposta da API com `Id`, `Title`, `Description`, `OwnerId`

3. **Model Terraform:** `Models/FolderCredentialData.cs` com método `GetSchema()`

4. **Resource Handler:** `Services/Resources/FolderCredentialResourceHandler.cs`
   - Fluxo Create: SignAppin → CreateCredentialSecret → Signout
   - Fluxo Read: SignAppin → GetSecret → Signout
   - Fluxo Update: SignAppin → UpdateCredentialSecret → Signout
   - Fluxo Delete: Determinar em documentação API

5. **Serialização:** Registrar em `Serialization/Json.cs`

6. **DI:** Registrar em `Program.cs`

7. **WireMock:** `AppHost/__admin/mappings/folder-credential-*.json`
   - `folder-credential-create.json` — POST /Secrets-Safe/Folders/{folderId}/secrets
   - `folder-credential-get.json` — GET /Secrets-Safe/Secrets/{secretId}
   - `folder-credential-update.json` — UPDATE (if supported)

8. **Testes unitários:**
   - `FolderCredentialDataTests.cs` — schema tests
   - `FolderCredentialResourceHandlerTests.cs` — unit tests (happy path + erros)

9. **Testes de integração:** via Aspire (ApplyResourceChange msgpack + JSON para CRUD)

10. **Documentação:** ⬜ `docs/resources/secretsafe_folder_credential.md` — Documentação para Terraform Registry

---

### ✅ 11. `secretsafe_folder_file` — NOVO RESOURCE (CREATE) — COMPLETO

**API:** `POST /Secrets-Safe/Folders/{folderId}/secrets/file` (multipart/form-data)

**Descrição:** Gerencia arquivos secretos dentro de pastas do Secret Safe — fazer upload de arquivos com metadados.

**Schema Terraform:**
| Atributo | Tipo | Required | Computed | Sensitive |
|----------|------|----------|----------|-----------|
| `id` | string | | ✅ | |
| `folder_id` | string | ✅ | | |
| `title` | string | ✅ | | |
| `description` | string | | | |
| `file_name` | string | ✅ | | |
| `file_content_base64` | string | ✅ | | ✅ |
| `owner_id` | number | ✅ | | |
| `owners` | list(object) | | | |

> Cada objeto em `owners` contém: `owner_id`

**Importante:** Upload via `multipart/form-data` com `SecretMetadata` (JSON) + `File` (binário).

**Artefatos necessários:**
1. **Refit:** Adicionar em `IBeyondTrustSecretSafe.cs`:
   - `CreateFileSecret(string folderId, CreateSecretFileRequest request)` → `Task<SecretResponse>` (multipart)

2. **Models:**
   - `Models/CreateSecretFileRequest.cs` — `Title`, `Description`, `FileName`, `FileContent` (base64), `OwnerId`, `Owners[]`
   - `Models/SecretResponse.cs` — resposta da API com `Id`, `Title`, `Description`, `OwnerId` (compartilhado com #10)

3. **Model Terraform:** `Models/FolderFileSecretData.cs` com método `GetSchema()`

4. **Resource Handler:** `Services/Resources/FolderFileSecretResourceHandler.cs`
   - Fluxo Create: SignAppin → CreateFileSecret (multipart) → Signout
   - Fluxo Read: SignAppin → GetSecret → Signout
   - Fluxo Delete: Determinar em documentação API
   - Nota: Decodificar base64 antes de enviar para API

5. **Serialização:** Registrar em `Serialization/Json.cs`

6. **DI:** Registrar em `Program.cs`

7. **WireMock:** `AppHost/__admin/mappings/folder-file-*.json`
   - `folder-file-create.json` — POST /Secrets-Safe/Folders/{folderId}/secrets/file (multipart)
   - `folder-file-get.json` — GET /Secrets-Safe/Secrets/{secretId}

8. **Testes unitários:**
   - `FolderFileSecretDataTests.cs` — schema tests (incluindo base64)
   - `FolderFileSecretResourceHandlerTests.cs` — unit tests (multipart, base64 encoding/decoding)

9. **Testes de integração:** via Aspire (ApplyResourceChange com multipart form-data)

10. **Documentação:** ⬜ `docs/resources/secretsafe_folder_file.md` — Documentação para Terraform Registry

---

## Lista de Implementação

| # | Status | Item | Tipo | Justificativa |
|---||--- ||--- ||---||---|
| 1 | ✅ | `secretsafe_credential_data` | (data source) | Já implementado |
| 2 | ✅ | `secretsafe_download_file_data` | (data source) | Já implementado |
| 3 | ⬜ | `secretsafe_text_data` | (data source) | Texto secreto (notas, tokens, API keys em texto) |
| 4 | ⬜ | `secretsafe_file_metadata_data` | (data source) | Metadados de arquivo (útil para verificação de hash) |
| 5 | ⬜ | `secretsafe_secrets_list_data` | (data source) | Listagem/descoberta de segredos |
| 6 | ⬜ | `secretsafe_folder_data` | (data source) | Informações de pasta (leitura) |
| 7 | ⬜ | `secretsafe_folders_list_data` | (data source) | Listar pastas com filtros |
| 8 | ✅ | `secretsafe_folder` | (resource CRUD) | Gerenciar pastas (criar, atualizar, deletar) |
| 9 | ⬜ | `secretsafe_folder_secrets_list_data` | (data source) | Listar secrets dentro de uma pasta |
| 10 | ✅ | `secretsafe_folder_credential` | (resource) | Já implementado |
| 11 | ✅ | `secretsafe_folder_file` | (resource) | Já implementado |

---

## Referência Técnica

### Arquivos-chave do projeto

| Arquivo | Propósito |
|---------|-----------|
| `Program.cs` | Entry point, DI, Kestrel, handshake |
| `Services/Terraform5ProviderService.cs` | gRPC service principal (despacha para handlers) |
| `Services/DataSources/IDataSourceHandler.cs` | Interface de data source |
| `Services/IBeyondTrustSecretSafe.cs` | Interface Refit (API REST) |
| `Services/BeyondTrustApiFactory.cs` | Factory para criar clientes API |
| `Serialization/Json.cs` | JsonSerializerContext (AOT-safe) |
| `Serialization/SmartSerializer.cs` | Serialização msgpack/JSON unificada |
| `Models/TfTypes.cs` | Tipos Terraform (String) |

### Convenções de teste

- **Framework:** TUnit
- **Mock:** Imposter (source-generated)
- **Padrão:** AAA (Arrange-Act-Assert)
- **SUT:** Variável sempre nomeada `_sut`
- **Integração:** Aspire Testing Framework com WireMock

### Documentação da API

- **Referência completa:** `bi-ps-api-24-1.pdf` (BeyondInsight and Password Safe 24.1 API Guide)
- **Seção relevante:** "Secrets Safe APIs" (páginas 430-457)
