# TIDE Island Validation - Full evidence capture
# Validates against the EXACT assertions in the Unity verification test files

$ErrorActionPreference = "Continue"
$script:allChecks = New-Object System.Collections.Generic.List[object]
$script:failCount = 0
$script:passCount = 0
$script:groupedOutput = @{}

function Record-Check {
    param(
        [string]$Island,
        [string]$Test,
        [string]$Name,
        [bool]$Ok,
        [string]$Detail = ""
    )
    $entry = [PSCustomObject]@{
        Island = $Island
        Test = $Test
        Name = $Name
        Ok = $Ok
        Detail = $Detail
    }
    $script:allChecks.Add($entry)
    if ($Ok) { $script:passCount++ } else { $script:failCount++ }

    $key = "$Island :: $Test"
    if (-not $script:groupedOutput.ContainsKey($key)) {
        $script:groupedOutput[$key] = New-Object System.Collections.Generic.List[object]
    }
    $script:groupedOutput[$key].Add($entry)
}

# Build a guid -> filepath map for the whole project
function Build-GuidMap {
    param([string]$Folder)
    $map = @{}
    Get-ChildItem -Path $Folder -Recurse -Filter "*.meta" -ErrorAction SilentlyContinue | ForEach-Object {
        $m = Get-Content $_.FullName -Raw -ErrorAction SilentlyContinue
        if ($m -match 'guid:\s+([a-f0-9]{32})') {
            $map[$matches[1]] = $_.FullName -replace '\.meta$', ''
        }
    }
    return $map
}

$resourceMap = Build-GuidMap "Assets/Resources"
Write-Host "Resource asset map: $($resourceMap.Count) entries"

# Per the IslandThemeRegistry progression order and aliases
$islands = @(
    @{ id = "island_lust";     file = "island_lust";    vice = "Lust";     bossId = "lust_boss";     puzzlePrefix = "lust";     encPrefix = "lust" }
    @{ id = "island_gluttony"; file = "island_gluttony";vice = "Gluttony"; bossId = "gluttony_boss"; puzzlePrefix = "gluttony"; encPrefix = "gluttony" }
    @{ id = "island_greed";    file = "island_greed";   vice = "Greed";    bossId = "greed_boss";    puzzlePrefix = "greed";    encPrefix = "greed" }
    @{ id = "island_sloth";    file = "island_desire";  vice = "Sloth";    bossId = "desire_boss";   puzzlePrefix = "desire";   encPrefix = "desire" }
    @{ id = "island_wrath";    file = "island_anger";   vice = "Wrath";    bossId = "anger_boss";    puzzlePrefix = "anger";    encPrefix = "anger" }
    @{ id = "island_envy";     file = "island_envy";    vice = "Envy";     bossId = "envy_boss";     puzzlePrefix = "envy";     encPrefix = "envy" }
    @{ id = "island_pride";    file = "island_ego";     vice = "Pride";    bossId = "ego_boss";      puzzlePrefix = "ego";      encPrefix = "ego" }
)

function Get-ResourceAsset {
    param([string]$Guid)
    if ($resourceMap.ContainsKey($Guid)) { return $resourceMap[$Guid] } else { return $null }
}

Write-Host ""
Write-Host "=== VSR-001: IslandContentVerificationTest ==="

# TestLoadExactIslandSet
$configsFound = @()
foreach ($island in $islands) {
    $cfgPath = "Assets/Resources/Islands/$($island.file).asset"
    if (Test-Path $cfgPath) {
        $configsFound += $island.id
    }
}
Record-Check "ALL" "VSR-001/TestLoadExactIslandSet" "7 configs present" ($configsFound.Count -eq 7) "$($configsFound.Count) found"

# Per-island validation
$bossGuidsList = @()
$primaryColorsList = @()

