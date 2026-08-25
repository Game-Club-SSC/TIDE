# TIDE Website

This folder contains the responsive front-facing website for **TIDE**.

## Open the site locally

From the repository root:

```bash
python3 -m http.server 8000 --directory website
```

Then open `http://localhost:8000`.

The implementation is a dependency-free static page, so it does not require an npm install or build step.

## Design sources

- **Figma design:** https://www.figma.com/design/uriODSVhX9n75FpqcOGEaZ
- **Game Design Document:** `../Game Design Document V2 FINAL.md`
- **Mintlify game guide:** https://ssc-296923d5.mintlify.app

## What the page covers

The website presents the main ideas from the current game design document:

- the 1–10 Tide scale and balance at 5;
- the explore → rebalance → unseal → restore → confront → sail-on loop;
- Fire, Water, Earth, Air, and Space matchups;
- turn-based combat, clashes, momentum, and Tide Breaks;
- the story pillars of emotional inevitability, balance over power, and character attachment;
- six-island restoration progression.

## Responsive QA

The page was checked at:

- 1440 × 900 desktop;
- 390 × 844 mobile.

The final implementation had no horizontal overflow at either viewport.

## Related development docs

The Mintlify documentation branch also adds pages covering:

- the website and Figma design system;
- the AGY automated bug-review workflow;
- the Games Club / Year 9 documentation checklist.
