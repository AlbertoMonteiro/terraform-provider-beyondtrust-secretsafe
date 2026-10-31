$tf = Join-Path $PSScriptRoot "terraform"

$env:TF_CLI_CONFIG_FILE = Join-Path $tf "dev.tfrc"

Push-Location $tf

try {
    terraform init
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    terraform plan
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
