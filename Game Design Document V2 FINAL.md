# 📄 Game Design Document

# Game Design Document

## TIDE

# 0\. Design Pillars:

## Pillar 1: Emotional Inevitability

The story should build towards an acceptance of the unavoidable fate, rather than rebellion against it.

## Pillar 2: Balance over Power

The gameplay must reinforce the theme of balance over overwhelming power or domination.ge

## Pillar 3: Character Attachment

The character relationships are crucial to make the ending more impactful.

# 1\. Game Overview:

## Genre:

Turn-Based Fantasy RPG

## Platform:

TBD

## High Concept:

The game is set in a world of five elemental forces: Fire, Water, Earth, Air, and Space. Every century, five chosen individuals are born with the ability to sense and manipulate “Tide,” the fundamental balance of good and evil. These chosen heroes are destined to cleanse six corrupted islands ruled by embodiments of human vice. 

However, it is hidden from them that once their purpose is fulfilled, they will die. As the story unfolds, the tone shifts from a fantasy adventure to a philosophical exploration of fate, purpose, and the burden of knowing when you will die.

# 2\. Core Gameplay:

## Core Loop:

1. Explore an island  
2. Balance corrupted areas by balancing Tide  
3. Solve environmental Tide puzzles  
4. Restore 75% of the island  
5. Unlock and defeat the island’s boss  
6. Progress to the next island  
7. Defeating enemies encountered on the islands can restore up to 50% of an island’s corruption. The remaining balance must be restored through Tide-based puzzles and environmental interaction.

## Exploration:

Players travel between islands by boat. Each island is heavily corrupted due to imbalance in Tide.

Tide represents the balance between Light and Dark, similar to Yin and Yang. Each area has a visible Tide meter ranging from 1 to 10:

* 1 \= Excess Evil (completely black corruption)  
* 10 \= Excess Good (overwhelming white distortion)  
* 5 \= Perfect balance

Players can “take” Light or Shadow from one area and redistribute it to another to restore balance. When 75% of an island is balanced to 5, the island’s boss appears.

# 

# 2.5 Tide Puzzles:

## Overview:

Tide puzzles occur within corrupted subsections of an island. Each subsection is represented as a top-down grid of tiles.

Each tile contains a Tide value ranging from 1 to 10\.

The goal of a subsection is to balance tiles according to the subsection’s win condition (see Win Conditions section).

## Tile States:

Each tile has:

* A Tide value (integer 1–10)  
* A state:  
  * Normal  
  * Sealed (Infested)

## Sealed Tiles:

* Sealed tiles cannot be interacted with.  
* Tide cannot be taken from them.  
* Tide cannot be placed onto them.  
* You cannot pass through them when moving Tide.  
* Sealed tiles become Normal after defeating the enemies guarding them.

## 

## Taking Tide :

A player may take Tide from a tile according to the following rule:

* If tile value \> 5:  
  Player may take up to (tile value − 5).  
* If tile value \< 5:  
  Player may take down to 1  
* If tile value \= 5:  
  No Tide may be taken.

A player cannot take Tide that would cause a tile to cross past 5\.

Example:

Tile \= 8 → may take up to 3\.

Tile \= 2 → may take only 1

Tile \= 5 → cannot take.

The player may carry only one bundle of Tide at a time.

## Move Definition:

A single move consists of:

1. Selecting a source tile.  
2. Taking a legal amount of Tide.  
3. Traversing the grid while holding the Tide.  
4. Placing the Tide onto a destination tile.  
   1. A move is completed when Tide is placed.   
   2. Instability decay triggers only after placement.   
   3. Decay does NOT trigger during traversal.

## Traversal Rules:

* While holding Tide, the player may traverse up to 2 tiles out in any direction.  
* Diagonal movement is allowed.  
* Traversal may pass through Normal tiles only.  
* Traversal is blocked by Sealed tiles.  
* Traversal itself does not trigger decay.

## Placement Rules:

* Tide may be placed only onto a Normal tile.  
* Tiles cannot exceed a value of 10\.  
* Placement must obey tile capacity limits.  
* After placement, instability decay is evaluated and applied.

