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
| GET | `/Secrets-Safe/Folders/` | Listar pastas |
| GET | `/Secrets-Safe/Folders/{id}` | Obter pasta por ID |

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

---

### ⬜ 6. `secretsafe_folder_data` — PENDENTE

**API:** `GET /Secrets-Safe/Folders/{id}`

**Descrição:** Recupera informações de uma pasta do Secret Safe por ID.

**Schema Terraform proposto:**
| Atributo | Tipo | Required | Computed | Sensitive |
|----------|------|----------|----------|-----------|
| `folder_id` | string | ✅ | | |
| `name` | string | | ✅ | |
| `description` | string | | ✅ | |
| `parent_id` | string | | ✅ | |

---

## Prioridade de Implementação

| # | Data Source | Prioridade | Justificativa |
|---|-----------|------------|---------------|
| 1 | ✅ `secretsafe_credential_data` | — | Já implementado |
| 2 | ✅ `secretsafe_download_file_data` | — | Já implementado |
| 3 | ⬜ `secretsafe_text_data` | **Alta** | Texto secreto (notas, tokens, API keys em texto) |
| 4 | ⬜ `secretsafe_file_metadata_data` | **Média** | Metadados de arquivo (útil para verificação de hash) |
| 5 | ⬜ `secretsafe_secrets_list_data` | **Baixa** | Listagem/descoberta de segredos |
| 6 | ⬜ `secretsafe_folder_data` | **Baixa** | Informações de pasta |

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
