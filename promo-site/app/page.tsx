const elements = [
  { name: "Fire", glyph: "F", className: "fire", beats: "Earth + Air" },
  { name: "Water", glyph: "W", className: "water", beats: "Fire + Space" },
  { name: "Earth", glyph: "E", className: "earth", beats: "Water + Space" },
  { name: "Air", glyph: "A", className: "air", beats: "Earth + Water" },
  { name: "Space", glyph: "S", className: "space", beats: "Fire + Air" },
];

const islands = [
  { number: "01", vice: "Greed", image: "A temple that demands restraint." },
  { number: "02", vice: "Gluttony", image: "Consumption turns the board against you." },
  { number: "03", vice: "Sloth", image: "Apathy slows every hard-won step." },
  { number: "04", vice: "Wrath", image: "Rage rewards aggression, then punishes it." },
  { number: "05", vice: "Envy", image: "The enemy reflects what makes you strong." },
  { number: "06", vice: "Pride", image: "Victory depends on the bonds you kept." },
];

const heroes = [
  { name: "Killian", element: "Fire", note: "Power shaped by restraint", className: "fire" },
  { name: "Merrick", element: "Water", note: "Healing with a personal cost", className: "water" },
  { name: "Freida", element: "Earth", note: "Control, protection, and patience", className: "earth" },
  { name: "Briar", element: "Air", note: "Grace, redirection, and resolve", className: "air" },
  { name: "The Fifth", element: "Space", note: "A path the player helps define", className: "space" },
];

