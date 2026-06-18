## What to build

Produce placeholder sprites for the 5 heroes and 6 island bosses so dev builds are visually legible.

Approach: extend the existing `FuturisticSpriteLibrary` procedural sprite pipeline to emit:
- One distinct hero sprite per element (fire, water, earth, air, space).
- One distinct boss sprite per vice island (lust, wrath, greed, sloth, pride, envy).

These are AFK placeholders meant to be replaced by real art later. The point is dev-build legibility.

## Acceptance criteria

- [ ] 5 hero sprites generated and assigned in HeroData assets.
- [ ] 6 boss sprites generated and assigned in EnemyData assets for each island boss.
- [ ] Sprites render correctly at combat scene resolution.
- [ ] No null sprite references in any HeroData or EnemyData asset.

## Blocked by

None - can start immediately.