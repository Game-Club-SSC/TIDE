#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
verify_tmp="${TIDE_VERIFY_TMP:-$repo_root/work/verify}"
export DOTNET_CLI_HOME="$verify_tmp/dotnet-home"
export NUGET_PACKAGES="$verify_tmp/nuget-packages"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES"

for harness in PhoneSecurityHarness PersistenceHarness ProgressionHarness CombatHarness PuzzleIslandHarness NarrativeHarness HeroGearHarness InputHarness; do
    echo "Running $harness (shipped sources with explicit engine/service adapters)."
    dotnet run --project "$repo_root/Verification/$harness/$harness.csproj" --configuration Release
done
dotnet run --project "$repo_root/Verification/InputHarness/InputHarness.csproj" --configuration Release -p:GamepadDefines=ENABLE_INPUT_SYSTEM
dotnet run --project "$repo_root/Verification/SceneIntegration/SceneIntegration.csproj" --configuration Release -- "$repo_root"

if command -v pwsh >/dev/null 2>&1; then
    for check in validate bug_audit runtime_sim; do
        pwsh -NoProfile -File "$repo_root/scripts/$check.ps1"
    done
else
    echo "PowerShell unavailable; the three PowerShell source/asset checks were skipped."
fi

# Unity writes this project file. Build it only on hosts with this project's
# checked-in Unity version, since hosted CI has neither Unity nor a license.
unity_core="/Applications/Unity/Hub/Editor/6000.3.7f1/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine/UnityEngine.CoreModule.dll"
if [[ -f "$unity_core" && -f "$repo_root/Assembly-CSharp.csproj" ]]; then
    dotnet build "$repo_root/Assembly-CSharp.csproj" --configuration Debug
else
    echo "Unity 6000.3.7f1 refs not found; skipped the generated editor project build."
fi
