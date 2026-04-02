# Issue #112 Umbrella Scope Gate (GDD Alignment)

This umbrella issue tracks scope alignment only. It documents what is in-slice for the current vertical-slice milestone and where deferred systems are tracked.

## Current-Slice Baseline

- Core loop baseline: explore island -> clear combat and Tide puzzles -> restore island -> unlock boss at 75% restoration.
- Combat baseline: elemental advantage, clash flow, momentum bar, Tide Break.
- Restoration baseline for current slice: combat contributes up to 50%, with the remaining restoration completed through Tide puzzles/environmental balancing (tracked here as a 50/50 planning baseline; final balancing remains TBD in GDD notes).

## Milestone Scope Notes

In scope for this umbrella branch:

- Documentation-level scope gate updates only.
- Explicit defer tracking links to focused implementation issues.
- Consolidated acceptance checklist for current GDD-aligned slice.

Out of scope for this umbrella branch:

- Re-implementing or duplicating code deferrals already handled in dedicated issue branches.
- New gameplay systems beyond current GDD minimums for vertical-slice validation.

## Deferred Systems Tracking

The systems below are deferred from this milestone and tracked separately:

| Deferred System | GDD Alignment Reason | Tracking Issue |
|---|---|---|
| In-battle party swap convenience paths | GDD party switching is between battles, not in active combat flow. | #106 |
| Cosmetic progression and style economy pathways | Not required for core loop acceptance (explore, combat/puzzle restoration, 75% boss gate). | #107 |
| Expanded overworld roaming AI complexity | Reliable encounter triggering is sufficient for current slice acceptance; advanced roaming behavior is deferred polish scope. | #108 |
| Expanded map zoom and marker stack UX | Not required to validate island restoration loop acceptance; map readability beyond baseline navigation is deferred polish scope. | #109 |
| Extended combat breadth beyond baseline loop (extra variants/depth) | Vertical slice requires elemental advantage, clash, momentum, and Tide Break baseline only. | #110 |

## Checklist References

- Acceptance checklist: this file, section `Consolidated Acceptance Checklist (Current Slice)`.
- Regression checklist anchor: `Assets/Docs/Island1-Scope.md` (`Definition of Done` and restoration/boss gate rows).
- Test execution reference: `AGENTS.md` test list (`BattleFlowVerificationTest`, `RestorationTrackerTest`, `BossEncounterGateTest`, `RestorationThresholdGateTest`, and related verification tests).

## Consolidated Acceptance Checklist (Current Slice)

- [ ] Scope notes in docs clearly separate in-scope slice systems from deferred systems.
- [ ] Deferred systems are linked to dedicated tracking issues instead of duplicated in this branch.
- [ ] Core loop remains aligned to GDD: explore island, solve Tide puzzles, clear combat, restore island.
- [ ] Restoration model remains aligned to current slice baseline: 50/50 combat-puzzle contribution with boss unlock at 75%, while broader balancing remains TBD in GDD notes.
- [ ] Combat baseline remains aligned to GDD minimums: elemental advantage, clash flow, momentum bar, Tide Break.
- [ ] No umbrella-branch changes expand non-GDD cosmetic or extended combat polish scope.
- [ ] Milestone test plan excludes deferred polish systems in #106-#110 unless explicitly promoted into acceptance scope.
- [ ] Deferred systems remain tracked and prioritized in their dedicated issues, not re-scoped into this umbrella branch.
