// TEACHING GUIDE: Game3D is OUR game coordinator; Node3D and PackedScene are GODOT.
// GODOT API: _Ready, _PhysicsProcess, GD.Load, GetNode, Instantiate, AddChild.
// OUR RULES: spawn mixed pairs after 1.5 s; win after 10 kills.
// Cycle Chaser / Striker / Heavy, cap the crowd, and gently increase spawn tempo.
// WORLD UNITS: speeds are Godot 3D units per second, not pixels per second.

using Godot;
using System;

public partial class Game3D : Node3D
{
	// OUR victory condition: the player must defeat 10 enemies.
	private const int TargetKills = 10;
	private const int MaxActiveEnemies = 8;

	private readonly RandomNumberGenerator _rng =
		new();

	private PackedScene _chaserScene = null!;

	private PackedScene _strikerScene = null!;

	private PackedScene _heavyScene = null!;

	private PackedScene _bulletScene = null!;

	private Node3D _entities = null!;

	private Player3D _player = null!;

	private float _spawnTimer;

	private int _kills;

	private int _score;

	private int _spawnCount;

	private bool _running;

	private Label _hud = null!;
	private Label _result = null!;

	// GODOT lifecycle callback, called when this scene enters the tree.
	public override void _Ready()
	{
		Engine.TimeScale = 1.0;

		_rng.Randomize();

		_chaserScene =
			GD.Load<PackedScene>(
				"res://Scenes3D/ChaserEnemy3D.tscn");

		_strikerScene =
			GD.Load<PackedScene>(
				"res://Scenes3D/StrikerEnemy3D.tscn");

		_heavyScene =
			GD.Load<PackedScene>(
				"res://Scenes3D/HeavyEnemy3D.tscn");

		_bulletScene =
			GD.Load<PackedScene>(
				"res://Scenes3D/Bullet3D.tscn");

		_entities =
			GetNode<Node3D>("Entities");

		_player =
			GetNode<Player3D>("Player");

		CreateHud();

		// OUR signal-based design: actors request actions; Game3D owns spawning.
		_player.ShotRequested +=
			SpawnPlayerBullet;

		_player.Died +=
			EndWithDefeat;

		StartGame();
	}

	public override void _PhysicsProcess(
		double deltaValue)
	{
		UpdateHud();

		if (!_running)
		{
			return;
		}

		float delta = Math.Min(
			(float)deltaValue,
			0.033f);

		_spawnTimer -= delta;

		if (_spawnTimer <= 0f)
		{
			// OUR small mixed groups: leave space to dodge rather than flood the arena.
			int spaces = MaxActiveEnemies - CountActiveEnemies();
			for (int i = 0; i < Math.Min(2, spaces); i++)
			{
				SpawnEnemy();
			}

			// OUR pacing: 2.5 s initially, 2.1 s at four kills, 1.8 s at eight.
			_spawnTimer = _kills >= 8 ? 1.8f : (_kills >= 4 ? 2.1f : 2.5f);
		}
	}

	private void StartGame()
	{
		ClearEntities();

		_kills = 0;

		_score = 0;

		_spawnCount = 0;

		// OUR opening delay: spawn first enemy 1.5 seconds after game start.
		_spawnTimer = 1.5f;

		_running = true;

		_player.ResetPlayer(
			new Vector3(0f, 0.5f, 0f));

		_result.Text = "";
		UpdateHud();
	}

	private void SpawnEnemy()
	{
		_spawnCount++;

		PackedScene selectedScene =
			// OUR mixed pattern keeps all three roles throughout the round.
			_spawnCount % 3 == 0
				? _heavyScene
				: (_spawnCount % 3 == 1 ? _chaserScene : _strikerScene);

		Enemy3D enemy =
			selectedScene.Instantiate<Enemy3D>();

		_entities.AddChild(enemy);

		enemy.GlobalPosition = SafeArenaEdgePosition();

		enemy.Configure(_player);

		enemy.Destroyed +=
			OnEnemyDestroyed;

		enemy.ShotRequested +=
			SpawnEnemyBullet;
	}

	private int CountActiveEnemies()
	{
		int count = 0;
		foreach (Node child in _entities.GetChildren())
		{
			if (child is Enemy3D enemy && !enemy.IsQueuedForDeletion())
			{
				count++;
			}
		}
		return count;
	}

	private Vector3 SafeArenaEdgePosition()
	{
		// Avoid appearing on top of the player or stacking a fresh pair together.
		Vector3 best = Vector3.Zero;
		float bestClearance = -1f;
		for (int attempt = 0; attempt < 12; attempt++)
		{
			Vector3 candidate = RandomArenaEdgePosition();
			float playerDistance = candidate.DistanceTo(_player.GlobalPosition);
			float clearance = playerDistance;
			foreach (Node child in _entities.GetChildren())
			{
				if (child is Enemy3D enemy && !enemy.IsQueuedForDeletion()
					&& enemy.Health > 0f)
				{
					clearance = Math.Min(clearance,
						candidate.DistanceTo(enemy.GlobalPosition) * 2f);
				}
			}
			if (clearance > bestClearance)
			{
				best = candidate;
				bestClearance = clearance;
			}
			if (playerDistance >= 4f && clearance >= 4f)
			{
				return candidate;
			}
		}
		return best;
	}

