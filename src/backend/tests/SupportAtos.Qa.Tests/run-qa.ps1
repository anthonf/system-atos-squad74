param(
    [ValidateSet('ALL', 'AUTH', 'RF01', 'RF10')][string]$Group = 'ALL',
    [string]$Scenario
)
$ErrorActionPreference = 'Stop'
if ($Scenario -and $Scenario -notmatch '^(AUTH|RF01|RF10)-[0-9]{2}(-COMPAT)?$') {
    throw 'Código de cenário inválido. Exemplo: AUTH-02 ou RF10-01.'
}
if ($Scenario -and $Group -ne 'ALL' -and -not $Scenario.StartsWith("$Group-")) {
    throw 'O cenário não pertence ao grupo informado.'
}
$runName = 'run-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8)
$reportPath = Join-Path $PSScriptRoot "TestResults/$runName"
New-Item -ItemType Directory -Path $reportPath | Out-Null
$previousReport = $env:QA_REPORT_DIRECTORY
$env:QA_REPORT_DIRECTORY = $reportPath
try {
    $testArguments = @('test', (Join-Path $PSScriptRoot 'SupportAtos.Qa.Tests.csproj'),
        '--logger', 'trx;LogFileName=resultados.trx', '--results-directory', $reportPath)
    if ($Scenario) { $testArguments += @('--filter', "DisplayName~$Scenario") }
    elseif ($Group -ne 'ALL') { $testArguments += @('--filter', "Group=$Group") }
    & dotnet @testArguments
    $testExitCode = $LASTEXITCODE
    $trxPath = Join-Path $reportPath 'resultados.trx'
    if ($testExitCode -eq 0) {
        if (-not (Test-Path -LiteralPath $trxPath)) { $testExitCode = 2 }
        else {
            [xml]$trx = Get-Content -LiteralPath $trxPath -Raw
            $counters = $trx.SelectSingleNode("//*[local-name()='Counters']")
            if ($null -eq $counters -or [int]$counters.executed -eq 0) { $testExitCode = 2 }
        }
    }
    if (-not (Test-Path -LiteralPath (Join-Path $reportPath 'RESUMO.md'))) {
        # Compilação/discovery falhou antes da fixture: nunca inventar resultados funcionais.
        @"
# QA não executado

Resultado: NÃO EXECUTADO. Categoria: INFRAESTRUTURA/COMPILAÇÃO/DISCOVERY.
dotnet test terminou com código $testExitCode antes de gerar evidência dos cenários.
Consulte a saída do comando. Nenhum resultado funcional pode ser inferido.
"@ | Set-Content -LiteralPath (Join-Path $reportPath 'RESUMO.md') -Encoding utf8
    }
    Write-Host "Relatórios: $reportPath"
}
finally { $env:QA_REPORT_DIRECTORY = $previousReport }
exit $testExitCode
