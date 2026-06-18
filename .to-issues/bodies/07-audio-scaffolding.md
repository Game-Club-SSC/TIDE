## What to build

Add an `AudioManager` singleton and hook it into the existing game systems.

Hooks:
- CombatScene loaded: play combat BGM loop (placeholder).
- Combat victory: play victory sting.
- Combat defeat: play defeat sting.
- Puzzle solved: play solve chime.
- Boss intro: play boss stinger.
- Ending cutscene start: play ending music.
- Exploration scene loaded: play exploration BGM loop (placeholder).

Placeholder audio clips are acceptable for this slice. The hooks and routing are the deliverable.

## Acceptance criteria

- [ ] AudioManager singleton exists with DontDestroyOnLoad.
- [ ] BGM transitions on scene load.
- [ ] Stingers fire on combat start/end, puzzle solve, boss intro, ending start.
- [ ] All hooks gracefully no-op if the referenced AudioClip is null (placeholder build safe).
- [ ] Mute/volume controls accessible via AudioManager API.

## Blocked by

None - can start immediately.