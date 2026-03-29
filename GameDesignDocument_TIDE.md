# Game Design Document - TIDE

## 0. Design Pillars

### Pillar 1: Emotional Inevitability
The story should build toward acceptance of unavoidable fate, rather than rebellion against it.

### Pillar 2: Balance over Power
The gameplay must reinforce the theme of balance over overwhelming power or domination.

### Pillar 3: Character Attachment
Character relationships are crucial to make the ending more impactful.

## 1. Game Overview

**Genre:** Turn-Based Fantasy RPG  
**Platform:** TBD

### High Concept
The game is set in a world of five elemental forces: Fire, Water, Earth, Air, and Space. Every century, five chosen individuals are born with the ability to sense and manipulate "Tide," the fundamental balance of good and evil. These chosen heroes are destined to cleanse six corrupted islands ruled by embodiments of human vice.

However, it is hidden from them that once their purpose is fulfilled, they will die. As the story unfolds, the tone shifts from a fantasy adventure to a philosophical exploration of fate, purpose, and the burden of knowing when you will die.

## 2. Core Gameplay

### Core Loop
- Explore an island
- Balance corrupted areas by balancing Tide
- Solve environmental Tide puzzles
- Restore 75% of the island
- Unlock and defeat the island's boss
- Progress to the next island

Defeating enemies encountered on islands can restore up to 50% of an island's corruption. The remaining balance must be restored through Tide-based puzzles and environmental interaction.

### Exploration
Players travel between islands by boat. Each island is heavily corrupted due to imbalance in Tide.

Tide represents the balance between Light and Dark, similar to Yin and Yang. Each area has a visible Tide meter ranging from 1 to 10:

- 1 = Excess Evil (completely black corruption)
- 10 = Excess Good (overwhelming white distortion)
- 5 = Perfect balance

Players can take Light or Shadow from one area and redistribute it to another to restore balance. When 75% of an island is balanced to 5, the island's boss appears.

## 2.5 Tide Puzzles

### Overview
Tide puzzles occur within corrupted subsections of an island. Each subsection is represented as a top-down grid of tiles.

Each tile contains a Tide value ranging from 1 to 10.

The goal of a subsection is to balance tiles according to the subsection's win condition (see Win Conditions section).

### Tile States
Each tile has:

- A Tide value (integer 1-10)
- A state:
  - Normal
  - Sealed (Infested)

### Sealed Tiles
- Sealed tiles cannot be interacted with.
- Tide cannot be taken from them.
- Tide cannot be placed onto them.
- You cannot pass through them when moving Tide.
- Sealed tiles become Normal after defeating enemies guarding them.

### Taking Tide
A player may take Tide from a tile according to the following rule:

- If tile value > 5: player may take up to (tile value - 5).
- If tile value < 5: player may take down to 1.
- If tile value = 5: no Tide may be taken.

A player cannot take Tide that would cause a tile to cross past 5.

Examples:

- Tile = 8 -> may take up to 3
- Tile = 2 -> may take only 1
- Tile = 5 -> cannot take

The player may carry only one bundle of Tide at a time.

### Move Definition
A single move consists of:

1. Selecting a source tile
2. Taking a legal amount of Tide
3. Traversing the grid while holding the Tide
4. Placing the Tide onto a destination tile

A move is completed when Tide is placed. Instability decay triggers only after placement. Decay does not trigger during traversal.

### Traversal Rules
- While holding Tide, the player may traverse up to 2 tiles in any direction.
- Diagonal movement is allowed.
- Traversal may pass through Normal tiles only.
- Traversal is blocked by Sealed tiles.
- Traversal itself does not trigger decay.

### Placement Rules
- Tide may be placed only onto a Normal tile.
- Tiles cannot exceed a value of 10.
- Placement must obey tile capacity limits.
- After placement, instability decay is evaluated and applied.

### Instability Decay
The island is naturally corrupted, meaning excess Light is unstable.

The party may sustain up to 3 (number TBD) tiles above 5 without instability.

If the number of tiles above 5 exceeds 3:

- Decay per move = (number of tiles above 5 - 3)

Decay Rules:

- Decay applies to all tiles with value > 5.
- Each affected tile loses the decay amount.
- Tiles cannot decay below 5.
- Decay occurs immediately after a move completes (after placement).

Examples:

- 4 tiles above 5 -> decay = 1 per move
- 5 tiles above 5 -> decay = 2 per move
- 6 tiles above 5 -> decay = 3 per move

Decay never affects tiles at or below 5.

### Subsection Win Conditions
Win conditions may vary by island and progression stage.

Possible conditions include:

- Early game: a percentage of tiles must reach 5.
- Late game: all tiles must reach exactly 5.

When a subsection is completed, it contributes to the island's overall restoration percentage.

### Island Restoration System
Each island contains multiple corrupted subsections.

Island completion percentage is calculated by:

- Combat-cleared areas (up to 50%)
- Puzzle-completed subsections

Example:

- Island has 5 subsections
- Player defeats all enemies (50%)
- Player completes 2 subsections
- Island restoration = 70%

At 75% restoration, the island boss becomes available (percentages above are TBD).

