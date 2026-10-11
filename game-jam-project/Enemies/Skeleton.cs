using Godot;
using System;

public partial class Skeleton : CharacterBody2D, IDamageable
{
	public const float Speed = 100.0f;
	[Export] 
	float damage = 2.0f;
	[Export]
	float aps = 2.0f;

	[Export]
	float maxHealth = 30.0f;
	float health;
	float attack_speed;
	float attack_cooldown;
	bool within_range = false;

	[ExportGroup("Knockback")]
	[Export]
	float knockbackForce = 250.0f;
	[Export]
	float knockbackDuration = 0.2f;

	bool dead;

	[Export]
	float stopDistance = 18.0f;


	[Export]
	public AnimationWrapperModule skelAnim;

	public event Action<Skeleton> Died;
	NavigationAgent2D Nav2D;
	CharacterBody2D Player;

	Vector2 knowckbackVelocity;
	float knockbackTimeLeft;







	public override void _Ready()
	{
		base._Ready();

		Nav2D = GetNode<NavigationAgent2D>("NavigationAgent2D");
		Player = GetParent().GetNode<CharacterBody2D>("Player");
		health = maxHealth;
		attack_speed = 1.0f / aps;
		attack_cooldown = attack_speed;
	}

    public override void _PhysicsProcess(double delta)
    { 
		if(dead)
			return;

		if (knockbackTimeLeft > 0.0f)
		{
			knockbackTimeLeft -= (float)delta;
			float t = Mathf.Clamp(knockbackTimeLeft / knockbackDuration, 0.0f, 1.0f);
			Velocity = knowckbackVelocity * t;
			MoveAndSlide();
			return;
		}

		//GD.Print(Nav2D.GetNextPathPosition(), " ", GlobalPosition); //Movement debugging
		
		float dist = GlobalPosition.DistanceTo(Player.GlobalPosition);
		
		bool inRange = dist <= stopDistance;

		attack_cooldown -= (float)delta;
		if (inRange && attack_cooldown <= 0.0f)
		{
			Attack();
			attack_cooldown = attack_speed;
		}


		if (inRange)
		{
			Velocity = Vector2.Zero;
		}
		else
		{
			Nav2D.TargetPosition = Player.GlobalPosition;

			Vector2 target = Player.GlobalPosition;//fallback movement cuz i might be stupid

			if (!Nav2D.IsNavigationFinished())
			{
				Vector2 next = Nav2D.GetNextPathPosition();
				if(next.DistanceTo(GlobalPosition) > 1.0f)
					target = next;
			}
			Velocity = (target - GlobalPosition).Normalized() * Speed;
		}

		
		MoveAndSlide();

		if (IsInstanceValid(skelAnim))
		{
			skelAnim.ChangeOnDir(Velocity);
		}
    }

	public void Attack()
	{
		GD.Print("Attacking");
		if (Player is IDamageable target)
		{
			target.TakeDamage(damage, GlobalPosition);
		}
	}

	public void TakeDamage(float amount, Vector2 sourcePos)
	{	
		if(dead)
			return;

		health -= amount;
		knowckbackVelocity = (GlobalPosition - sourcePos).Normalized() * knockbackForce;
		knockbackTimeLeft = knockbackDuration;
		GD.Print($"Skeleton took damage: {amount}, remaining health: {health}");
		if(health <= 0.0f)
		{
			Die();
		}
	}
	public void Die()
	{
		dead = true;
		Died?.Invoke(this);
		QueueFree();
	}
}
