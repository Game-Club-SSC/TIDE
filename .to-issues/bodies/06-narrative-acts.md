## What to build

Wire the existing AncientTextData assets to the GDD's three-act structure (section 5.5).

Behavior:
- Act I (islands 0-2): Adventurous ancient texts surface as the player explores.
- Act II (islands 3-5): Heavier, unsettling texts surface as players discover more about their origins.
- Act III (final island): Acceptance-focused texts surface.

Use `IslandProgressionManager.ActiveIslandId` to gate which set of texts is discoverable.

## Acceptance criteria

- [ ] AncientTextDiscoverable components exist for at least one Act I text on island_lust, one Act II text on island_greed, one Act III text on the final island.
- [ ] Discoverable texts only show on islands matching their act.
- [ ] AncientTextLogUI lists discovered texts in chronological order.
- [ ] AncientTextLogUITest extended to cover act-gating.

## Blocked by

- Slice 03 (good-ending cutscene) should land first so the final-act narrative beats align with the ending flow.