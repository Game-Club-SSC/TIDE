$ErrorActionPreference = "Stop"
& (Join-Path $PSScriptRoot "../runtime_sim.ps1")
exit $LASTEXITCODE
