$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "../bug_audit.ps1")
exit $LASTEXITCODE
