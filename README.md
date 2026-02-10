# Barfight

**CSCI 426 - Game Prototyping**

## Prompt

> The game must be played using **ONE BUTTON** (use the spacebar), and it must have an end/win condition.

## About

This prototype is a **one-button Boss Rush** game inspired by the fishing minigame in Stardew Valley. The player fights a gauntlet of three bosses using only the spacebar -- holding it moves a player indicator upward, and releasing lets it fall. When the indicator overlaps the boss icon, the boss's health fills; when it doesn't, the health drains back down. Fill the bar completely to defeat each boss.

### How One Button Drives the Design

The spacebar always does the same physical thing (raise/lower the player indicator), but the *context* changes everything:

- **Boss 1 (Wolf)** -- A straightforward introduction. The boss indicator drifts at a steady pace, teaching the core hold/release rhythm.
- **Boss 2 (Clock)** -- The boss moves more aggressively, demanding quicker reactions with the same single input.
- **Boss 3 (Dragon)** -- Midway through the fight the dragon transforms into a more powerful Phase 2 form: the player's indicator bar shrinks, the boss speeds up, and the screen shakes -- all while the player still has only the spacebar to work with.

Difficulty scales across the run: drain rate increases and the player's bar shrinks with each boss defeated, so the same button press becomes progressively harder to use effectively.

### Features

- Three unique animated bosses, each with custom death sequences (flashing, explosions, screen shake).
- Animated player character (idle, attack, death) with a lightning bolt attack effect.
- Dragon Phase 2 transformation cutscene with additional difficulty modifiers.
- Difficulty scaling (escape rate, bar shrink) across the endless boss loop.
- Pause menu with master volume control (Escape key).
- Sound effects for boss/player death explosions and the Phase 2 transition.
- Press **R** at any time to restart.

## Controls

| Input | Action |
|---|---|
| **Spacebar** (hold) | Move player indicator up |
| **Spacebar** (release) | Player indicator falls |
| **Escape** | Pause / Resume |
| **R** | Restart |

## Credits

### Art

- **Wolf boss sprite** -- Segel on [OpenGameArt](https://opengameart.org)
- **Clock boss sprite** -- Forklifting\_Madman on Reddit
- **Dragon boss sprite** -- ZaPaper on [OpenGameArt](https://opengameart.org)
- **Phase 2 dragon sprite** -- kisk123 on Reddit
- **Player character sprite** -- Calciumtrice on [OpenGameArt](https://opengameart.org)

### Tools & AI

AI (Claude / Cursor) was used to assist with scripting and code implementation throughout the project.