## Instability Decay:

* The island is naturally corrupted, meaning excess Light is unstable.  
* The party may sustain up to 3 (number TBD) tiles above 5 without instability.  
* If the number of tiles above 5 exceeds 3:  
  * Decay per move \= (Number of tiles above 5 − 3\)  
* Decay Rules:  
  * Decay applies to all tiles with value \> 5\.  
  * Each affected tile loses the decay amount.  
  * Tiles cannot decay below 5\.  
  * Decay occurs immediately after a move completes (after placement).  
* Examples:  
  * 4 tiles above 5 → decay \= 1 per move.  
  * 5 tiles above 5 → decay \= 2 per move.  
  * 6 tiles above 5 → decay \= 3 per move.  
* Decay never affects tiles at or below 5\.

## Subsection Win Conditions:

Win conditions may vary by island and progression stage.

Possible conditions include:

* Early Game:  
  * A percentage of tiles must reach 5\.  
* Late Game:  
  * All tiles must reach exactly 5\.

When a subsection is completed:

* It contributes to the island’s overall restoration percentage.

## 

## Island Restoration System:

Each island contains multiple corrupted subsections.

Island completion percentage is calculated by:

* Combat-cleared areas (up to 50%)  
* Puzzle-completed subsections

Example:

* Island has 5 subsections.  
* Player defeats all enemies (50%).  
* Player completes 2 subsections.  
* Island restoration \= 70%.

At 75% restoration, the island boss becomes available (Above percentages are TBD).

# 3\. Combat System:

Combat is turn-based and built around elemental advantage and momentum control.

## Element System:

Element System

There are five elemental affinities:

* Fire beats Earth and Air, loses to Water and Space  
* Water beats Fire and Space, loses to Earth and Air  
* Earth beats Water and Space, loses to Fire and Air  
* Air beats Earth and Water, loses to Fire and Space  
* Space beats Fire and Air, loses to Water and Earth

Each character has one elemental affinity. The main character chooses their affinity at the beginning of the game.

## 

## Party Structure:

* Five chosen heroes exist total  
* Three are active in battle at a time  
* Players can switch party members between battles

At the beginning of each turn, the player selects actions for all party members. Action order is then determined by Speed.

## Rock-Paper-Scissors Clash System:

If a player and an enemy target each other simultaneously, a clash occurs.

* Elemental advantage determines the winner.  
* The winner deals increased damage.  
* The winner still takes reduced damage.  
* If both elements are neutral, a Quick Time Event (QTE) may trigger.  
* Winning the QTE shifts advantage and momentum.  
* Failing shifts momentum toward the enemy.

## Tug-of-War Momentum System:

A unique tug-of-war bar appears at the top of the screen during battle.

* Landing effective elemental attacks shifts the bar toward the player.  
* Enemies can shift it back with advantageous attacks.  
* When fully shifted to one side, that side can unleash a Tide Break (TB).  
* There are many different types of TBs, depending on the party members and more TBs can be unlocked through gameplay.

## Character Stats:

* Speed  
* Defense  
* Attack  
* HP  
* MP  
* Crit Rate  
* Crit Damage

# 4\. Progression System:

## Experience & Leveling:

After battles:

* Active party members gain full XP  
* Off-party members gain reduced XP

Leveling up grants new skills and abilities tied to each character’s elemental affinity.

## Gear System:

Character builds have limited customization.

* Gear is obtained in full sets.  
* Characters must equip complete sets.  
* Sets affect both Attack and Defense.  
* Each set provides special percentage-based stat bonuses.

Armor sets gain XP through battles.  
When an armor set levels up:

* A new slot opens.  
* The slot is filled with a random percentage buff.  
* Each armor has up to 3 slots to unlock, meaning there will be up to a total of 4 percentage-based stat bonuses on a set of gear at once.

Players can pay smithies to duplicate gear sets once they obtain ideal stat rolls. This allows optimization without re-grinding.

# 

# 5\. World and Narrative:

## Setting:

The game takes place on one central island surrounded by six corrupted islands. Each island is ruled by a manifestation of one of the six enemies of Hindu philosophy (Arishadvarga):

