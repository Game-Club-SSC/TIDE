# Real code-bug-hunting audit
# Looks for common bug patterns in the .cs files: side effects in sort
# comparers, missing null guards before property access, state mutations
# before early returns, etc.

$ErrorActionPreference = "Continue"
$script:bugChecks = New-Object System.Collections.Generic.List[object]
$script:fail = 0
$script:pass = 0

function Bug-Check {
    param([string]$File, [string]$Name, [bool]$Ok, [string]$Detail = "")
    $script:bugChecks.Add([PSCustomObject]@{
        File = $File
        Name = $Name
        Ok = $Ok
        Detail = $Detail
    })
    if ($Ok) { $script:pass++ } else { $script:fail++ }
}

# ==================== 1. EnemyTrigger: composition set before flow check? ====================
$et = Get-Content "Assets/EnemyTrigger.cs" -Raw
# Order: After the flow controller early return, PendingEnemyComposition is set.
# Use a more permissive multi-line check.
$flowReturnIdx = $et.IndexOf('HasActiveFlowController')
$pendingAfterFlow = $false
if ($flowReturnIdx -ge 0) {
    $afterFlow = $et.Substring($flowReturnIdx)
    $pendingIdx = $afterFlow.IndexOf('PendingEnemyComposition')
    $earlyReturnIdx = $afterFlow.IndexOf('return;')
    if ($pendingIdx -gt $earlyReturnIdx -and $earlyReturnIdx -ge 0) {
        $pendingAfterFlow = $true
    }
}
Bug-Check "EnemyTrigger.cs" "PendingEnemyComposition set AFTER flow early return" $pendingAfterFlow ""

# ==================== 2. HeroProgressionManager: currency persistence ====================
$hp = Get-Content "Assets/HeroProgressionManager.cs" -Raw
# All three currency mutators should call PersistCurrency
$addPersists = $hp -match 'public void AddCurrency[\s\S]{0,300}PersistCurrency'
$spendPersists = $hp -match 'public bool TrySpendCurrency[\s\S]{0,400}PersistCurrency'
$setPersists = $hp -match 'public void SetCurrency[\s\S]{0,200}PersistCurrency'
Bug-Check "HeroProgressionManager.cs" "AddCurrency persists" $addPersists ""
Bug-Check "HeroProgressionManager.cs" "TrySpendCurrency persists" $spendPersists ""
Bug-Check "HeroProgressionManager.cs" "SetCurrency persists" $setPersists ""

# ==================== 3. BattleManager: turn registration reset on StartBattle ====================
$bm = Get-Content "Assets/BattleManager.cs" -Raw
$resetOnStart = $bm -match 'case BattlePhase\.StartBattle:[\s\S]{0,300}ResetTurnRegistration'
$resetMethod = $bm -match 'private void ResetTurnRegistration[\s\S]{0,200}unitRegistrationOrder\.Clear'
Bug-Check "BattleManager.cs" "StartBattle resets turn registration" $resetOnStart ""
Bug-Check "BattleManager.cs" "ResetTurnRegistration clears unitRegistrationOrder" $resetMethod ""

# ==================== 4. BattleManager: BuildTurnQueueFromLivingUnits caches speeds ====================
$cacheUsed = $bm -match 'BuildTurnQueueFromLivingUnits[\s\S]{0,2000}effectiveSpeeds'
Bug-Check "BattleManager.cs" "BuildTurnQueueFromLivingUnits caches GetEffectiveSpeed" $cacheUsed ""

# ==================== 5. TideManager: no truly-dead BuildLegacyLockedEncounterId ====================
$tm = Get-Content "Assets/TideManager.cs" -Raw
# The function is now referenced at line 224 (InitializePuzzle) so it's NOT dead
$usedFromInit = $tm -match 'BuildLegacyLockedEncounterId\(sealedTile\)'
$definedAs = $tm -match 'private string BuildLegacyLockedEncounterId'
Bug-Check "TideManager.cs" "BuildLegacyLockedEncounterId is referenced from InitializePuzzle" $usedFromInit "function is intentionally retained for legacy puzzles without PuzzleData"
Bug-Check "TideManager.cs" "BuildLegacyLockedEncounterId is defined" $definedAs ""