## 3. Combat System
Combat is turn-based and built around elemental advantage and momentum control.

### Element System
There are five elemental affinities:

- Fire beats Earth and Air, loses to Water and Space
- Water beats Fire and Space, loses to Earth and Air
- Earth beats Water and Space, loses to Fire and Air
- Air beats Earth and Water, loses to Fire and Space
- Space beats Fire and Air, loses to Water and Earth

Each character has one elemental affinity. The main character chooses their affinity at the beginning of the game.

### Party Structure
- Five chosen heroes exist total
- Three are active in battle at a time
- Players can switch party members between battles

At the beginning of each turn, the player selects actions for all party members. Action order is then determined by Speed.

### Rock-Paper-Scissors Clash System
If a player and an enemy target each other simultaneously, a clash occurs.

- Elemental advantage determines the winner.
- The winner deals increased damage.
- The winner still takes reduced damage.
- If both elements are neutral, a Quick Time Event (QTE) may trigger.
- Winning the QTE shifts advantage and momentum.
- Failing shifts momentum toward the enemy.

### Tug-of-War Momentum System
A unique tug-of-war bar appears at the top of the screen during battle.

- Landing effective elemental attacks shifts the bar toward the player.
- Enemies can shift it back with advantageous attacks.
- When fully shifted to one side, that side can unleash a Tide Break (TB).
- There are many types of TBs depending on party members, and more TBs can be unlocked through gameplay.

### Character Stats
- Speed
- Defense
- Attack
- HP
- MP
- Crit Rate
- Crit Damage

## 4. Progression System

### Experience and Leveling
After battles:

- Active party members gain full XP
- Off-party members gain reduced XP

Leveling up grants new skills and abilities tied to each character's elemental affinity.

### Gear System
Character builds have limited customization.

- Gear is obtained in full sets.
- Characters must equip complete sets.
- Sets affect both Attack and Defense.
- Each set provides special percentage-based stat bonuses.

Armor sets gain XP through battles.

When an armor set levels up:

- A new slot opens.
- The slot is filled with a random percentage buff.
- Each armor has up to 3 slots to unlock, meaning up to 4 percentage-based stat bonuses on one set of gear at once.

Players can pay smithies to duplicate gear sets once they obtain ideal stat rolls. This allows optimization without re-grinding.

## 5. World and Narrative

### Setting
The game takes place on one central island surrounded by six corrupted islands. Each island is ruled by a manifestation of one of the six enemies of Hindu philosophy (Arishadvarga):

- Lust
- Anger
- Greed
- Desire
- Ego
- Envy

Each enemy has full control over Tide, corrupting their island completely.

### The Chosen
Every century, five individuals are chosen at age 15 during a coming-of-age ceremony that reveals their connection to Tide.

These individuals:

- Are drawn randomly from normal lives.
- Initially struggle with each other.
- Gradually bond over shared fate and responsibility.
- Each embody one elemental force.

Ancient texts written by past heroes reveal that the enemies return every 100 years, just like the chosen heroes. Victory only brings temporary relief.

### Thematic Direction
The narrative evolves from heroic fantasy into a meditation on fate.

Core themes:

- Being born for a single purpose
- The loss of individuality
- Knowing the exact moment of your death
- The cycle of temporary salvation

## 5.5 Narrative Structure Blueprint

### Act I (Island 0, 1, and 2)
Tone: tense, rough, adventurous, optimistic

- The chosen five first meet.
- There is a rough party dynamic because none of them want this.
- They opt to just try their best.
- After an island or two, they discover ancient texts.
- Characters and players do not know their fate yet.
- The party dynamic gradually improves.

### Act II (Island 3, 4, and 5)
Tone: heavier, unsettling, melancholic

- Through more texts, they discover more about their true origins.
- The ancient texts lead them through the same discoveries that the past generation had.
- The player will most likely have full realization at this point.
- The characters are mildly aware, but they do not talk about it.
- The party dynamic gets worse again.

### Act III (Final Island)
Tone: acceptance, acknowledgement

- On the final island, before the boss fight, the characters talk about it briefly.
- They have already accepted it and face the final boss.
- The characters gradually understand more about their fate as they discover more ancient texts.

## 6. Endings

### Good / Bittersweet Ending
The party defeats the six enemies.

However, it is revealed that the chosen heroes are not truly human. They are manifestations of Light and Dark, brought into existence only to maintain balance.

With the enemies gone, Darkness disappears. Without Darkness, there is no need for Light.

The natural balance strips the heroes of their Tide powers, and they slowly fade away. The party dies together on a hill facing the sunset, accepting their fate and finding peace in having fulfilled their purpose.

### Bad Ending
Triggered if:

- The final boss defeats the player more than three times
- OR the player clears only the minimum 75% of corruption before proceeding on each island

In this ending:

The party is defeated. Everyone dies except the main character.

Broken by despair, he concludes that death was inevitable and meaningless. On a hill facing the sunset, he stabs himself and dies without completing his purpose.

## 7. Art Direction
- 2D presentation, top-down POV, similar to Pokemon
- Pixel art or 3D models
- Visual corruption represented through color shifts:
  - Complete black for excess evil
  - Blinding white for excess good
