# Neon Rift 3D — Week 8 Collaboration Demo

A Godot 4 **.NET/C#** teaching project to demonstrate OOP, game rules,
Godot APIs, and GitHub collaboration. All source-code comments are in English.

## Running

1. Open this directory as a project in a compatible Godot 4 .NET editor.
2. Build the C# project in Godot, then run `Scenes3D/Main3D.tscn`.
3. Use **W/A/S/D** to move; **move the mouse** to aim; hold **left mouse button** to shoot.
4. Observe kills, score, victory, and defeat messages in the **Godot Output panel**.

The project targets `Godot.NET.Sdk/4.7.2` and `net8.0` as in the supplied
project. The assembly is named `NeonRiftStage1` for compatibility; this
legacy assembly name is **not** the current gameplay stage or dimension.

## Game rules (values defined by our C# code)

| Rule | Value | File |
| --- | --- | --- |
| Victory | Defeat 10 enemies | `Game3D.cs` |
| Initial enemy spawn | 1.5 s after start | `Game3D.cs` |
| Subsequent spawns | Every 2.5 s | `Game3D.cs` |
| Enemy progression | Chasers while kills < 4; Strikers from 4 onward | `Game3D.cs` |
| Player max health | 100 HP | `Player3D.cs` |
| Player movement | 7 world units/s | `Player3D.cs` |
| Player firing rate | 5 shots/s (0.2 s apart) | `Player3D.cs` |
| Player damage immunity | 0.6 s after taking damage | `Player3D.cs` |
| Player bullet | 14 units/s, 24 damage, 2 s lifetime | `Game3D.cs`, `Bullet3D.cs` |
| Enemy bullet | 8 units/s, 11 damage, 4 s lifetime | `Game3D.cs`, `Bullet3D.cs` |
| Chaser | 38 HP, 2.0 units/s, 14 contact damage, 100 score | `ChaserEnemy3D.cs` |
| Chaser stopping distance | 1.0 world unit | `ChaserEnemy3D.cs` |
| Striker | 72 HP, 3.2 units/s, 22 contact damage, 180 score | `StrikerEnemy3D.cs` |
| Striker preferred distance | 4.5–6.5 world units | `StrikerEnemy3D.cs` |
| Striker fire | First at 1.0 s, then every 1.6 s if within 12 units | `StrikerEnemy3D.cs` |
| Enemy contact | Up to 1 hit per 0.6 s within 1.1 units | `Enemy3D.cs` |

**Engine vs user code:** `Node3D`, `CharacterBody3D`, `Vector3`,
`_PhysicsProcess`, `MoveAndSlide`, `MoveAndCollide`, `GD.Load`, etc. are
**Godot-provided**. Game parameters, behaviours, class responsibilities,
and win/lose conditions are **our own design**.

## Week 8 GitHub collaboration

**Student A**: edit `Scripts3D/StrikerEnemy3D.cs` — change the shooting
interval from `1.6f` to `1.2f`; also update the corresponding comment.

**Student B**: edit `Scripts3D/Player3D.cs` — change the player speed
from `7f` to `8.5f`; also update the corresponding comment.

These independent edits are normally **mergeable** because they modify
different files. To intentionally demonstrate a **merge conflict**, let
both students branch from the same commit and change
`Game3D.cs`'s `TargetKills = 10` line to different values. Merge one
branch into `main` first, then merge the other **without first reconciling
those conflicting changes**; Git may require manual resolution. The
existence of two pull requests alone does not necessarily create a conflict.

For detailed call flow, signals, and collision layers, see `ARCHITECTURE.md`.