foreach ($island in $islands) {
    $cfgPath = "Assets/Resources/Islands/$($island.file).asset"
    if (-not (Test-Path $cfgPath)) {
        Record-Check $island.id "VSR-001" "config exists" $false "missing $cfgPath"
        continue
    }

    $cfg = Get-Content $cfgPath -Raw

    # vice name
    $viceOk = $cfg -match "viceName: $($island.vice)\b"
    Record-Check $island.id "VSR-001" "viceName=$($island.vice)" $viceOk ""

    # parse encounter blocks - split on each "- encounterId:" line
    $entryTexts = $cfg -split '(?m)(?=^\s*-\s+encounterId:)'
    $entries = @()
    foreach ($t in $entryTexts) {
        if ($t -match 'encounterId:') { $entries += ,$t }
    }
    if ($entries.Count -lt 9) {
        Record-Check $island.id "VSR-001" "9 encounters parsed" $false "only $($entries.Count) parsed"
        continue
    }
    Record-Check $island.id "VSR-001/TestEncounterSequenceAndRestorationBudgets" "encounters=9" ($entries.Count -eq 9) "count=$($entries.Count)"

    $encounterInfo = @()
    for ($i = 0; $i -lt $entries.Count; $i++) {
        $entry = $entries[$i]
        $idMatch = [regex]::Match($entry, 'encounterId:\s+(\S+)')
        $typeMatch = [regex]::Match($entry, 'type:\s+(\d+)')
        $rvMatch = [regex]::Match($entry, 'restorationValue:\s+([\d.]+)')
        $hasEncCfg = $entry -match 'encounterConfig:\s+\{fileID: 11400000'
        $hasPuz = $entry -match 'puzzleData:\s+\{fileID: 11400000'
        $encCfgMatch = [regex]::Match($entry, 'encounterConfig:\s*\{fileID: 11400000, guid:\s+([a-f0-9]{32})')
        $puzMatch = [regex]::Match($entry, 'puzzleData:\s*\{fileID: 11400000, guid:\s+([a-f0-9]{32})')

        $encounterInfo += [PSCustomObject]@{
            Id = if ($idMatch.Success) { $idMatch.Groups[1].Value } else { "" }
            Type = if ($typeMatch.Success) { [int]$typeMatch.Groups[1].Value } else { -1 }
            Rv = if ($rvMatch.Success) { [double]$rvMatch.Groups[1].Value } else { 0.0 }
            HasEncounterConfig = $hasEncCfg
            HasPuzzleData = $hasPuz
            EncounterConfigGuid = if ($encCfgMatch.Success) { $encCfgMatch.Groups[1].Value } else { "" }
            PuzzleDataGuid = if ($puzMatch.Success) { $puzMatch.Groups[1].Value } else { "" }
        }
    }

    $combat = 0.0
    $puzzle = 0.0
    for ($j = 0; $j -lt 8; $j++) {
        $e = $encounterInfo[$j]
        if ($j % 2 -eq 0) {
            Record-Check $island.id "VSR-001" "enc[$j] type=Combat (actual=$($e.Type))" ($e.Type -eq 0) "id=$($e.Id)"
            Record-Check $island.id "VSR-001" "enc[$j] has EncounterConfig" $e.HasEncounterConfig "id=$($e.Id)"
            $combat += $e.Rv
        } else {
            Record-Check $island.id "VSR-001" "enc[$j] type=Puzzle (actual=$($e.Type))" ($e.Type -eq 1) "id=$($e.Id)"
            Record-Check $island.id "VSR-001" "enc[$j] has PuzzleData" $e.HasPuzzleData "id=$($e.Id)"
            $puzzle += $e.Rv
        }
    }

    # boss
    $boss = $encounterInfo[8]
    $bossIdOk = $boss.Id -and $boss.Id.IndexOf("boss", [System.StringComparison]::OrdinalIgnoreCase) -ge 0
    Record-Check $island.id "VSR-001" "boss id contains 'boss' ($($boss.Id))" $bossIdOk ""
    Record-Check $island.id "VSR-001" "boss type=Combat" ($boss.Type -eq 0) "type=$($boss.Type)"
    Record-Check $island.id "VSR-001" "boss has EncounterConfig" $boss.HasEncounterConfig ""
    Record-Check $island.id "VSR-001" "boss restoration=0.25" ([math]::Abs($boss.Rv - 0.25) -lt 0.001) "rv=$($boss.Rv)"

    if ($island.id -eq "island_gluttony") {
        $combatExpected = 0.375
        $puzzleExpected = 0.375
    } else {
        $combatExpected = 0.5
        $puzzleExpected = 0.25
    }
    Record-Check $island.id "VSR-001" "combat contribution" ([math]::Abs($combat - $combatExpected) -lt 0.001) "expected=$combatExpected actual=$combat"
    Record-Check $island.id "VSR-001" "puzzle contribution" ([math]::Abs($puzzle - $puzzleExpected) -lt 0.001) "expected=$puzzleExpected actual=$puzzle"
    $preBoss = $combat + $puzzle
    Record-Check $island.id "VSR-001" "pre-boss total = 0.75" ([math]::Abs($preBoss - 0.75) -lt 0.001) "actual=$preBoss"
    $total = $preBoss + $boss.Rv
    Record-Check $island.id "VSR-001" "total restoration = 1.0" ([math]::Abs($total - 1.0) -lt 0.001) "actual=$total"

    # capture last encounter boss GUID
    $bossGuidsList += $boss.EncounterConfigGuid

    # collect primary color
    $colorMatch = [regex]::Match($cfg, 'vicePrimaryColor:\s*\{r:\s*([\d.]+),\s*g:\s*([\d.]+),\s*b:\s*([\d.]+)')
    if ($colorMatch.Success) {
        $primaryColorsList += ,@($island.id, [double]$colorMatch.Groups[1].Value, [double]$colorMatch.Groups[2].Value, [double]$colorMatch.Groups[3].Value)
    }

    # Resolve PuzzleData GUIDs
    $puzzleRefs = @()
    for ($j = 1; $j -lt 8; $j += 2) {
        $e = $encounterInfo[$j]
        if ($e.PuzzleDataGuid) { $puzzleRefs += $e.PuzzleDataGuid }
    }
    $hasAllEqual = $false
    $hasPercentage = $false
    $uniquePuzzles = @{}
    foreach ($guid in $puzzleRefs) {
        $pPath = Get-ResourceAsset $guid
        if ($pPath -and (Test-Path $pPath)) {
            $puz = Get-Content $pPath -Raw
            $wt = [regex]::Match($puz, '(?ms)winCondition:\s*\n\s*type:\s+(\d+)')
            if ($wt.Success) {
                $t = [int]$wt.Groups[1].Value
                if ($t -eq 0) { $hasAllEqual = $true }
                if ($t -eq 1) { $hasPercentage = $true }
            }
            $uniquePuzzles[$guid] = $pPath
        }
    }
    Record-Check $island.id "VSR-001/TestPuzzleDataVariation" "unique puzzle count >= 2" ($uniquePuzzles.Count -ge 2) "count=$($uniquePuzzles.Count)"
    Record-Check $island.id "VSR-001/TestPuzzleDataVariation" "has PercentageAtTarget" $hasPercentage ""
    Record-Check $island.id "VSR-001/TestPuzzleDataVariation" "has AllEqualToTarget" $hasAllEqual ""

    # Ancient text
    $textPath = "Assets/Resources/AncientTexts/text_$($island.puzzlePrefix)_intro.asset"
    $textValid = $false
    if (Test-Path $textPath) {
        $t = Get-Content $textPath -Raw
        $expectedTextId = "$($island.puzzlePrefix)_intro_fragment"
        $textValid = ($t -match "textId:\s+$expectedTextId") -and ($t -match "title:") -and ($t -match "body:")
    }
    Record-Check $island.id "VSR-001/TestAncientTextsPerIslandVice" "ancient text $textPath valid" $textValid ""

    # Scene file - use the actual island id, not the file asset name
    $sceneId = if ($island.id -eq "island_lust") { "1" } else { $island.id.Substring("island_".Length) }
    $sceneFile = "Assets/Scenes/level_$sceneId.unity"
    $sceneOk = Test-Path $sceneFile
    Record-Check $island.id "VSR-001/per-island-TestSceneExists" "scene file $sceneFile" $sceneOk ""

    # ===== Per-island tests =====
    # Greed test
    if ($island.id -eq "island_greed") {
        $combatCount = 0
        $puzzleCount = 0
        foreach ($e in $encounterInfo) {
            if ($e.Type -eq 0) { $combatCount++ } else { $puzzleCount++ }
        }
        Record-Check $island.id "VSR-011/TestEncounterLayout" "5 combat encounters" ($combatCount -eq 5) "count=$combatCount"
        Record-Check $island.id "VSR-011/TestEncounterLayout" "4 puzzle encounters" ($puzzleCount -eq 4) "count=$puzzleCount"
    }
    # Sloth/Wrath/Envy/Pride generic tests
    if ($island.id -in @("island_sloth", "island_wrath", "island_envy", "island_pride")) {
        $combatCount = 0
        $puzzleCount = 0
        foreach ($e in $encounterInfo) {
            if ($e.Type -eq 0) { $combatCount++ } else { $puzzleCount++ }
        }
        Record-Check $island.id "VSR-01X/TestEncounterLayout" "5 combat encounters" ($combatCount -eq 5) "count=$combatCount"
        Record-Check $island.id "VSR-01X/TestEncounterLayout" "4 puzzle encounters" ($puzzleCount -eq 4) "count=$puzzleCount"
    }
}