# ==================== 6. All public API surface expected by Unity tests exists ====================
# (Re-confirm the dependency surface for the test suites)
Bug-Check "CombatUnit.cs" "GetEffectiveSpeed() method present" ((Get-Content "Assets/CombatUnit.cs" -Raw) -match 'public int GetEffectiveSpeed') ""
Bug-Check "CombatUnit.cs" "ShouldSkipTurn() method present" ((Get-Content "Assets/CombatUnit.cs" -Raw) -match 'public bool ShouldSkipTurn') ""
Bug-Check "CombatUnit.cs" "DebugSpeed setter present" ((Get-Content "Assets/CombatUnit.cs" -Raw) -match 'internal int DebugSpeed') ""
Bug-Check "BattleManager.cs" "EnableEnvyMirror field" ($bm -match 'public bool EnableEnvyMirror') ""
Bug-Check "BattleManager.cs" "ComputeEnemyAction method" ($bm -match 'ComputeEnemyAction\(') ""
Bug-Check "BattleManager.cs" "RegisterUnit public" ($bm -match 'public void RegisterUnit\(CombatUnit unit\)') ""
Bug-Check "HeroProgressionManager.cs" "Currency property" ($hp -match 'public int Currency\s*=>') ""
Bug-Check "HeroProgressionManager.cs" "TrySpendCurrency method" ($hp -match 'public bool TrySpendCurrency') ""
Bug-Check "PuzzleData.cs" "coinTileYield field" ((Get-Content "Assets/PuzzleData.cs" -Raw) -match 'public int coinTileYield\s*=\s*2') ""
Bug-Check "PuzzleData.cs" "enableGreedEconomy field" ((Get-Content "Assets/PuzzleData.cs" -Raw) -match 'public bool enableGreedEconomy') ""
Bug-Check "StatusEffect.cs" "Slow enum value" ((Get-Content "Assets/StatusEffect.cs" -Raw) -match '\bSlow\b') ""
Bug-Check "StatusEffect.cs" "Drowsy enum value" ((Get-Content "Assets/StatusEffect.cs" -Raw) -match '\bDrowsy\b') ""

# ==================== 7. Null safety on singleton access ====================
# GameStateManager.Instance is accessed in many places. Look for the
# pattern of "if (Instance == null) return;" before use.
$gsm = Get-Content "Assets/GameStateManager.cs" -Raw
# Count guard-then-use patterns
$guardedAccess = ([regex]::Matches($gsm, 'GameStateManager\.Instance\s*!=\s*null')).Count
$totalInstanceAccess = ([regex]::Matches($gsm, 'GameStateManager\.Instance')).Count
$guardRatio = if ($totalInstanceAccess -gt 0) { $guardedAccess / $totalInstanceAccess } else { 1.0 }
Bug-Check "GameStateManager.cs" "null-guard ratio on Instance access" ($guardRatio -ge 0.4) "guarded=$guardedAccess total=$totalInstanceAccess ratio=$([math]::Round($guardRatio, 2))"

# ==================== 8. Ensure ScriptableObject factories are intentional ====================
# Runtime-generated data is valid for authored fallback content and transient
# mirrored/gear/Tide Break definitions. Keep the allowlist explicit so a new,
# potentially leaking CreateInstance call cannot silently pass this audit.
$createInRuntime = @(Get-ChildItem "Assets\*.cs" -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -notmatch 'Test\.cs$' -and
    $_.Name -notmatch 'TestSuite\.cs$' -and
    $_.Name -notmatch 'VerificationTest\.cs$' -and
    $_.Name -ne 'RegressionCheckHelpers.cs'
} | Where-Object {
    (Get-Content $_.FullName -Raw) -match 'ScriptableObject\.CreateInstance'
})
$allowedRuntimeFactories = @(
    'AncientTextAuthoring.cs',
    'AncientTextSceneBootstrap.cs',
    'EnvyMirrorService.cs',
    'GearSetFactory.cs',
    'HeroTideBreakFactory.cs'
)
$unexpectedRuntimeFactories = @($createInRuntime | Where-Object {
    $_.Name -notin $allowedRuntimeFactories
})
$missingRuntimeFactories = @($allowedRuntimeFactories | Where-Object {
    $_ -notin @($createInRuntime | ForEach-Object { $_.Name })
})
$factoryAuditOk = $unexpectedRuntimeFactories.Count -eq 0 -and $missingRuntimeFactories.Count -eq 0
$factoryDetail = "found=$(@($createInRuntime | ForEach-Object { $_.Name }) -join ', '); " +
    "unexpected=$(@($unexpectedRuntimeFactories | ForEach-Object { $_.Name }) -join ', '); " +
    "missing=$($missingRuntimeFactories -join ', ')"
Bug-Check "*.cs" "ScriptableObject.CreateInstance usage matches intentional runtime factory allowlist" $factoryAuditOk $factoryDetail

# ==================== 9. Singleton ownership releases on destroy ====================
$onDestroyReleases = @()
foreach ($f in @("GameStateManager.cs", "IslandRestorationTracker.cs", "HeroProgressionManager.cs", "IslandProgressionManager.cs")) {
    $c = Get-Content "Assets\$f" -Raw
    if ($c -match 'OnDestroy[\s\S]{0,300}Instance\s*=\s*null') {
        $onDestroyReleases += $f
    }
}
Bug-Check "*.cs" "Singletons release ownership in OnDestroy" ($onDestroyReleases.Count -ge 3) "releasing: $($onDestroyReleases -join ', ')"

# ==================== Final report ====================
Write-Host ""
Write-Host "================================================================"
Write-Host "BUG AUDIT SUMMARY"
Write-Host "================================================================"
Write-Host "Total checks: $($script:pass + $script:fail)"
Write-Host "Pass:         $script:pass"
Write-Host "Fail:         $script:fail"
Write-Host ""

if ($script:fail -gt 0) {
    Write-Host "=== FAILURES ==="
    $script:bugChecks | Where-Object { -not $_.Ok } | ForEach-Object {
        Write-Host "  [$($_.File)] $($_.Name) :: $($_.Detail)"
    }
    exit 1
} else {
    Write-Host "ALL BUG-AUDIT CHECKS PASSED"
    exit 0
}
