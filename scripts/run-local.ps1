param([switch]$SeedAdmin)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$env:Database__Provider = 'Sqlite'
$dbPath = Join-Path $projectRoot 'agroconecta.db'
$env:ConnectionStrings__DefaultConnection = "Data Source=$dbPath"
if (-not $env:Jwt__Key) {
    $env:Jwt__Key = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
}
if ($SeedAdmin) {
    $env:Seed__AdminEmail = Read-Host 'E-mail do administrador'
    $securePassword = Read-Host 'Senha do administrador (mínimo 10 caracteres)' -AsSecureString
    $env:Seed__AdminPassword = [System.Net.NetworkCredential]::new('', $securePassword).Password
}
try {
    dotnet run --project (Join-Path $projectRoot 'src/AgroConecta.Api/AgroConecta.Api.csproj') --urls http://localhost:5080
    if ($LASTEXITCODE -ne 0) { throw "A API encerrou com código $LASTEXITCODE." }
}
finally { Remove-Item Env:Seed__AdminPassword -ErrorAction SilentlyContinue }