# TestDistinctViceColors
$distinct = $true
for ($i = 0; $i -lt $primaryColorsList.Count; $i++) {
    for ($j = $i + 1; $j -lt $primaryColorsList.Count; $j++) {
        $a = $primaryColorsList[$i]
        $b = $primaryColorsList[$j]
        $dr = [math]::Abs($a[1] - $b[1])
        $dg = [math]::Abs($a[2] - $b[2])
        $db = [math]::Abs($a[3] - $b[3])
        if ($dr -lt 0.01 -and $dg -lt 0.01 -and $db -lt 0.01) {
            $distinct = $false
            Write-Host "  Color collision: $($a[0]) vs $($b[0])"
            break
        }
    }
}
Record-Check "ALL" "VSR-001/TestDistinctViceColors" "all vice primary colors distinct" $distinct ""

# TestBossEncounterUniqueness
$uniqueBoss = ($bossGuidsList | Sort-Object -Unique | Measure-Object).Count
$expectedUnique = $bossGuidsList.Count
Record-Check "ALL" "VSR-001/TestBossEncounterUniqueness" "boss EncounterConfigs unique" ($uniqueBoss -eq $expectedUnique) "$expectedUnique expected, $uniqueBoss actual"

# ==================== VSR-010: Gluttony specific ====================
Write-Host ""
Write-Host "=== VSR-010: GluttonyIslandVerificationTest ==="
$gluttonyBossGuid = $null
foreach ($island in $islands) {
    if ($island.id -eq "island_gluttony") {
        $cfg = Get-Content "Assets/Resources/Islands/$($island.file).asset" -Raw
        $entries = $cfg -split '(?m)(?=^\s*-\s+encounterId:)'
        $filtered = @()
        foreach ($t in $entries) { if ($t -match 'encounterId:') { $filtered += ,$t } }
        $entries = $filtered
        $bossEnc = $entries[8]
        $b = [regex]::Match($bossEnc, 'encounterConfig:\s*\{fileID: 11400000, guid:\s+([a-f0-9]{32})')
        $gluttonyBossGuid = $b.Groups[1].Value
    }
}

