$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$envPath = Join-Path $projectRoot '.env'
if (Test-Path -LiteralPath $envPath) { throw '.env já existe. Preserve suas credenciais; não será sobrescrito.' }
function New-RandomSecret { [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48)) }
$dbPassword = New-RandomSecret
$jwtKey = New-RandomSecret
$adminPassword = New-RandomSecret
@"
POSTGRES_DB=agroconecta
POSTGRES_USER=agroconecta
POSTGRES_PASSWORD=$dbPassword
JWT_KEY=$jwtKey
ADMIN_EMAIL=admin@agroconecta.local
ADMIN_PASSWORD=$adminPassword
"@ | Set-Content -LiteralPath $envPath -Encoding utf8
Write-Host 'Arquivo .env criado com segredos aleatórios. Consulte-o localmente para obter o login ADMIN.'
Write-Host 'Execute: docker compose up -d --build'
