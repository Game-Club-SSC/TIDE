# Combat source verification

Run from the repository root with a .NET 8+ SDK:

```sh
dotnet run --project Verification/CombatHarness/CombatHarness.csproj -- "$PWD"
```

The executable links the shipped `Assets/BattleManager.cs`, `CombatUnit`, elemental
matrix, statuses, momentum, skills, Tide Breaks, difficulty, balance, enemy data,
and enemy stat scaler. It also executes the existing battle/unit/Tide Break suites,
the 11 adversarial combat cases, and the new combat readiness regressions. It exits
nonzero when a check fails.

The harness has explicit Unity and assertion adapters. It does **not** run the
Unity Editor, NUnit runner, rendering, coroutine timing, real scene transitions,
or Unity asset reference/import resolution. Resource tests read scalar fields
from shipped YAML; they cannot validate referenced ScriptableObjects. The QTE
scenario executes the actual BattleManager session/callback branch against a
minimal QTESession adapter. The completion scenario checks exactly-once delivery
to a GameStateManager adapter; progression/save integration requires their own
source checks and Editor tests.

As of this change, 99 cases pass. A temporary negative control restoring the old
free skill-clash MP and enemy area Tide Break faction defects failed four cases.

In Unity, run `CombatReadinessRegressionTest` → **Run Combat Readiness Regressions**
and `AdversarialCombatChallengerRunner` → **Run Adversarial Combat Tests**. Then
play a generated encounter on each island and verify the following:

- A faster ordinary action executes before a slower mutual clash; dead/stunned
  actors cannot clash, and a unit that already acted cannot act again via retargeting.
- One-turn buffs, drowsy, stun, shields and taunt persist through their turn and
  expire after round completion. Poison/burn tick once for each participating turn.
- Skill clashes spend MP once and use authored damage/status effects. A neutral
  clash QTE pauses the queue, resumes at the next speed slot, and does not repeat
  resource spending or status ticks.
- Each side's area Tide Break hits its opponents; a consumed shared gauge cannot
  fund another queued Tide Break. Hero HUD choices only expose owned/unlocked skills.
- Hardcore restrictions resolve through the persistent difficulty singleton.
  Island vice profiles use assigned assets or the existing `CreateDefault` weights.
- Envy bosses copy the player's last skill before their ordinary skill selection,
  including the 0.7 damage modifier for area attacks.
- NG+ enemies scale from base data and HP/MP are full at spawn. Repeated encounters
  do not compound live stats; NG+ XP is applied by progression once.
- A terminal battle result returns to exploration/puzzle exactly once with the
  expected rewards and restoration. Test scene unloading during a pending QTE, and disabling/re-enabling the battle
  component: teardown does no damage; re-enabling settles the cancelled QTE as a failure.

Five review passes covered turn/QTE flow, status ownership/durations, resource
and faction targeting, numeric/boss/NG+ boundaries, and integration/test guards.
An independent integration reviewer also reviewed the staged clash changes.
