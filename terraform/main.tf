terraform {
  required_providers {
    secretsafe = {
      source = "beyondtrust/secretsafe"
    }
  }
}

provider "secretsafe" {
  key     = ""
  runas   = ""
  baseUrl = "http://localhost:32772"
}

data "secretsafe_credential_data" "example" {
  secret_id = "2E602574-F465-4B1A-A008-6A1348C751F3"
}

output "content" {
  value = data.secretsafe_credential_data.example.username
}
