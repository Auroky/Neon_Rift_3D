// TEACHING GUIDE: StrikerEnemy3D is OUR subclass of OUR Enemy3D class.
// GODOT API: Vector3, Velocity. OUR RULE: keep 4.5–6.5 units from the player;
// strafe in that band; fire every 1.45 seconds within 12 units (first after 1.2 s).

using Godot;

public partial class StrikerEnemy3D : Enemy3D
{
	// OUR opening delay gives the player time to see each newly spawned Striker.
	private float _shootTimer = 1.2f;
	private float _strafeDirection = 1f;

	protected override void ConfigureStats()
	{
		// OUR STRIKER STATS: 72 HP; 3.2 units/s; 22 HP contact damage; 180 points.
		MaxHealth = 72f;

		MoveSpeed = 3.2f;

		ContactDamage = 22f;

		Points = 180;

		_shootTimer = 1.2f;
		_strafeDirection = GlobalPosition.Z >= 0f ? 1f : -1f;
	}

	protected override void UpdateBehaviour(
		float delta,
		float distance,
		Vector3 direction)
	{
		// OUR distance band: retreat inside 4.5 units; approach beyond 6.5.
		const float MinimumDistance = 4.5f;
		const float MaximumDistance = 6.5f;

		if (distance > MaximumDistance)
		{
			Velocity =
				direction * MoveSpeed;
		}
		else if (distance < MinimumDistance)
		{
			Velocity =
				-direction * MoveSpeed;
		}
		else
		{
			// OUR lateral movement makes the player adjust aim while dodging shots.
			Vector3 sideways = new(-direction.Z, 0f, direction.X);
			Velocity = sideways * (1.1f * _strafeDirection);
		}

		_shootTimer -= delta;

		// OUR attack rule: shoot only while target is closer than 12 units.
		if (_shootTimer <= 0f
			&& distance < 12f)
		{
			RequestShot(direction);

			_shootTimer = 1.45f;
		}
	}
}