* Lust  
* Anger  
* Greed  
* Desire  
* Ego  
* Envy

Each enemy has full control over Tide, corrupting their island completely.

## The Chosen:

Every century, five individuals are chosen at age 15 during a coming-of-age ceremony that reveals their connection to Tide.

These individuals:

* Are drawn randomly from normal lives.  
* Initially struggle with each other.  
* Gradually bond over shared fate and responsibility.  
* Each embody one elemental force.

Ancient texts written by past heroes reveal that the enemies return every 100 years, just like the chosen heroes. Victory only brings temporary relief.

## Thematic Direction:

The narrative evolves from heroic fantasy into a meditation on fate.

Core themes:

* Being born for a single purpose  
* The loss of individuality  
* Knowing the exact moment of your death  
* The cycle of temporary salvation

# 5.5 Narrative Structure Blueprint:

## Act I (Island 0, 1, and 2):

Tone: tense, rough, adventurous, optimistic

* The chosen 5 first meet  
* There is a rough party dynamic because none of them want this  
* They opt to just try their best  
* After an island or two, they discover some ancient texts.  
* The characters and players do not know of their fate yet.  
* The party dynamic gradually improves.

## Act II (Island 3, 4, and 5):

Tone: heavier, unsettling, melancholic

* Through more texts, they discover more about their true origins.  
* The ancient texts lead them through the same discoveries that the past generation had.  
* The player most likely will have made the full realization at this point.  
* The characters are mildly aware, but they do not talk about it.  
* The party dynamic gets worse again.

## Act III (Final Island):

Tone: acceptance, acknowledgement.

* On the final island, before the boss fight.  
* The characters talk about it briefly.  
* They have already accepted it at this point, and face the final boss.

The characters gradually understand more about their fate as they discover more ancient texts.

# 

# 6\. Endings: 

## Good / Bittersweet Ending:

The party defeats the six enemies.

However, it is revealed that the chosen heroes are not truly human. They are manifestations of Light and Dark, brought into existence only to maintain balance.

With the enemies gone, Darkness disappears.  
Without Darkness, there is no need for Light.

The natural balance strips the heroes of their Tide powers, and they slowly fade away. The party dies together on a hill facing the sunset, accepting their fate and finding peace in having fulfilled their purpose.

## Bad Ending:

Triggered if:

* The final boss defeats the player more than three times  
  OR  
* The player clears only the minimum 75% of corruption before proceeding on each island.

In this ending:  
The party is defeated. Everyone dies except the main character.

Broken by despair, he concludes that death was inevitable and meaningless. On a hill facing the sunset, he stabs himself and dies without completing his purpose.

# 7\. Art Direction:

* 2D presentation, top down POV, similar to Pokemon  
* Pixel art or 3D models  
* Visual corruption represented through color shifts:  
  * Complete black for excess evil  
  * Blinding white for excess good

# 📚 Notes:

- Potential enemies:  
  - Kelpies  
  - Wendigo  
  - Enchanted moura  
  - jorogumo yokai  
  - Ouroboros  
  - Ravana  
  - Red boy  
- Potential final boss as fate?  
- General monsters appear on each island.  
- Island specific monsters representing each sin appear on specific islands.  
- Loosely take inspiration from actual cultures and add it to in-game culture(s)?

Roles:

- Ryan N \- Lead  
- Cho \- Music  
- Enzo \- Music  
- Ryan P \- Programming / Art  
- Hannah \- World building / Character writing  
- Tilly \- World building / Character writing / Art  
- Tully \- Writing / World building / Art  
- Andrian \- Programming  
- Clinton \- Programming

UI Ideas:

- Heavily based off of / inspired by Persona 5 and Persona 3 Reload  
- The main color scheme for UI will be blue, to match the theme of somberness a bit more, or we could do green to signal the characters “returning to nature” in the good ending. Both colors can represent sadness / melancholy so we can discuss.

Characters (concept):  
Earth:

Name: Freida

Age: 15 (almost 16\)

Gender: Female

Personality: Tranquil, chill, zen, understanding, suckup (bad ending)

