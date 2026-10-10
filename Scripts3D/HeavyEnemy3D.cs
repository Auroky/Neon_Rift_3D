// TEACHING GUIDE: HeavyEnemy3D is OUR subclass of OUR Enemy3D base class.
// GODOT API: Vector3, Velocity. OUR RULE: slowly chase the player without shooting.
// The base class handles movement, contact cooldown, health, and the death signal.

using Godot;

public partial class HeavyEnemy3D : Enemy3D
{
	protected override void ConfigureStats()
	{
		// OUR HEAVY STATS: 200 HP; 1.2 units/s; 35 HP contact damage; 300 points.
		// At 24 damage per player bullet, eight hits leave 8 HP; the ninth kills.
		MaxHealth = 200f;

		MoveSpeed = 1.2f;

		ContactDamage = 35f;

		Points = 300;
	}

	protected override void UpdateBehaviour(
		float delta,
		float distance,
		Vector3 direction)
	{
		// OUR behaviour: stop inside the base class's 1.1-unit contact range.
		const float StopDistance = 1.0f;

		if (distance > StopDistance)
		{
			Velocity = direction * MoveSpeed;
		}
		else
		{
			Velocity = Vector3.Zero;
		}
	}
}
