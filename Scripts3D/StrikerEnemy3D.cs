// TEACHING GUIDE: StrikerEnemy3D is OUR subclass of OUR Enemy3D class.
// GODOT API: Vector3, Velocity. OUR RULE: keep 4.5–6.5 units from the player;
// fire every 1.6 seconds when within 12 units (first shot after 1 second).

using Godot;

public partial class StrikerEnemy3D : Enemy3D
{
	// OUR rule: the initial shot delay is 1.0 s, then 1.6 s per shot.
	private float _shootTimer = 1f;

	protected override void ConfigureStats()
	{
		// OUR STRIKER STATS: 72 HP; 3.2 units/s; 22 HP contact damage; 180 points.
		MaxHealth = 72f;

		MoveSpeed = 3.2f;

		ContactDamage = 22f;

		Points = 180;
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
			Velocity =
				Vector3.Zero;
		}

		_shootTimer -= delta;

		// OUR attack rule: shoot only while target is closer than 12 units.
		if (_shootTimer <= 0f
			&& distance < 12f)
		{
			RequestShot(direction);

			_shootTimer = 1.6f;
		}
	}
}
