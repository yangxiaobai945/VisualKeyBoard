param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionPath = Join-Path $scriptDir "VisualKeyBoard.sln"

if (-not (Test-Path $solutionPath)) {
    throw "Solution file not found: $solutionPath"
}

Write-Host "==> Restoring packages..." -ForegroundColor Cyan
dotnet restore $solutionPath

Write-Host "==> Building solution ($Configuration)..." -ForegroundColor Cyan
dotnet build $solutionPath -c $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "==> Build succeeded." -ForegroundColor Green