worried about losing all her friends so she is greedy by taking up their attention all the time (bad ending)

looks: kinda lanky, ginger, curly shoulder length hair, tall, freckles, pale skin, green eyes

abilities: nature control (obvi), can grow plants, IM COUNTING ROCKS AS EARTH, can stun and push back enemies, main weapon is bow and arrow

Air:

Name: Briar

Age: 15

Gender: Female

Personality: creative, problem solver, sweet, relatable, cool, elegant, insecure (bad ending)

Jealous of the other's powers, and the others underappreciating her (they could believe the fact she has to dance to create wind and stuff a bit weird/childish?), she starts not helping them in battle because of jealousy. (bad ending)

looks: short, a bit chubby, brown skin, black/dark brown hair, nose ring, straight hair, dark brown eyes

abilities: make strong winds around teammates to redirect attacks from enemies away from teammate (only one teammate at a time), can push enemy away, can use strong wind (mentioned before) on enemy to force them not to use ability, does abilities by dancing, main weapon is fans.

Fire:

Name: Killian

Age: 15 (almost 16 but younger than Freida)

Gender: Male

Personality: Quiet, anger issues, internalised feelings, mature, sensitive, extremely, chill, over-emotional (bad ending)

realises that they will die no matter what and takes it out on the environment, which makes Freida upset and Merrick tries to calm him down, starts a bit of a rivalry (bad ending)

looks: tall, a little bit chubby, tan skin, short pushed back brown hair, slight sideburns, thick eyebrows, hazel eyes

abilities: able to use rage for damage boost (exists for a certain amount of turns and has a cooldown), he does most damage (aether close second), fire bolt, could use melee weapons on fire? main weapon is dual chakram (ring sword)

Water:

Name: Merrick

Age: 15 (freshly 15\)

Gender: Male

Personality: Outgoing, extroverted, trustworthy, loyal, willing to sacrifice (bad ending)

One of his abilities is pain absorption, if another team member is downed, he can absorb the pain and down himself. He is attached to the attention he gains from trading pain, and eventually can’t handle it,(not realising he takes mental states too) leading to his death. (bad ending)

Looks: Medium height, lanky, wavy blonde hair, blue eyes, moles, pierced ears

abilites: able to revive by taking teammates pain and transferring it to himself (it looks like percy jackson's water absorption, search it up its from the movies), ability to heal others by spraying water at them (this will lower Killian's damage if he is healed a lot), lower HP but higher healing output and self heal ability. Possible ability with air where they are able to create water tornado to stun enemies and do damage. The main weapon is staff.

Aether: very cool enchanted purple type shi (magic whoahhh)

Feel free to comment if something is contradicting another or if you have a better idea :3  
I also have concept art if anyone would like a visual  
![][image1]![][image2]![][image3]![][image4]

PLOT (can be changed): 

| Chapter/part | Events/important moments |
| :---- | :---- |
| Chapter Zero/One (Tutorial/introduction) ACT 1 | Introduced to the world, traditions, etc Giving background information NOT told from MC POV, told from future storyteller? Introduction to coming of age ceremony which transitions to characters being chosen Then goes to training (tutorial) |

| Chapter Two ACT 1 | Meeting other characters Getting to know each other Travelling to get gear \+ XP  Left first island Discovery of ancient texts hinting at fate Travel to first corrupted island Greed, final boss located in a temple full of gold and coins, group cannot take any gold, one takes some and wakes the boss |
| :---- | :---- |

| Chapter Three ACT 1 | Leaving the previous island Characters grow closer together Discover more ancient text Slowly uncovering the truth Team starts to get stressed (players actions decide whether or not the others calm down) Attachment final boss, located in a garden, shows items that characters were previously attached to (deceased family, broken items, trauma, etc) to distract them Team ups and skills will not work as well as normal during this |
| :---- | :---- |

| Chapter Four ACT 2 | Group travels to the next island Discover more texts, they start believing  The group starts growing more distant depending on players choices. Group is mildly aware of situation but avoid talking about it Party dynamic starts to get tense Jealousy final boss, boss is able to enter the heads of characters, telling them to be jealous and envious of each other, located on the beach |
| :---- | :---- |

