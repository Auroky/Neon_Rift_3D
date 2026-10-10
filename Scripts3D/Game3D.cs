// TEACHING GUIDE: Game3D is OUR game coordinator; Node3D and PackedScene are GODOT.
// GODOT API: _Ready, _PhysicsProcess, GD.Load, GetNode, Instantiate, AddChild.
// OUR RULES: mixed enemies in a 60-second score challenge; death ends the round early.
// Cycle Chaser / Striker / Heavy, cap the crowd, and gently increase spawn tempo.
// WORLD UNITS: speeds are Godot 3D units per second, not pixels per second.

using Godot;
using System;
using System.Text;

public partial class Game3D : Node3D
{
	private const double RoundDuration = 60.0;
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
	private double _remainingTime;
	private ScoreHistory _history = null!;

	private Label _hud = null!;
	private Label _result = null!;
	private CombatHud _presentation = null!;

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
		_history = new ScoreHistory();

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
		if (!_running)
		{
			return;
		}

		// Count the full physics delta so slow frames do not extend the round.
		_remainingTime = Math.Max(0.0, _remainingTime - Math.Max(0.0, deltaValue));
		UpdateHud();
		if (_remainingTime <= 0.000001)
		{
			_remainingTime = 0.0;
			EndRound("TIME UP");
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
		_remainingTime = RoundDuration;

		// OUR opening delay: spawn first enemy 1.5 seconds after game start.
		_spawnTimer = 1.5f;

		_running = true;

		_player.ResetPlayer(
			new Vector3(0f, 0.5f, 0f));

		_result.Text = "";
		_presentation.HideResults();
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
		if (!_running) return;

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
			$"Kills: {_kills}"
			+ $" | Score: {_score}");
	}

	private void EndWithDefeat()
	{
		EndRound("GAME OVER");
	}

	private void EndRound(string reason)
	{
		// A round can finish only once, even if death and the timer happen together.
		if (!_running) return;
		_running = false;
		_player.Active = false;
		_player.Velocity = Vector3.Zero;
		foreach (Node child in _entities.GetChildren()) child.SetPhysicsProcess(false);

		RoundScore round = new()
		{
			Score = _score,
			Kills = _kills,
			SecondsPlayed = RoundDuration - _remainingTime,
			PlayedAt = DateTime.Now,
			Result = reason
		};
		bool saved = _history.Record(round);
		int rank = _history.RankOf(round.Id);
		StringBuilder summary = new();
		summary.AppendLine(reason);
		summary.AppendLine($"Score: {_score}   Kills: {_kills}   Time: {round.SecondsPlayed:0.0}s");
		summary.AppendLine($"This run: #{rank}   |   PERSONAL TOP 5");
		RoundScore[] leaders = _history.TopFive();
		for (int i = 0; i < 5; i++)
		{
			if (i < leaders.Length)
			{
				RoundScore leader = leaders[i];
				string marker = leader.Id == round.Id ? "  < YOU" : "";
				summary.AppendLine($"{i + 1}.  {leader.Score} pts   |   {leader.Kills} kills   |   {leader.PlayedAt:MM/dd HH:mm}{marker}");
			}
			else summary.AppendLine($"{i + 1}.  ---");
		}
		summary.AppendLine(saved ? "Scores saved on this computer" : "Save unavailable: this session only");
		summary.Append("Press R to play again");
		// Godot Labels expect LF line breaks, including on Windows.
		_result.Text = summary.ToString().Replace("\r\n", "\n");
		_presentation.ShowResults(round, rank, leaders, saved);
		UpdateHud();
		GD.Print($"{reason} | Score: {_score} | Kills: {_kills} | Rank: {rank}");
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
		_presentation = new CombatHud { Name = "Hud" };
		AddChild(_presentation);
		_hud = _presentation.Stats;
		_result = _presentation.Result;
		_presentation.RestartRequested += StartGame;
	}

	private void UpdateHud()
	{
		int seconds = (int)Math.Ceiling(_remainingTime);
		_hud.Text = $"Time: {seconds / 60:00}:{seconds % 60:00}    HP: {_player.Health:0}/100    Kills: {_kills}    Score: {_score}";
		_hud.AddThemeColorOverride("font_color",
			_player.Health <= 35f || _remainingTime <= 10.0
				? new Color(1f, 0.45f, 0.4f) : Colors.White);
		_presentation.UpdateStats(_remainingTime, _player.Health, _kills, _score);
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
