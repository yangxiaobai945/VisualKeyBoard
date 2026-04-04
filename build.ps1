param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$slnxPath = Join-Path $scriptDir "VisualKeyBoard.slnx"
$slnPath = Join-Path $scriptDir "VisualKeyBoard.sln"

if (Test-Path $slnxPath) {
    $solutionPath = $slnxPath
} elseif (Test-Path $slnPath) {
    $solutionPath = $slnPath
} else {
    throw "Solution file not found. Expected one of: $slnxPath, $slnPath"
}

Write-Host "==> Restoring packages..." -ForegroundColor Cyan
dotnet restore $solutionPath

Write-Host "==> Building solution ($Configuration)..." -ForegroundColor Cyan
dotnet build $solutionPath -c $Configuration --no-restore

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

Write-Host "==> Build succeeded." -ForegroundColor Green
