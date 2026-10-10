# Neon Rift 3D — Week 8 Collaboration Demo

A Godot 4 **.NET/C#** teaching project to demonstrate OOP, game rules,
Godot APIs, and GitHub collaboration. All source-code comments are in English.

## Running

1. Open this directory as a project in a compatible Godot 4 .NET editor.
2. Build the C# project in Godot, then run `Scenes3D/Main3D.tscn`.
3. Use **W/A/S/D** to move; **move the mouse** to aim; hold **left mouse button** to shoot.
4. Watch the 60-second countdown, HP, kills and score on screen. Timeout or death
   ends the round and displays the saved top-five leaderboard. Press **R** or click
   **PLAY AGAIN** to retry.

The project targets `Godot.NET.Sdk/4.7.1` and `net8.0` as in the supplied
project. The assembly is named `NeonRiftStage1` for compatibility; this
legacy assembly name is **not** the current gameplay stage or dimension.

## Game rules (values defined by our C# code)

The presentation uses plum instrument panels, mint player health, amber time/score,
colored enemy badges and a card-based results table. Player and enemies have
camera-projected overhead HP bars with a trailing damage indicator. Valid hits
briefly flash the struck tank's private material; destroyed enemies leave sparks,
an expanding blast ring and fading smoke. These visual helpers contain no damage,
collision, AI or score rules; the arena and tank models are unchanged.

| Rule | Value | File |
| --- | --- | --- |
| Round | Score challenge lasting up to 60 seconds; death ends it early | `Game3D.cs` |
| Initial enemy spawn | Mixed pair 1.5 s after start | `Game3D.cs` |
| Subsequent spawns | Up to two; every 2.5 s, 2.1 s at 4 kills, 1.8 s at 8 kills | `Game3D.cs` |
| Enemy mix | Cycle Chaser, Striker, Heavy; at most 8 active enemies | `Game3D.cs` |
| Player max health | 100 HP | `Player3D.cs` |
| Player movement | 7 world units/s | `Player3D.cs` |
| Player firing rate | 5 shots/s (0.2 s apart) | `Player3D.cs` |
| Player damage immunity | 0.6 s after taking damage | `Player3D.cs` |
| Player bullet | 14 units/s, 24 damage, 2 s lifetime | `Game3D.cs`, `Bullet3D.cs` |
| Enemy bullet | 8 units/s, 11 damage, 4 s lifetime | `Game3D.cs`, `Bullet3D.cs` |
| Chaser | 38 HP, 2.0 units/s, 14 contact damage, 100 score | `ChaserEnemy3D.cs` |
| Chaser stopping distance | 1.0 world unit | `ChaserEnemy3D.cs` |
| Striker | 72 HP, 3.2 units/s, 22 contact damage, 180 score | `StrikerEnemy3D.cs` |
| Striker preferred distance | 4.5–6.5 world units; strafe inside this band | `StrikerEnemy3D.cs` |
| Striker fire | First at 1.2 s, then every 1.45 s if within 12 units | `StrikerEnemy3D.cs` |
| Heavy | 200 HP, 1.2 units/s, 35 contact damage, 300 score; nine player bullets to destroy | `HeavyEnemy3D.cs` |
| Score history | Save completed rounds to `user://score_history.json`; show top five | `ScoreHistory.cs` |
| Enemy contact | Up to 1 hit per 0.6 s within 1.1 units | `Enemy3D.cs` |

**Engine vs user code:** `Node3D`, `CharacterBody3D`, `Vector3`,
`_PhysicsProcess`, `MoveAndSlide`, `MoveAndCollide`, `GD.Load`, etc. are
**Godot-provided**. Game parameters, behaviours, class responsibilities,
and win/lose conditions are **our own design**.

## Week 8 GitHub collaboration

**Student A**: edit `Scripts3D/StrikerEnemy3D.cs` — change the shooting
interval from `1.45f` to `1.2f`; also update the corresponding comment.

**Student B**: edit `Scripts3D/Player3D.cs` — change the player speed
from `7f` to `8.5f`; also update the corresponding comment.

These independent edits are normally **mergeable** because they modify
different files. To intentionally demonstrate a **merge conflict**, let
both students branch from the same commit and change
`Game3D.cs`'s `RoundDuration = 60.0` line to different values. Merge one
branch into `main` first, then merge the other **without first reconciling
those conflicting changes**; Git may require manual resolution. The
existence of two pull requests alone does not necessarily create a conflict.

For detailed call flow, signals, and collision layers, see `ARCHITECTURE.md`.

## Repair regression check

After building, run `Tests/repair_smoke.gd` using Godot .NET. On Windows, set
`APPDATA` for the test process to a fresh directory under `.godot/repair-test-user/`
so the test never writes to the player's score history. The test refuses to run
against a normal or previously populated save directory. It checks movement,
all three enemy spawns, bullet collisions, heavy health, score, timeout, death,
R-to-restart and saved leaderboard reload. Success prints `REPAIR_RUNTIME_CHECK_OK`.

After resource import, `Tests/resource_check.gd` verifies scene dependencies and
reports any unused assets under `Assets/`. Run it with
`--headless --path . --script Tests/resource_check.gd`.
`Tests/visual_preview.gd` renders the actual actor scenes
in a paused arrangement to `.godot/visual_preview.png`; it needs a graphical renderer.
These test scripts are not part of normal gameplay.

`Tests/presentation_check.gd` checks real HP fractions, exactly-once non-colliding
explosions, automatic cleanup, five recorded rounds and button restart. Use a fresh
`APPDATA` directory containing `presentation-test-user`. With a graphical renderer,
append `-- --capture-dir C:/absolute/output` to save combat and results PNGs there.
The screenshots use completed test rounds in that disposable directory, not the
player's real score history. Success prints `PRESENTATION_CHECK_OK`.
