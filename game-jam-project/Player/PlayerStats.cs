using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class PlayerStats : Resource
{
	[Signal] public delegate void HealthChangedEventHandler(float current, float max);
	[Signal] public delegate void DiedEventHandler();
	[Signal] public delegate void StatsChangedEventHandler();

	// ---------- Base values (edited in the inspector) ----------

	[ExportGroup("Core")]
	[Export] public float BaseMaxHealth { get; set; } = 100f;
	[Export] public float BaseMoveSpeed { get; set; } = 200f;     // pixels per second

	[ExportGroup("Attack")]
	[Export] public float BaseDamage { get; set; } = 10f;
	[Export] public float BaseAttackSpeed { get; set; } = 3.0f;   // attacks per second
	[Export] public float BaseRange { get; set; } = 40f;          // length of the hitbox in pixels

	[ExportGroup("Dash")]
	[Export] public float BaseDashSpeed { get; set; } = 550f;
	[Export] public float BaseDashDuration { get; set; } = 0.15f; // seconds
	[Export] public float BaseDashCooldown { get; set; } = 0.8f;  // seconds

	// ---------- Final values (base + upgrades). Player.cs keeps using these names. ----------

	public float MaxHealth    => Get(StatType.MaxHealth);
	public float MoveSpeed    => Get(StatType.MoveSpeed);
	public float Damage       => Get(StatType.Damage);
	public float AttackSpeed  => Get(StatType.AttackSpeed);
	public float Range        => Get(StatType.Range);
	public float DashSpeed    => Get(StatType.DashSpeed);
	public float DashDuration => Get(StatType.DashDuration);
	public float DashCooldown => Get(StatType.DashCooldown);

	public float CurrentHealth { get; private set; }

	// Floor so a harsh debuff can never produce 0 or negative (AttackSpeed is used as a divisor).
	private const float MinStatValue = 0.01f;

	private readonly List<Upgrade> _upgrades = new();
	private readonly float[] _cache = new float[Enum.GetValues<StatType>().Length];
	private bool _dirty = true;

	public IReadOnlyList<Upgrade> Upgrades => _upgrades;

	public void Init()
	{
		_dirty = true;
		CurrentHealth = MaxHealth;
		EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
	}

	// ---------- Upgrades ----------

	public void AddUpgrade(Upgrade upgrade)
	{
		float oldMax = MaxHealth;
		_upgrades.Add(upgrade);   // adding the same upgrade twice stacks it
		OnModifiersChanged(oldMax);
	}

	public bool RemoveUpgrade(Upgrade upgrade)
	{
		float oldMax = MaxHealth;
		bool removed = _upgrades.Remove(upgrade);   // removes one stack
		if (removed) OnModifiersChanged(oldMax);
		return removed;
	}

	public void ClearUpgrades()
	{
		float oldMax = MaxHealth;
		_upgrades.Clear();
		OnModifiersChanged(oldMax);
	}

	private void OnModifiersChanged(float oldMax)
	{
		_dirty = true;
		float newMax = MaxHealth;

		// Gaining max health also grants that much current health; losing it clamps current.
		if (newMax > oldMax) CurrentHealth += newMax - oldMax;
		CurrentHealth = Mathf.Min(CurrentHealth, newMax);

		EmitSignal(SignalName.StatsChanged);
		EmitSignal(SignalName.HealthChanged, CurrentHealth, newMax);
	}

	// ---------- Stat calculation ----------

	// final = (base + sum(flat)) * (1 + sum(percent)) * product(multipliers)
	public float Get(StatType stat)
	{
		if (_dirty) Recalculate();
		return _cache[(int)stat];
	}

	private void Recalculate()
	{
		foreach (StatType stat in Enum.GetValues<StatType>())
			_cache[(int)stat] = Compute(stat);
		_dirty = false;
	}

	private float Compute(StatType stat)
	{
		float flat = 0f, percent = 0f, mult = 1f;

		foreach (Upgrade upgrade in _upgrades)
		{
			foreach (StatModifier mod in upgrade.Modifiers)
			{
				if (mod.Stat != stat) continue;

				switch (mod.Type)
				{
					case ModifierType.Flat:       flat += mod.Value; break;
					case ModifierType.PercentAdd: percent += mod.Value; break;
					case ModifierType.Multiplier: mult *= mod.Value; break;
				}
			}
		}

		return Mathf.Max(MinStatValue, (GetBase(stat) + flat) * (1f + percent) * mult);
	}

	private float GetBase(StatType stat) => stat switch
	{
		StatType.MaxHealth    => BaseMaxHealth,
		StatType.MoveSpeed    => BaseMoveSpeed,
		StatType.Damage       => BaseDamage,
		StatType.AttackSpeed  => BaseAttackSpeed,
		StatType.Range        => BaseRange,
		StatType.DashSpeed    => BaseDashSpeed,
		StatType.DashDuration => BaseDashDuration,
		StatType.DashCooldown => BaseDashCooldown,
		_ => 0f,
	};

	// ---------- Health ----------

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
