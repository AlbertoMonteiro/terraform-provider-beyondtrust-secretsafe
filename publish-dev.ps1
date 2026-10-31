$project = "BeyondTrust.SecretSafeProvider/BeyondTrust.SecretSafeProvider.csproj"
$dist    = Join-Path $PSScriptRoot "dist"
$tf      = Join-Path $PSScriptRoot "terraform"

rm $dist -Force -Recurse
rm (Join-Path $tf ".terraform") -Force -Recurse -ErrorAction SilentlyContinue
rm (Join-Path $tf ".terraform.lock.hcl") -Force -ErrorAction SilentlyContinue

dotnet publish $project -r win-x86 -o $dist
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Get-ChildItem -Path $dist -Exclude "*.exe" | Remove-Item -Force