| Chapter Five ACT 2 | Travel back to previous islands to see if there’s any more ancient text Ask tribe leaders about the text, they refuse to be truthful Team goes to next island Team resorts to asking enchanted moura (unaware of the fact they are evil) which leads to lust boss (mini boss mostly for gear, goods, info or easter eggs) Player would have made full discovery of the teams fate by now Final boss is anger, boss makes characters take the new knowledge poorly and take out anger on boss or each other depending on relationship levels |
| :---- | :---- |

| Chapter Six ACT 3 | Group understands their fate Second last island How they react depends on the player’s choices Low relationship levels \=  poor team dynamic, team ups and skills dont work as well High relationship levels \= good team dynamic, team ups and skills work the same or possibly better Pride/Ego boss, convincing characters that they are better than each other. Located on mountain. Group realises that every boss has tried to drive them against each other, group now knows if they work together, it can defeat the last boss. |
| :---- | :---- |

| Chapter Seven ACT 3 FINALE | Group travels to final island Fate as final boss Instead of fighting the whole group, fate enters MC’s mind, asking questions like ‘Have you come to peace with your fate?’ etc, if you say yes, you dont fight the boss, and this could cause the bad ending. If you say no and you could defeat the boss Hardest boss of them all At the end of the story, whether you win or not, the screen closes like a book. And the narrator finishes the story he’s telling to children. |
| :---- | :---- |

# 🙋 Q\&A

**Organisation and Responsibility**

**What are the team roles? Who will take on each role?**

The game design document doesn't list the exact team roles or who will do what yet. We still need to figure out who's the main coder, the artist, the writer/narrative lead, and the one who handles the puzzle design.

---

**Inspiration and points of originality**

**What has inspired your game? Are there any cartoons, books, movies or games that inspire you? What are they?**

The main inspiration comes from a big philosophical idea: accepting unavoidable fate, and the game is built around that. Also, the enemies we fight are based on the six enemies of Hindu philosophy, which are things like Lust, Anger, and Greed.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**How do you plan to use this inspiration in your game?**

The whole game is a "philosophical exploration of fate, purpose, and the burden of knowing when you will die". The heroes are destined to die once they save the world, so the story is about them coming to terms with that sad ending (Pillar 1: Emotional Inevitability).[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**Are there other games that have similar gameplay mechanics? Similar functionality? Similar stories or characters?**

Yes, our game is a **Turn-Based Fantasy RPG**, so it's similar to games like *Final Fantasy* or *Pokemon* in terms of combat style and progression.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**How will your game be different? Why would people prefer to play your game over these other games?**

Our game is different because it focuses on **Balance over Power** (Design Pillar 2). Most RPGs are about getting stronger and dominating, but in our game, the core mechanic—the Tide system—is about balancing the forces of Light and Dark to exactly 5\. Players will prefer this because it's not just a hack-and-slash game; you have to solve clever puzzles to cleanse the islands.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**Is your game different enough to be worth making? Why/why not? Provide references.**

Yes, totally\! The combination of the unique Tide-balancing puzzle mechanics and the deep, emotional story about fate and death makes it original and definitely worth making.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

---

**Game overview**

**Game title**

The game title is **TIDE**.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**Why did you choose this name?**

We chose this name because the whole game revolves around "Tide," which is what we call the fundamental balance of good and evil. It’s the central idea.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**Does this name help players to know what the game might be about? Explain.**

Yes, because the word "tide" makes you think of a huge, natural, moving force, and that's exactly what the players are trying to control—the world's basic balance of Light and Dark.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

---

**Game description**

**What is your game about?**

Our game is about five chosen heroes born with the ability to sense and manipulate the elemental forces and the "Tide". Their job is to cleanse six corrupted islands ruled by evil.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**What will players do in the game?**

Players will:

* Travel between islands by boat.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)  
* Explore corrupted islands.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)  
* Solve environmental Tide puzzles to restore balance.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)  
* Fight turn-based battles against enemies.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**What is the objective of your game?**

