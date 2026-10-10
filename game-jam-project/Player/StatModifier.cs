using Godot;

public enum StatType
{
	MaxHealth,
	MoveSpeed,
	Damage,
	AttackSpeed,
	Range,
	DashSpeed,
	DashDuration,
	DashCooldown,
}

public enum ModifierType
{
	Flat,        // +Value                     (e.g. 5   = +5)
	PercentAdd,  // +Value as a fraction       (e.g. 0.2 = +20%, stacks additively with other percents)
	Multiplier,  // *Value                     (e.g. 1.5 = x1.5, stacks multiplicatively)
}

[GlobalClass]
public partial class StatModifier : Resource
{
	[Export] public StatType Stat { get; set; } = StatType.Damage;
	[Export] public ModifierType Type { get; set; } = ModifierType.Flat;
	[Export] public float Value { get; set; } = 0f;
}
