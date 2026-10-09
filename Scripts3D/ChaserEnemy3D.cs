// TEACHING GUIDE: ChaserEnemy3D is OUR subclass of OUR Enemy3D base class.
// GODOT API: Vector3, Velocity. OUR RULE: chase until 1.0 world unit from the player.
// The base class handles movement, contact damage, health, and death.

using Godot;

public partial class ChaserEnemy3D : Enemy3D
{
	protected override void ConfigureStats()
	{
		// OUR CHASER STATS: 38 HP; 2.0 units/s; 14 HP contact damage; 100 points.
		MaxHealth = 38f;

		MoveSpeed = 2.0f;

		ContactDamage = 14f;

		Points = 100;
	}

	protected override void UpdateBehaviour(
		float delta,
		float distance,
		Vector3 direction)
	{
		// OUR behaviour: stop approaching when within 1.0 world unit.
		const float StopDistance = 1.0f;

		if (distance > StopDistance)
		{
			Velocity =
				direction * MoveSpeed;
		}
		else
		{
			Velocity = Vector3.Zero;
		}
	}
}
