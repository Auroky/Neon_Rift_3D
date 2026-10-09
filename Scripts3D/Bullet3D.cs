// TEACHING GUIDE: Bullet3D is OUR gameplay class; CharacterBody3D is provided by GODOT.
// GODOT API: Vector3, Velocity, CollisionMask, _PhysicsProcess, MoveAndCollide, QueueFree.
// OUR RULES: bullets travel in a straight line; expire after 2 s (player) or 4 s (enemy);
// they damage only the opposing team and disappear at the first collision.

using Godot;
using System;

public partial class Bullet3D
	: CharacterBody3D
{
	// GODOT collision-layer bit flags; layer 1 = player, 2 = enemies, 4 = world.
	private const uint PlayerLayer = 1u;
	private const uint EnemyLayer = 2u;
	private const uint WorldLayer = 8u;

	// OUR state: hostile bullets hit players; friendly bullets hit enemies.
	private bool _hostile;

	private float _damage;

	private float _life;

	public void Configure(
		Vector3 direction,
		float speed,
		float damage,
		bool hostile)
	{
		_hostile = hostile;

		_damage = damage;

		// OUR lifetime limit in seconds; prevents off-screen projectiles accumulating.
		_life = hostile
			? 4f
			: 2f;

		// GODOT Velocity stores movement in world units per second.
		Velocity =
			direction.Normalized() * speed;

		CollisionMask =
			WorldLayer
			| (hostile
				? PlayerLayer
				: EnemyLayer);
	}

	public override void _PhysicsProcess(
		double deltaValue)
	{
		float delta = Math.Min(
			(float)deltaValue,
			0.033f);

		_life -= delta;

		if (_life <= 0f)
		{
			QueueFree();
			return;
		}

		// GODOT reports the first collision along the intended movement.
		KinematicCollision3D? collision =
			MoveAndCollide(
				Velocity * delta);

		if (collision == null)
		{
			return;
		}

		GodotObject collider =
			collision.GetCollider();

		if (_hostile
			&& collider is Player3D player)
		{
			player.TakeDamage(_damage);
		}
		else if (!_hostile
			&& collider is Enemy3D enemy)
		{
			enemy.TakeDamage(_damage);
		}

		QueueFree();
	}
}
