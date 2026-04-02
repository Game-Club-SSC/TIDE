# Repo Scope Audit vs Game Design Document

Date: 2026-04-01

This audit lists candidate feature creep relative to `GameDesignDocument_TIDE.md`. The goal is to keep vertical-slice scope focused on the documented core loop.

## Candidate Scope Creep

- In-battle party swap flow (`BattleEscapeMenu`, `PartySwapPanel`) exceeds GDD guidance that party switching happens between battles.
- Cosmetic progression/premium-style systems (`PlayerCustomizationUI`, `FuturisticSpriteLibrary` cosmetic XP pathways) are not described in the GDD core systems.
- Advanced status-effect breadth and expanded targeting matrix go beyond current documented combat minimums.
- Expanded map zoom/marker stack is useful but not required by documented core loop validation.
- Highly complex roaming AI behavior on overworld enemies increases implementation surface beyond strict MVP needs.

## Recommendation

Treat the above systems as defer-candidates unless explicitly required by current milestone acceptance criteria (core loop validation, restoration gate validation, and combat baseline validation).

Current-slice baseline reference for this recommendation:

- Core loop: explore -> combat/puzzle restoration -> 75% boss gate.
- Combat minimums: elemental advantage, clash resolution, momentum bar, Tide Break.
- Restoration split baseline in current slice docs: combat contributes up to 50%, with remaining restoration via Tide puzzles/environmental balancing (tracked as 50/50 planning baseline; final balancing TBD in GDD notes).

## Umbrella Scope Gate Note (Issue #112)

This document is intentionally documentation-first for umbrella alignment. Code-level deferrals belong to their dedicated issue branches and should not be duplicated here.

Deferred scope tracking map (adjacent issue references under #112 umbrella for traceability only):

- In-battle party swap flow -> issue #106
- Cosmetic progression/style economy pathways -> issue #107
- Overworld roaming AI complexity deferral -> issue #108
- Map zoom and marker stack UX deferral -> issue #109
- Extended combat breadth beyond baseline loop deferral -> issue #110

## Milestone Deferral Matrix (#106-#110)

| Issue | Deferred Scope | Why Deferred At Milestone Level | Promotion Trigger (Required to Re-Scope) |
|---|---|---|---|
| #106 | In-battle party swap UX and related combat-time convenience flow | GDD states party switching occurs between battles; in-battle swap is not required for vertical-slice validation. | GDD update or milestone acceptance explicitly requires in-combat swapping behavior. |
| #107 | Cosmetic progression, style economy, and non-core customization progression loops | Cosmetic systems do not affect core restoration progression, boss gate validation, or baseline combat verification. | Product decision to include cosmetic progression in slice acceptance criteria. |
| #108 | Expanded overworld enemy roaming AI complexity | Reliable encounter access is sufficient for current slice; advanced AI behavior is polish scope. | Milestone criteria requires advanced roaming behaviors to prove encounter loop quality. |
| #109 | Expanded map zoom levels, marker stack UX, and map polish breadth | Core loop can be completed and verified without extended map UX systems. | Milestone criteria adds map-navigation UX quality bar beyond baseline playability. |
| #110 | Extended combat breadth beyond baseline loop (extra variants, depth expansions) | Current slice only needs elemental advantage, clash resolution, momentum bar, and Tide Break baseline. | Combat acceptance criteria expands beyond baseline mechanics documented in current GDD slice. |

Acceptance guardrails for this audit:

- Milestone test planning excludes deferred polish systems unless a dedicated issue is explicitly reclassified into acceptance scope.
- Deferred systems continue to be prioritized and scheduled in their dedicated issues; this umbrella doc records alignment and does not absorb implementation scope.
- Any scope promotion decision must update both this audit and `Assets/Issue112_GDD_Scope_Notes.md` in the same change.

Checklist references:

- Acceptance checklist source: `Assets/Issue112_GDD_Scope_Notes.md` (`Consolidated Acceptance Checklist (Current Slice)`).
- Regression checklist anchor: `Assets/Docs/Island1-Scope.md` (`Definition of Done` restoration and boss-gate checks).
