// TEACHING GUIDE: Player3D is OUR player model; CharacterBody3D is GODOT.
// GODOT API: [Signal], _Input, _PhysicsProcess, Input.GetVector, MoveAndSlide,
// Camera3D.ProjectRayOrigin/ProjectRayNormal, LookAt, EmitSignal.
// OUR RULES: 100 health, 7 world units/second, up to 5 shots/second,
// 0.6-second damage immunity after being hit; WASD movement and mouse aiming/shooting.

using Godot;
using System;

public partial class Player3D : CharacterBody3D
{
	// GODOT signal declaration; OUR event requests a shot from Game3D.
	[Signal]
	public delegate void ShotRequestedEventHandler(
		Vector3 position,
		Vector3 direction);

	[Signal]
	public delegate void DiedEventHandler();

	// OUR PLAYER STATS: max HP, movement speed (units/s), firing rate (shots/s).
	private const float MaxHealth = 100f;
	private const float MoveSpeed = 7f;
	private const float FireRate = 5f;

	private float _fireCooldown;
	private float _invulnerable;

	private Vector3 _aimDirection = Vector3.Forward;

	private Vector2 _mousePosition;
	private bool _hasMousePosition;

	public bool Active { get; set; } = true;

	public float Health { get; private set; } = MaxHealth;

	// GODOT input callback: remember screen position for mouse-based aiming.
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			_mousePosition = mouseMotion.Position;
			_hasMousePosition = true;
		}
		else if (@event is InputEventMouseButton mouseButton)
		{
			_mousePosition = mouseButton.Position;
			_hasMousePosition = true;
		}
	}

	public override void _PhysicsProcess(double deltaValue)
	{
		float delta = Math.Min(
			(float)deltaValue,
			0.033f);

		_fireCooldown = Math.Max(
			0f,
			_fireCooldown - delta);

		_invulnerable = Math.Max(
			0f,
			_invulnerable - delta);

		if (!Active)
		{
			Velocity = Vector3.Zero;
			return;
		}

		// GODOT Input Map maps WASD actions to a normalized 2D direction.
		Vector2 input = Input.GetVector(
			"move_left",
			"move_right",
			"move_up",
			"move_down");

		Vector3 movement = new(
			input.X,
			0f,
			input.Y);

		Velocity = movement * MoveSpeed;

		MoveAndSlide();

		AimAtMouse();

		if (Input.IsMouseButtonPressed(MouseButton.Left)
			&& _fireCooldown <= 0f)
		{
			Fire();
		}
	}

	// OUR aiming algorithm uses GODOT camera rays to intersect the player-height XZ plane.
	private void AimAtMouse()
	{
		if (!_hasMousePosition)
		{
			return;
		}

		Camera3D? camera =
			GetViewport().GetCamera3D();

		if (camera == null)
		{
			return;
		}

		Vector3 rayOrigin =
			camera.ProjectRayOrigin(_mousePosition);

		Vector3 rayDirection =
			camera.ProjectRayNormal(_mousePosition);

		if (Math.Abs(rayDirection.Y) < 0.001f)
		{
			return;
		}

		float distance =
			(GlobalPosition.Y - rayOrigin.Y)
			/ rayDirection.Y;

		if (distance <= 0f)
		{
			return;
		}

		Vector3 target =
			rayOrigin + rayDirection * distance;

		Vector3 direction =
			target - GlobalPosition;

		direction.Y = 0f;

		if (direction.LengthSquared() < 0.001f)
		{
			return;
		}

		_aimDirection =
			direction.Normalized();

		LookAt(
			GlobalPosition + _aimDirection,
			Vector3.Up);
	}

	private void Fire()
	{
		// OUR fire-rate limit: at 5 shots/s, minimum interval = 0.2 s.
		_fireCooldown = 1f / FireRate;

		EmitSignal(
			SignalName.ShotRequested,
			GlobalPosition + _aimDirection * 0.9f,
			_aimDirection);
	}

	public void TakeDamage(float damage)
	{
		if (!Active || _invulnerable > 0f)
		{
			return;
		}

		Health = Math.Max(
			0f,
			Health - damage);

		// OUR rule: ignore further damage for 0.6 s after a valid hit.
		_invulnerable = 0.6f;

		if (Health <= 0f)
		{
			Active = false;
			Visible = false;

			EmitSignal(
				SignalName.Died);
		}
	}

	public void ResetPlayer(Vector3 position)
	{
		GlobalPosition = position;

		Velocity = Vector3.Zero;

		Health = MaxHealth;

		_fireCooldown = 0f;
		_invulnerable = 0f;

		_aimDirection = Vector3.Forward;

		Active = true;
		Visible = true;
	}
}