	private Vector3 RandomArenaEdgePosition()
	{
		// OUR arena spawn bounds (world units), slightly inside the walls.
		const float MaxX = 8.5f;
		const float MaxZ = 5.5f;

		int side =
			_rng.RandiRange(0, 3);

		if (side == 0)
		{
			return new Vector3(
				_rng.RandfRange(-MaxX, MaxX),
				0.5f,
				-MaxZ);
		}

		if (side == 1)
		{
			return new Vector3(
				MaxX,
				0.5f,
				_rng.RandfRange(-MaxZ, MaxZ));
		}

		if (side == 2)
		{
			return new Vector3(
				_rng.RandfRange(-MaxX, MaxX),
				0.5f,
				MaxZ);
		}

		return new Vector3(
			-MaxX,
			0.5f,
			_rng.RandfRange(-MaxZ, MaxZ));
	}

	private void SpawnPlayerBullet(
		Vector3 position,
		Vector3 direction)
	{
		// OUR PLAYER BULLET: friendly, 14 units/s, 24 HP damage.
		SpawnBullet(
			position,
			direction,
			false,
			14f,
			24f);
	}

	private void SpawnEnemyBullet(
		Vector3 position,
		Vector3 direction)
	{
		// OUR ENEMY BULLET: hostile, 8 units/s, 11 HP damage.
		SpawnBullet(
			position,
			direction,
			true,
			8f,
			11f);
	}

	private void SpawnBullet(
		Vector3 position,
		Vector3 direction,
		bool hostile,
		float speed,
		float damage)
	{
		Bullet3D bullet =
			_bulletScene.Instantiate<Bullet3D>();

		_entities.AddChild(bullet);

		bullet.GlobalPosition =
			position;

		bullet.Configure(
			direction,
			speed,
			damage,
			hostile);
	}

	// OUR scoring rule: each destroyed enemy counts as one kill and adds its points.
	private void OnEnemyDestroyed(int points)
	{
		if (!_running)
		{
			return;
		}

		_kills++;

		_score += points;

		UpdateHud();

		GD.Print(
			$"Kills: {_kills}/{TargetKills}"
			+ $" | Score: {_score}");

		if (_kills >= TargetKills)
		{
			_running = false;

			_player.Active = false;

			_result.Text = $"VICTORY!\nScore: {_score}\nPress R to play again";

			GD.Print("VICTORY");
		}
	}

	private void EndWithDefeat()
	{
		if (!_running)
		{
			return;
		}

		_running = false;

		UpdateHud();
		_result.Text = $"GAME OVER\nKills: {_kills}/{TargetKills}  |  Score: {_score}\nPress R to retry";

		GD.Print("GAME OVER!!!!");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_running && @event is InputEventKey key
			&& key.Pressed && !key.Echo && key.Keycode == Key.R)
		{
			StartGame();
			GetViewport().SetInputAsHandled();
		}
	}

	private void CreateHud()
	{
		CanvasLayer layer = new() { Name = "Hud" };
		AddChild(layer);

		_hud = new Label { Name = "Stats", Position = new Vector2(20f, 14f) };
		StyleHudLabel(_hud, 24);
		layer.AddChild(_hud);

		Label hint = new()
		{
			Name = "Controls",
			Position = new Vector2(20f, 50f),
			Text = "WASD: move  |  Mouse: aim / hold left to shoot\nRed: Chaser  |  Pink: Striker  |  Purple: Heavy (9 hits)"
		};
		StyleHudLabel(hint, 17);
		layer.AddChild(hint);

		_result = new Label
		{
			Name = "Result",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		StyleHudLabel(_result, 36);
		layer.AddChild(_result);
		_result.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
	}

	private static void StyleHudLabel(Label label, int fontSize)
	{
		label.MouseFilter = Control.MouseFilterEnum.Ignore;
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
		label.AddThemeConstantOverride("outline_size", 5);
	}

	private void UpdateHud()
	{
		_hud.Text = $"HP: {_player.Health:0}/100    Kills: {_kills}/{TargetKills}    Score: {_score}";
		_hud.AddThemeColorOverride("font_color",
			_player.Health <= 35f ? new Color(1f, 0.45f, 0.4f) : Colors.White);
	}

	private void ClearEntities()
	{
		foreach (Node child
			in _entities.GetChildren())
		{
			child.QueueFree();
		}
	}
}