if ($gluttonyBossGuid) {
    $bossPath = Get-ResourceAsset $gluttonyBossGuid
    if ($bossPath -and (Test-Path $bossPath)) {
        $bossEnc = Get-Content $bossPath -Raw
        $hasEnemies = $bossEnc -match 'enemies:\s*\n\s*-\s*\{fileID: 11400000'
        Record-Check "island_gluttony" "VSR-010/TestGluttonyBossUsesDevourSkill" "boss encounter has enemies" $hasEnemies ""

        $e1 = [regex]::Match($bossEnc, 'enemies:\s*\n\s*-\s*\{fileID: 11400000, guid:\s+([a-f0-9]{32})')
        $enemyGuid = $e1.Groups[1].Value
        $enemyPath = Get-ResourceAsset $enemyGuid
        if ($enemyPath -and (Test-Path $enemyPath)) {
            $enemy = Get-Content $enemyPath -Raw
            $displayMatch = [regex]::Match($enemy, 'displayName:\s+(.+)')
            $displayName = if ($displayMatch.Success) { $displayMatch.Groups[1].Value.Trim() } else { "" }
            Record-Check "island_gluttony" "VSR-010/TestGluttonyBossUsesDevourSkill" "boss displayName = The Devourer" ($displayName -eq "The Devourer") "actual='$displayName'"

            $hasLifestealReal = $false
            $skillsMatch = [regex]::Match($enemy, '(?ms)skills:\s*\n(?<skills>(?:\s*-\s*\{fileID: 11400000, guid:\s+[a-f0-9]+[^\n]*\n)+)')
            if ($skillsMatch.Success) {
                $sguids = [regex]::Matches($skillsMatch.Value, 'guid:\s+([a-f0-9]{32})')
                foreach ($sg in $sguids) {
                    $skillPath = Get-ResourceAsset $sg.Groups[1].Value
                    if ($skillPath -and (Test-Path $skillPath)) {
                        $sk = Get-Content $skillPath -Raw
                        if ($sk -match 'restoreCasterPercentOfDamage:\s+([\d.]+)') {
                            if ([double]$matches[1] -gt 0) {
                                $hasLifestealReal = $true
                            }
                        }
                    }
                }
            }
            Record-Check "island_gluttony" "VSR-010/TestGluttonyBossUsesDevourSkill" "boss has lifesteal skill (restoreCasterPercentOfDamage > 0)" $hasLifestealReal ""
        } else {
            Record-Check "island_gluttony" "VSR-010" "boss enemy asset resolvable" $false "guid $enemyGuid not found in Resources"
        }
    } else {
        Record-Check "island_gluttony" "VSR-010" "boss encounter asset resolvable" $false "guid $gluttonyBossGuid not found in Resources"
    }
}