The main objective is to restore at least 75% of the balance on all six corrupted islands, leading to the boss fight on each one. The deeper objective is for the characters to accept their inevitable fate of dying once their purpose is fulfilled.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**Who is your intended audience?**

Our audience is people who like Turn-Based Fantasy RPGs and who enjoy stories with strong character relationships and deep, philosophical themes about purpose.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**What makes your game fun or interesting?**

The unique gameplay is what makes it fun:

* The Tide puzzles force you to move Light and Shadow values on a grid to reach perfect balance.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)  
* The combat system uses a cool **Tug-of-War Momentum System** where landing elemental attacks shifts a bar, letting you unleash powerful "Tide Breaks".[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)  
* The story gets really emotional as the party bonds and then faces their known death.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

---

**Characters**

**Does your game have characters or objects?**

Yes, there are the five main playable heroes, known as "The Chosen," and six main enemies (bosses).[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**What role do the characters or objects play in the story?**

* **The Chosen (Heroes):** They are drawn randomly from normal life and embody one of the five elemental forces (Fire, Water, Earth, Air, Space). They are the manifestations of Light and Dark brought into existence to maintain balance.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)  
* **The Enemies (Objects of Vice):** They are the physical manifestations of human vices: Lust, Anger, Greed, Desire, Ego, and Envy. They rule the corrupted islands and fully control the Tide.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**What is the motivation for these characters or objects within the game?**

The heroes are motivated by their shared responsibility and their destiny to fight the enemies. Even though they initially struggle and don't want the job, they gradually bond over their fate and face the final boss with acceptance.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

---

**Environment**

**Where does the game take place?**

The game takes place on one central island and six corrupted islands.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**Under what conditions does your game take place?**

The game happens in a world of five elemental forces, where the islands are heavily corrupted due to an imbalance in the world's fundamental good/evil force, the Tide.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**Do these conditions have any effect on the gameplay that you might need to consider?**

Absolutely\! The corruption is the whole point of the gameplay loop. The players have to manage the Tide meter (which shows the balance of Light and Dark) for every area they enter, and they must restore 75% of the island before they can even fight the boss.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

---

**Theme (Ocean)**

**How is the theme incorporated in your game?**

The theme is incorporated through the setting and the core mechanic:

* **Setting:** The world is made up of islands, and players travel between them using a boat.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)  
* **Core Mechanic:** The game's balance system is named **TIDE**, which is a powerful ocean phenomenon.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**What parts of your game relate to the theme and why? Provide examples.**

The title "TIDE" relates to the natural, shifting power that the heroes must master, just like the flow of the ocean. The act of moving Tide (Light and Shadow) between different areas during puzzles is like shifting the balance of the ocean itself.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

---

**Gameplay/mechanicsObjectives/Goals**

**What is the aim of the game?**

The aim is to restore the balance of Tide on the six corrupted islands.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**Can a player win the game? How? What is the player trying to achieve?**

Yes, a player can win by defeating all six enemies/bosses. This achieves the heroes' purpose of maintaining balance in the world.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**Can a player finish the game? How?**

Yes, finishing the game means reaching one of the two endings:

* **Good/Bittersweet Ending:** The party defeats the enemies, fulfills their destiny, and slowly fades away with peace.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)  
* **Bad Ending:** The player is defeated by the final boss more than three times, or they only cleared the minimum 75% of corruption on every island. The main character dies alone in despair.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**How does the player progress through the game?**

Progress happens by exploring an island, clearing corrupted areas through combat (up to 50% restoration), and solving Tide puzzles to restore the remaining balance. Once 75% is restored, the boss unlocks, and defeating the boss lets the player move to the next island.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**Are there multiple levels or does the game get more difficult or introduce new goals over time?**

