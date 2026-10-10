# Neon Rift 3D — Architecture and Call Flow

This is a **3D** Godot 4 C# project. `Node3D`, `CharacterBody3D`, `Camera3D`,
`PackedScene`, input handling, collision functions, and the engine lifecycle are
**Godot-provided APIs**. The classes in `Scripts3D/` are **our own C# code**.

## Project layout

- `Scenes3D/Main3D.tscn`: camera, lighting, playable floor and collision bounds.
- `Scenes3D/ArenaDetails.tscn`: decorative platform, railings, sign, barrels and trees.
- Other `Scenes3D/*.tscn`: reusable player, enemy and bullet scenes.
- `Scripts3D/`: gameplay, round coordination and score persistence.
- `Assets/Arena/PaintedYard.gdshader`: pale sand floor and subtle wear.
- `Assets/Tanks/`: two self-contained GLB models, four active color textures and `TankReadable.gdshader`.
  This shared shader provides neutral fill lighting. Only the Striker scene enables
  the purple armor palette; the Heavy uses its original gray texture. No outline pass.
  The GLBs preserve the supplied FBX geometry, normals, UVs and transforms, while
  omitting obsolete material references to missing auxiliary textures. Materials
  are assigned by actor scenes. Import sidecars and script/shader UIDs are required metadata.
- `Tests/`: gameplay regression, resource dependency audit and deterministic visual preview.
- `.godot/`: ignored, regenerable editor/build caches and current verification output.

## Main flow

```text
Godot loads Scenes3D/Main3D.tscn
  -> Game3D._Ready()                [Godot lifecycle callback]
     -> load reusable enemy/bullet scenes via GD.Load() [Godot]
     -> connect C# gameplay signals; StartGame() [Our logic]
Each Godot physics tick:
  -> Game3D._PhysicsProcess()       -> spawn timer / enemy spawn [Our rules]
  -> Player3D._PhysicsProcess()     -> WASD, motion, mouse aim, request shot
  -> Enemy3D._PhysicsProcess()      -> select behaviour, move, contact attack
     -> ChaserEnemy3D / StrikerEnemy3D / HeavyEnemy3D.UpdateBehaviour()
  -> Bullet3D._PhysicsProcess()     -> move, collide, damage, expire
```

## Responsibilities and signals

| Our class | Responsibility | Godot base class |
| --- | --- | --- |
| Game3D | 60-second rounds, spawning, score, results and restart | Node3D |
| Player3D | Movement, aim, fire cooldown, HP | CharacterBody3D |
| Enemy3D | Shared enemy health, contact damage, signals | CharacterBody3D |
| ChaserEnemy3D | Chasing movement and Chaser stats | Our Enemy3D |
| StrikerEnemy3D | Ranged-attack AI and Striker stats | Our Enemy3D |
| HeavyEnemy3D | Slow chasing, 200 HP and Heavy stats | Our Enemy3D |
| ScoreHistory | Persistent round scores and top-five ranking | Plain C# class |
| Bullet3D | Bullet lifetime, movement, collision/damage | CharacterBody3D |

- `Player3D.ShotRequested` -> `Game3D.SpawnPlayerBullet()`
- `Player3D.Died` -> `Game3D.EndWithDefeat()`
- `Enemy3D.ShotRequested` -> `Game3D.SpawnEnemyBullet()`
- `Enemy3D.Destroyed` -> `Game3D.OnEnemyDestroyed()`

Godot's `[Signal]` and `EmitSignal()` mechanism is engine-provided; **the events,
their meaning, and the handlers are designed in our code**. `Game3D` creates the
HUD, round results and restart controls. Status and outcomes are also printed
to Godot's output panel. Player score history lives in `user://`, outside the project.

## Collision configuration

| Layer number | Layer bit mask | Object |
| ---: | ---: | --- |
| 1 | 1 | Player |
| 2 | 2 | Enemy |
| 3 | 4 | Projectiles |
| 4 | 8 | Arena / walls |

**Important:** Godot's inspector **layer numbers** are 1, 2, 3, 4;
C# collision mask **bit values** are 1, 2, 4, 8, respectively.
The `.tscn` scenes set the bodies' collision layers. `Bullet3D.Configure()`
sets each bullet's mask dynamically to hit the world plus the opposing team.

## Terminology note

The coordinate system is 3D, with movement restricted to the **XZ plane**;
Y is vertical. `Vector3` and `CharacterBody3D` are appropriate here.