# Gluttony consumption puzzles
$gluttonyPuzzles = Get-ChildItem "Assets/Resources/Puzzles/puzzle_gluttony_p*.asset"
$consumptionCount = 0
foreach ($p in $gluttonyPuzzles) {
    $pc = Get-Content $p.FullName -Raw
    $hasCons = ($pc -match 'enableConsumption:\s+1')
    $amt = [regex]::Match($pc, 'consumptionAmount:\s+(\d+)').Groups[1].Value
    if ($hasCons -and ($amt -as [int]) -ge 1) { $consumptionCount++ }
}
Record-Check "island_gluttony" "VSR-010/TestGluttonyPuzzlesUseConsumption" ">=3 puzzles use consumption" ($consumptionCount -ge 3) "actual=$consumptionCount"

# ==================== VSR-016: Sloth status effect ====================
Write-Host ""
Write-Host "=== VSR-016: SlothStatusEffectTestSuite ==="
$statusEffectCs = Get-Content "Assets/StatusEffect.cs" -Raw
Record-Check "ALL" "VSR-016" "StatusEffectType.Slow enum" ($statusEffectCs -match '\bSlow\b') ""
Record-Check "ALL" "VSR-016" "StatusEffectType.Drowsy enum" ($statusEffectCs -match '\bDrowsy\b') ""

