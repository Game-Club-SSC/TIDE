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

Treat the above systems as defer-candidates unless explicitly required for current milestone acceptance.