There are six main islands, and the game gets more difficult. The puzzles change from simple goals (a percentage of tiles must be balanced) to much harder goals (all tiles must be **exactly** 5). The story also gets heavier as the game progresses through three Acts where the characters learn their true, tragic fate.**Perspective**[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**What is the players’ perspective when playing the game? Do they experience the game from a first-person point of view? From the side (like a platformer)? From a top-down perspective?**

The player's perspective is **top-down POV (point of view)**.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**Is it a two-dimensional (2D) or three-dimensional (3D) game?**

It will be a **2D presentation** using pixel art, or maybe 3D models with a top-down camera.**Controls**[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**How do players actually play or interact with the game?**

The game involves moving around, fighting in turn-based combat, and interacting with the grid-based Tide puzzles. In the puzzles, players select a tile, take an allowed amount of Tide (like taking 3 if the value is 8), move across the grid (up to 2 tiles, including diagonals) while holding the Tide, and then place it on a new tile.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**What are the controls? — How do they work?**

The specific buttons and keys (like WASD or controller buttons) haven't been decided yet in the plan.**Instructions/Tutorials**

**What are gameplay mechanics?**

Gameplay mechanics are the rules and procedures that guide players through the game and show how the game reacts to their actions. For example, our element system (Fire beats Earth) is a mechanic.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

**What features did you include to help the player learn to play the game?**

The plan doesn't mention specific tutorial features like pop-up text boxes. However, the narrative itself acts as a tutorial: in Act I and Act II, the characters find ancient texts written by past heroes, which helps the party (and the player) slowly understand the world's deep rules and their history.[1](https://docs.google.com/document/d/12l--0itcV7szzwbQKj-_OC7onK25H5t9OFOXjvRiue8/edit)

[image1]: <data:image/png;base64,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>

[image2]: <data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAJcAAADSCAYAAABHClKmAABGlUlEQVR4Xu3d959UVbY28P6b3pm5d+bOONEwKpgTmMWsmBAUFSSJAVHBhDlgBHVQUEBoxQQiOuY8Zh3TzDjJO+G99z3v+e5yt8XuqupTVaeqq6V+WB+0K51z9tprPetZYQ9k//4qqyVPrn0gu/7i878zsnjenGzihMOzH/9y9+y0E0/Krrlo/rD31JPL5s7KTj1+cvbrcftn3/vJrtnPd9wrO2rSsdkls88d9t6+fCsDqVJF+f17r36nZN2aVdmhR07ODjvyxGz92lXDXo/yybuvZM9u2pDdf9892bK77wwya8752Q7jJmT/58e7ZNvvekC2YMEl2dNPPpK9/tLmIXnz5S3Zu288n330zsvDvnNblbrK9V2TDRvWZxMOOz4785y52e/efHnY61H++qdPssuvvDr7+c77BWVKZWL+HQsuWZxdc+0NQ3LtdTdmN958a3b3smXZQw89mD3yyLrsiccezTY//WS25ZmN2bPfyPPPbc5ee+W32QfvvpZ9+el72T/++nn2//71p2HX8F2RnlYuD/6ff/si+/OXH2afffK77KP33wiL38qCUK4DDz9hROX64+fvZxdfujj7+a/3zX6Uu9Bf735gtucBR2bj9z40/O0HPx0/TOFS+c+f7xaUc9zeh2V7Tzgy22fiUUEOOOTY7NjJU7MZs+Znl1+1JLvn3nvy6xrMXn/1+exvX30y7FrGuvSMclGYr//8aVCgF5/fkj315GPZ+vVrs3vvvTe7/oabwoLPnb8gWIX//utnwz4/krzw283Z0SdMyU487czs5Re3DHu9Wl56YUu2OLde5124MLvl1qW5NVoVFGHRFVdnZ587L5t21uxhctq0mdnk06Znx+TKc8iRJ2X7HXxMUKw995+UK9mh2U67Tcx+kSscxatWREp43MlnhO//7JN3sv9tYeP0qoy6cn39l0+Dm9i86Ynsrrvvzuacd1FYnB3GTwgLAUBvt8NeYYEOOuLE7M677s6+yi1Z+j3V8r///GP2+e/fzX7/0dvZn774IPv7V78PSksJDs5xF3eVfqZd+ffXX4Tf+vC913NFfiZ79NH12cMPr85WPbgyW7ZseXbzLbfm1urqbGaO36IyHnn8acEysobud3H++gf5511/+v1jUUZFuTy8L3LM8fxz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>

[image3]: <data:image/png;base64,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>

[image4]: <data:image/png;base64,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>