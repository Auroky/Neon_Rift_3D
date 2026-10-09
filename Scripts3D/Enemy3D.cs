// TEACHING GUIDE: Enemy3D is OUR abstract gameplay model; CharacterBody3D is GODOT.
// GODOT API: [Signal], _PhysicsProcess, LookAt, MoveAndSlide, EmitSignal, QueueFree.
// OUR RULES: each subclass defines its stats and movement/shooting behaviour;
// enemies deal contact damage within 1.1 units, at most once every 0.6 seconds.

using Godot;
using System;

public abstract partial class Enemy3D
	: CharacterBody3D
{
	// GODOT signal declaration; OUR event informs Game3D about scoring.
	[Signal]
	public delegate void DestroyedEventHandler(
		int points);

	// GODOT signal declaration; OUR event requests a bullet without creating it.
	[Signal]
	public delegate void ShotRequestedEventHandler(
		Vector3 position,
		Vector3 direction);

	protected Player3D Target { get; private set; }
		= null!;

	// OUR per-enemy configuration: speed (world units/s), damage (HP),
	// score points, and maximum hit points; subclasses supply actual values.
	protected float MoveSpeed { get; set; }

	protected float ContactDamage { get; set; }

	protected int Points { get; set; }

	protected float MaxHealth { get; set; }

	public float Health { get; private set; }

	private bool _dead;

	private float _contactCooldown;

	public void Configure(Player3D target)
	{
		Target = target;

		ConfigureStats();

		Health = MaxHealth;
	}

	// OUR extension points: subclasses supply stats and movement/attack decisions.
	protected abstract void ConfigureStats();

	protected abstract void UpdateBehaviour(
		float delta,
		float distance,
		Vector3 direction);

	public override void _PhysicsProcess(
		double deltaValue)
	{
		if (_dead
			|| !IsInstanceValid(Target)
			|| !Target.Active)
		{
			Velocity = Vector3.Zero;
			return;
		}

		// GODOT passes seconds since the previous physics tick; cap unusually long steps.
		float delta = Math.Min(
			(float)deltaValue,
			0.033f);

		_contactCooldown = Math.Max(
			0f,
			_contactCooldown - delta);

		Vector3 toPlayer =
			Target.GlobalPosition - GlobalPosition;

		toPlayer.Y = 0f;

		float distance = Math.Max(
			toPlayer.Length(),
			0.001f);

		Vector3 direction =
			toPlayer / distance;

		// GODOT rotates the 3D model to face the player on the horizontal XZ plane.
		LookAt(
			GlobalPosition + direction,
			Vector3.Up);

		UpdateBehaviour(
			delta,
			distance,
			direction);

		MoveAndSlide();

		CheckPlayerContact(distance);
	}

	public void TakeDamage(float damage)
	{
		if (_dead)
		{
			return;
		}

		Health = Math.Max(
			0f,
			Health - damage);

		if (Health <= 0f)
		{
			Die();
		}
	}

	protected void RequestShot(
		Vector3 direction)
	{
		EmitSignal(
			SignalName.ShotRequested,
			GlobalPosition + direction * 1.0f,
			direction);
	}

	// OUR contact rule: 1.1-unit reach; 0.6-second cooldown between hits.
	private void CheckPlayerContact(float distance)
	{
		const float ContactDistance = 1.1f;

		if (distance <= ContactDistance
			&& _contactCooldown <= 0f)
		{
			Target.TakeDamage(ContactDamage);

			_contactCooldown = 0.6f;
		}
	}

	private void Die()
	{
		_dead = true;

		EmitSignal(
			SignalName.Destroyed,
			Points);

		QueueFree();
	}
}
