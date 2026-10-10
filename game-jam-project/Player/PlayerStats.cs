using Godot;

[GlobalClass]
public partial class PlayerStats : Resource
{
	[Signal] public delegate void HealthChangedEventHandler(float current, float max);
	[Signal] public delegate void DiedEventHandler();

	[ExportGroup("Core")]
	[Export] public float MaxHealth { get; set; } = 100f;
	[Export] public float MoveSpeed { get; set; } = 200f;     // pixels per second

	[ExportGroup("Attack")]
	[Export] public float Damage { get; set; } = 10f;
	[Export] public float AttackSpeed { get; set; } = 3.0f;   // attacks per second
	[Export] public float Range { get; set; } = 40f;          // length of the hitbox in pixels

	[ExportGroup("Dash")]
	[Export] public float DashSpeed { get; set; } = 550f;
	[Export] public float DashDuration { get; set; } = 0.15f; // seconds
	[Export] public float DashCooldown { get; set; } = 0.8f;  // seconds

	public float CurrentHealth { get; private set; }

	public void Init()
	{
		CurrentHealth = MaxHealth;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}

	public void TakeDamage(float amount)
	{
		if (CurrentHealth <= 0f) return;

		CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);

		if (CurrentHealth <= 0f)
			EmitSignal(SignalName.Died);
	}

	public void Heal(float amount)
	{
		CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}
}