export default function Home() {
  return (
    <main id="top">
      <a className="skip-link" href="#content">Skip to content</a>

      <nav className="site-nav" aria-label="Primary navigation">
        <a className="wordmark" href="#top" aria-label="TIDE home">
          <span>TIDE</span>
          <small>an RPG about balance</small>
        </a>
        <div className="nav-links">
          <a href="#tide">The Tide</a>
          <a href="#combat">Combat</a>
          <a href="#world">World</a>
          <a href="#development">Development</a>
        </div>
        <a className="nav-cta" href="#world">Chart the journey</a>
      </nav>

      <header className="hero" id="content">
        <img
          className="hero-art"
          src="/og.png"
          alt="Five elemental heroes overlook a central island and six corrupted islands divided by Light and Shadow Tide."
        />
        <div className="hero-shade" aria-hidden="true" />
        <div className="hero-copy">
          <p className="eyebrow">Turn-based fantasy RPG · In development</p>
          <h1>The world does not need a conqueror.</h1>
          <p className="hero-kicker">It needs balance.</p>
          <p className="hero-summary">
            Move Light and Shadow. Restore six corrupted islands. Build a party
            worth caring about—then face the fate waiting at the end of the tide.
          </p>
          <div className="hero-actions">
            <a className="button button-primary" href="#tide">Discover the Tide</a>
            <a className="button button-ghost" href="#chosen">Meet the Chosen</a>
          </div>
        </div>
        <div className="hero-facts" aria-label="Game details">
          <span>Unity 6</span>
          <span>Five elements</span>
          <span>Six corrupted islands</span>
          <span>One unavoidable fate</span>
        </div>
      </header>

      <section className="tide-section section-shell" id="tide">
        <div className="section-heading">
          <p className="eyebrow">The defining mechanic</p>
          <h2>Power is easy. Balance is the puzzle.</h2>
          <p>
            Every corrupted area sits somewhere between consuming Shadow and
            blinding Light. Tide is not a resource to hoard—it is a force to
            redistribute until the world rests at five.
          </p>
        </div>

        <div className="balance-layout">
          <div className="tide-meter" role="img" aria-label="Tide scale: 1 is excess Shadow, 5 is perfect balance, and 10 is excess Light">
            <div className="meter-label meter-shadow"><strong>1</strong><span>Excess Shadow</span></div>
            <div className="meter-track">
              <span className="track-mark" style={{ left: "0%" }}>1</span>
              <span className="track-mark balance-mark" style={{ left: "44.44%" }}>5</span>
              <span className="track-mark" style={{ left: "100%" }}>10</span>
            </div>
            <div className="meter-label meter-light"><strong>10</strong><span>Excess Light</span></div>
            <div className="balance-callout"><span>5</span>Perfect balance</div>
          </div>

          <ol className="loop-list">
            <li><span>01</span><div><strong>Explore</strong><p>Read the island, meet its people, and find the corruption.</p></div></li>
            <li><span>02</span><div><strong>Redistribute</strong><p>Take a legal bundle of Tide and move it across the grid.</p></div></li>
            <li><span>03</span><div><strong>Restore</strong><p>Combine combat and puzzles to reach the 75% boss threshold.</p></div></li>
            <li><span>04</span><div><strong>Confront</strong><p>Defeat the vice ruling the island and carry the consequences onward.</p></div></li>
          </ol>
        </div>

        <div className="pillar-grid" aria-label="TIDE design pillars">
          <article><span>01</span><h3>Emotional inevitability</h3><p>The story moves from adventure toward acceptance, not a last-minute escape from fate.</p></article>
          <article><span>02</span><h3>Balance over power</h3><p>The strongest move is not always the right move. Systems reward judgment and restraint.</p></article>
          <article><span>03</span><h3>Character attachment</h3><p>Five reluctant heroes become a party whose final choice only matters if their bond does.</p></article>
        </div>
      </section>

      <section className="combat-section" id="combat">
        <div className="section-shell combat-layout">
          <div className="combat-copy">
            <p className="eyebrow">Elemental momentum</p>
            <h2>Every action pulls the battle.</h2>
            <p>
              Choose actions for three active heroes, resolve clashes by Speed and
              elemental advantage, and shift the tug-of-war momentum bar. Push it
              fully to your side to unleash a character-specific Tide Break.
            </p>
            <div className="momentum-demo" aria-label="Momentum runs from enemy Tide Break through contested space to party Tide Break">
              <span>Enemy break</span><div><i /></div><span>Party break</span>
            </div>
          </div>
          <div className="element-wheel" aria-label="Five elemental affinities">
            {elements.map((element) => (
              <article className={`element-card ${element.className}`} key={element.name}>
                <span className="element-glyph" aria-hidden="true">{element.glyph}</span>
                <div><h3>{element.name}</h3><p>Strong against {element.beats}</p></div>
              </article>
            ))}
          </div>
        </div>
      </section>

      <section className="world-section section-shell" id="world">
        <div className="section-heading heading-row">
          <div><p className="eyebrow">One hub · six wounds</p><h2>An archipelago shaped by vice.</h2></div>
          <p>Each island changes the rules around a shared core: explore, fight, solve, restore, confront.</p>
        </div>
        <div className="island-grid">
          {islands.map((island) => (
            <article className="island-card" key={island.vice}>
              <span>{island.number}</span>
              <h3>{island.vice}</h3>
              <p>{island.image}</p>
              <i aria-hidden="true" />
            </article>
          ))}
        </div>
      </section>

      <section className="narrative-section">
        <div className="section-shell narrative-layout">
          <div className="narrative-statement">
            <p className="eyebrow">The truth beneath the adventure</p>
            <blockquote>“Victory brings relief. Balance brings an ending.”</blockquote>
            <p>
              Ancient texts reveal that the enemies return every century—and so do
              the Chosen. Saving the world means learning why they were born, what
              they are, and why their purpose cannot survive its own completion.
            </p>
          </div>
          <div className="act-list" aria-label="Three-act narrative progression">
            <article><span>Act I</span><h3>Reluctant hope</h3><p>Five strangers try to make the best of a destiny none of them wanted.</p></article>
            <article><span>Act II</span><h3>Unspoken dread</h3><p>The texts become clearer. The party understands before it is ready to speak.</p></article>
            <article><span>Act III</span><h3>Acceptance</h3><p>They stop asking how to escape and decide how they will face the end together.</p></article>
          </div>
        </div>
      </section>

      <section className="chosen-section section-shell" id="chosen">
        <div className="section-heading heading-row">
          <div><p className="eyebrow">The Chosen</p><h2>Five lives. Five elements. One century.</h2></div>
          <p>Only three fight at once, but every hero grows, every relationship matters, and no one is disposable.</p>
        </div>
        <div className="hero-grid">
          {heroes.map((hero) => (
            <article className={`chosen-card ${hero.className}`} key={hero.name}>
              <div className="chosen-sigil" aria-hidden="true">{hero.element.charAt(0)}</div>
              <p>{hero.element}</p><h3>{hero.name}</h3><span>{hero.note}</span>
            </article>
          ))}
        </div>
      </section>

      <section className="development-section" id="development">
        <div className="section-shell development-layout">
          <div>
            <p className="eyebrow">Currently in development</p>
            <h2>A playable foundation, becoming a world.</h2>
            <p>
              TIDE is a student-built Unity 6 project. Its core combat, momentum,
              Tide puzzle, restoration, progression, save, hub, and narrative
              frameworks are in place. The next tide is content: authored art,
              music, dialogue, environments, and sustained playtest tuning.
            </p>
          </div>
          <dl className="status-grid">
            <div><dt>Engine</dt><dd>Unity 6</dd></div>
            <div><dt>Perspective</dt><dd>Top-down</dd></div>
            <div><dt>Party</dt><dd>3 active · 2 reserve</dd></div>
            <div><dt>Boss gate</dt><dd>75% restored</dd></div>
          </dl>
        </div>
      </section>

      <footer className="site-footer">
        <div><span className="footer-mark">TIDE</span><p>Balance the world. Face the fate.</p></div>
        <p>Created by Game Club 2026 · SSC</p>
        <a href="#top">Back to the surface ↑</a>
      </footer>
    </main>
  );
}
