using Godot;
using System;

public partial class Skeleton : CharacterBody2D
{
	public const float Speed = 100.0f;

	[Export]
	public AnimationWrapperModule skelAnim;
	NavigationAgent2D Nav2D;
	CharacterBody2D Player;

	public override void _Ready()
	{
		base._Ready();
		Nav2D = GetNode<NavigationAgent2D>("NavigationAgent2D");
		Player = GetNode<CharacterBody2D>("Player");
	}

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
		Vector2 velocity = Velocity;

		//Chasing the player
		//needs player position
		Vector2 PlayerPos = Player.GlobalPosition;
		Rid nav_map = Nav2D.GetNavigationMap();
		Vector2 safe_target = NavigationServer2D.MapGetClosestPoint(nav_map, PlayerPos);
		Nav2D.TargetPosition = safe_target;

		Vector2 CurrentPos = GlobalPosition;
		Vector2 NextPos = Nav2D.TargetPosition;
		Vector2 direction = (NextPos - CurrentPos).Normalized();
		velocity.X = direction.X * Speed;
		velocity.Y = direction.Y * Speed;


		if (IsInstanceValid(skelAnim))
		{
			skelAnim.ChangeOnDir(Velocity);
		}
		MoveAndSlide();
    }

}
