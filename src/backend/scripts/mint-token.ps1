# Emite um JWT RS256 assinado com a chave privada de desenvolvimento (SupportAtos).
# Cole o token impresso no campo "Authorize" do Swagger (http://localhost:5176/swagger).
#
# Requisitos:
#   - Chave privada gerada em tools/SupportAtos.TokenMinter/keys (rode o comando `generate` antes,
#     se ainda nao existir). Configure o PEM publico correspondente em Sso:PublicKeyPem
#     no User Secrets. Confira com: dotnet run --project src/SupportAtos.Api -- --check-dev-auth
#   - A API deve estar rodando com ASPNETCORE_ENVIRONMENT=Development.
#
# Exemplos:
#   .\scripts\mint-token.ps1 -Sub portal-admin -Email admin@atos.com -Name "Admin" -GrupoId grupo-1 -GrupoNome "Grupo Norte" -Admin
#   .\scripts\mint-token.ps1 -Sub portal-agente -Email agente@atos.com -Name "Agente" -GrupoId grupo-1 -GrupoNome "Grupo Norte" -Papel Agente -Minutes 30

param(
    [Parameter(Mandatory = $true)]
    [string]$Sub,

    [Parameter(Mandatory = $true)]
    [string]$Email,

    [Parameter(Mandatory = $true)]
    [string]$Name,

    [Parameter(Mandatory = $true)]
    [string]$GrupoId,

    [Parameter(Mandatory = $true)]
    [string]$GrupoNome,

    [string]$Papel = 'Agente',

    [int]$Minutes = 60,

    [string]$Issuer = 'supportatos-dev',

    [string]$Audience = 'supportatos-api',

    [switch]$Admin
)

$ErrorActionPreference = 'Stop'

if ($Admin) {
    $Papel = 'Admin'
}

$projeto = Join-Path $PSScriptRoot '..\tools\SupportAtos.TokenMinter'
$projeto = [System.IO.Path]::GetFullPath($projeto)

if (-not (Test-Path -LiteralPath (Join-Path $projeto 'SupportAtos.TokenMinter.csproj'))) {
    throw "Projeto do emissor de tokens nao encontrado em '$projeto'."
}

$saida = & dotnet run --project $projeto -- token `
    --sub $Sub `
    --email $Email `
    --name $Name `
    --grupo-id $GrupoId `
    --grupo-nome $GrupoNome `
    --papel $Papel `
    --issuer $Issuer `
    --audience $Audience `
    --minutes $Minutes

if ($LASTEXITCODE -ne 0) {
    $saida | ForEach-Object { Write-Host $_ }
    throw "Falha ao gerar o token (exit code $LASTEXITCODE)."
}

$token = $saida | Where-Object { $_ -match '^eyJ' } | Select-Object -Last 1

if (-not $token) {
    $saida | ForEach-Object { Write-Host $_ }
    throw 'Nenhum token encontrado na saida do gerador.'
}

Write-Host ''
Write-Host '=== Token (cole no Authorize do Swagger, sem aspas) ==='
Write-Host $token
Write-Host '========================================================'
Write-Host "Issuer: $Issuer | Audience: $Audience | Papel: $Papel | Validade: $Minutes min"

# Copia o token para a area de transferencia e ja deixa uma dica de uso.
try {
    Set-Clipboard -Value $token
    Write-Host 'Token copiado para a area de transferencia.'
} catch {
    Write-Host 'Nao foi possivel copiar para a area de transferencia.'
}
