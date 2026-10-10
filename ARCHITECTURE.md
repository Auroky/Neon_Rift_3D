# Neon Rift 3D — Architecture and Call Flow

This is a **3D** Godot 4 C# project. `Node3D`, `CharacterBody3D`, `Camera3D`,
`PackedScene`, input handling, collision functions, and the engine lifecycle are
**Godot-provided APIs**. The classes in `Scripts3D/` are **our own C# code**.

## Main flow

```text
Godot loads Scenes3D/Main3D.tscn
  -> Game3D._Ready()                [Godot lifecycle callback]
     -> load reusable enemy/bullet scenes via GD.Load() [Godot]
     -> connect C# gameplay signals; StartGame() [Our logic]
Each Godot physics tick:
  -> Game3D._PhysicsProcess()       -> HUD / mixed spawning / crowd limit [Our rules]
  -> Player3D._PhysicsProcess()     -> WASD, motion, mouse aim, request shot
  -> Enemy3D._PhysicsProcess()      -> select behaviour, move, contact attack
     -> ChaserEnemy3D / StrikerEnemy3D / HeavyEnemy3D.UpdateBehaviour()
  -> Bullet3D._PhysicsProcess()     -> move, collide, damage, expire
```

## Responsibilities and signals

| Our class | Responsibility | Godot base class |
| --- | --- | --- |
| Game3D | Game state, mixed spawning, score, HUD, victory/defeat, retry | Node3D |
| Player3D | Movement, aim, fire cooldown, HP | CharacterBody3D |
| Enemy3D | Shared enemy health, contact damage, signals | CharacterBody3D |
| ChaserEnemy3D | Chasing movement and Chaser stats | Our Enemy3D |
| StrikerEnemy3D | Ranged-attack AI and Striker stats | Our Enemy3D |
| HeavyEnemy3D | Slow chasing movement and Heavy stats | Our Enemy3D |
| Bullet3D | Bullet lifetime, movement, collision/damage | CharacterBody3D |

- `Player3D.ShotRequested` -> `Game3D.SpawnPlayerBullet()`
- `Player3D.Died` -> `Game3D.EndWithDefeat()`
- `Enemy3D.ShotRequested` -> `Game3D.SpawnEnemyBullet()`
- `Enemy3D.Destroyed` -> `Game3D.OnEnemyDestroyed()`

Godot's `[Signal]` and `EmitSignal()` mechanism is engine-provided; **the events,
their meaning, and the handlers are designed in our code**. Game3D creates a
small HUD showing HP, kills, score and round results, and accepts R to retry
after the round ends. Status and outcomes also print to Godot's output panel.
Movement, bullets, actor classes, signals and collision layers retain the
original framework; no dash or new game mode is introduced.

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
**MD5 is a cryptographic hash algorithm, not a Godot 3D movement/model class.**
There is no MD5 reference in this project; it should not be confused with 3D,
`Node3D`, or `CharacterBody3D`.
