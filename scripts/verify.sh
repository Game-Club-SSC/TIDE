#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
harness="$repo_root/Verification/PhoneSecurityHarness/PhoneSecurityHarness.csproj"
verify_tmp="${TMPDIR:-/tmp}/tide-verify"
export DOTNET_CLI_HOME="$verify_tmp/dotnet-home"
export NUGET_PACKAGES="$verify_tmp/nuget-packages"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES"

dotnet run --project "$harness" --configuration Release

# Unity writes this project file. Build it only on hosts with this project's
# checked-in Unity version, since hosted CI has neither Unity nor a license.
unity_core="/Applications/Unity/Hub/Editor/6000.3.7f1/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine/UnityEngine.CoreModule.dll"
if [[ -f "$unity_core" && -f "$repo_root/Assembly-CSharp.csproj" ]]; then
    dotnet build "$repo_root/Assembly-CSharp.csproj" --configuration Debug
else
    echo "Unity 6000.3.7f1 refs not found; skipped the generated editor project build."
fi
