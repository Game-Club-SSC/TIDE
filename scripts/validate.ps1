$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "../validate.ps1")
exit $LASTEXITCODE