$combatUnitCs = Get-Content "Assets/CombatUnit.cs" -Raw
Record-Check "ALL" "VSR-016" "CombatUnit.GetEffectiveSpeed()" ($combatUnitCs -match 'public int GetEffectiveSpeed\(\)') ""
Record-Check "ALL" "VSR-016" "CombatUnit.ShouldSkipTurn()" ($combatUnitCs -match 'public bool ShouldSkipTurn\(\)') ""
Record-Check "ALL" "VSR-016" "CombatUnit.DebugSpeed setter" ($combatUnitCs -match 'internal int DebugSpeed') ""

# ==================== VSR-017: Envy mirror ====================
Write-Host ""
Write-Host "=== VSR-017: EnvyMirrorTestSuite ==="
$battleManagerCs = Get-Content "Assets/BattleManager.cs" -Raw
Record-Check "ALL" "VSR-017" "BattleManager.EnableEnvyMirror" ($battleManagerCs -match 'public bool EnableEnvyMirror') ""
Record-Check "ALL" "VSR-017" "BattleManager.RegisterUnit(CombatUnit)" ($battleManagerCs -match 'public void RegisterUnit\(CombatUnit unit\)') ""
Record-Check "ALL" "VSR-017" "BattleManager.lastAttacker field" ($battleManagerCs -match 'private CombatUnit lastAttacker') ""
Record-Check "ALL" "VSR-017" "BattleManager.lastPlayerSkill field" ($battleManagerCs -match 'private SkillData lastPlayerSkill') ""
Record-Check "ALL" "VSR-017" "BattleManager.isBossEncounter field" ($battleManagerCs -match 'private bool isBossEncounter') ""
Record-Check "ALL" "VSR-017" "BattleManager.ComputeEnemyAction method" ($battleManagerCs -match 'ComputeEnemyAction\(') ""

# ==================== VSR-018: Greed economy ====================
Write-Host ""
Write-Host "=== VSR-018: GreedEconomyTestSuite ==="
$puzzleDataCs = Get-Content "Assets/PuzzleData.cs" -Raw
Record-Check "ALL" "VSR-018" "PuzzleData.coinTileYield field" ($puzzleDataCs -match 'public int coinTileYield') ""
Record-Check "ALL" "VSR-018" "PuzzleData.coinTileYield default = 2" ($puzzleDataCs -match 'coinTileYield\s*=\s*2') ""
Record-Check "ALL" "VSR-018" "PuzzleData.enableGreedEconomy field" ($puzzleDataCs -match 'public bool enableGreedEconomy') ""

$heroProgCs = Get-Content "Assets/HeroProgressionManager.cs" -Raw
Record-Check "ALL" "VSR-018" "HeroProgressionManager.Currency property" ($heroProgCs -match 'public int Currency') ""
Record-Check "ALL" "VSR-018" "HeroProgressionManager.SetCurrency()" ($heroProgCs -match 'public void SetCurrency') ""
Record-Check "ALL" "VSR-018" "HeroProgressionManager.TrySpendCurrency()" ($heroProgCs -match 'public bool TrySpendCurrency') ""

$skillDataCs = Get-Content "Assets/SkillData.cs" -Raw
Record-Check "ALL" "VSR-018" "SkillData.currencyStealAmount field" ($skillDataCs -match 'public int currencyStealAmount') ""

# ==================== Final report ====================
Write-Host ""
Write-Host "================================================================"
Write-Host "VALIDATION SUMMARY"
Write-Host "================================================================"
Write-Host "Total checks: $($script:passCount + $script:failCount)"
Write-Host "Passed:       $script:passCount"
Write-Host "Failed:       $script:failCount"
Write-Host ""

if ($script:failCount -gt 0) {
    Write-Host "=== FAILURES ==="
    $script:allChecks | Where-Object { -not $_.Ok } | ForEach-Object {
        Write-Host "  [$($_.Island)] $($_.Test) :: $($_.Name) :: $($_.Detail)"
    }
    exit 1
} else {
    Write-Host "ALL CHECKS PASSED"
    exit 0
}
