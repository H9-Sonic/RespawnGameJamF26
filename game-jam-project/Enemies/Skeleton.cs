using Godot;
using System;

public partial class Skeleton : CharacterBody2D
{
	public const float Speed = 100.0f;
	[Export] 
	float damage = 10.0f;
	[Export]
	float aps = 2.0f;

	[Export]
	float maxHealth = 30.0f;
	float health;
	float attack_speed;
	float attack_cooldown;
	bool within_range = false;

	[Export]
	float stopDistance = 18.0f;


	[Export]
	public AnimationWrapperModule skelAnim;
	NavigationAgent2D Nav2D;
	CharacterBody2D Player;

	public override void _Ready()
	{
		base._Ready();
		Nav2D = GetNode<NavigationAgent2D>("NavigationAgent2D");
		Player = GetParent().GetNode<CharacterBody2D>("Player");
		var attackRange = GetNode<Area2D>("AttackRange");
		attackRange.BodyEntered += OnAttackRangeBodyEnter;
		attackRange.BodyExited += OnAttackRangeBodyExit;
		GD.Print(Player.Name);
		attack_speed = 1.0f / aps;
		attack_cooldown = attack_speed;
	}

    public override void _PhysicsProcess(double delta)
    { 

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

			Vector2 target = Player.GlobalPosition;

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

	public void OnAttackRangeBodyEnter(Node2D body)
	{
		if (body is Player)
		{
			GD.Print("Player entered attack range");
			within_range = true;
		}
	}
	public void OnAttackRangeBodyExit(Node2D body)
	{
		if (body is Player)
		{
			within_range = false;
			attack_cooldown = attack_speed;
			GD.Print("Player exited attack range");
		}
	}

	public void TakeDamage(float amount, Vector2 sourcePos)
	{	
		GD.Print("THEY JUMPING ME");
		health -= amount;
		if(health <= 0.0f)
		{
			QueueFree();
		}
	}

}
