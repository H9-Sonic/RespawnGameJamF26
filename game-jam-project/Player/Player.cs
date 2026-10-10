using Godot;

public partial class Player : CharacterBody2D, IDamageable
{
	private enum State { Normal, Attacking, Dashing, Dead }

	// Must match the animation names in the AnimatedSprite2D's SpriteFrames (case-sensitive).
	private const string AnimIdle = "Idle";
	private const string AnimWalk = "Walk";
	private const string AnimAttack = "Attack";
	private const string AnimDeath = "Death";

	[Export] public PlayerStats Stats { get; set; }

	[ExportGroup("Attack Setup")]
	[Export] public int AttackHitFrame { get; set; } = 3;             // frame (0-5) where damage lands
	[Export] public float AttackMoveMultiplier { get; set; } = 0.3f;  // slow walk while swinging

	[ExportGroup("Dash Visuals")]
	[Export] public float DashAnimSpeed { get; set; } = 2f;           // Walk plays this much faster while dashing
	[Export(PropertyHint.Range, "0,1,0.05")] public float DashAlpha { get; set; } = 0.6f;

	[ExportGroup("Visuals")]
	[Export] public bool SpritesFaceLeft { get; set; } = false;       // false = art faces right

	private AnimatedSprite2D _sprite;
	private Node2D _attackPivot;
	private Area2D _hitbox;
	private CollisionShape2D _hitboxShape;

	private State _state = State.Normal;
	private bool _facingRight = true;
	private Vector2 _dashDir;
	private double _dashTimeLeft;
	private double _dashCooldownLeft;
	private double _attackCooldownLeft;
	private float _attackAnimLength = 1f;   // read from the Attack animation in _Ready
	private bool _invulnerable;

	public override void _Ready()
	{
		// Duplicate so multiple players/enemies never share one stats object.
		Stats = (PlayerStats)Stats.Duplicate();
		Stats.Init();
		Stats.Died += OnDied;

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_attackPivot = GetNode<Node2D>("AttackPivot");
		_hitbox = GetNode<Area2D>("AttackPivot/Hitbox");
		_hitboxShape = GetNode<CollisionShape2D>("AttackPivot/Hitbox/CollisionShape2D");
		_hitboxShape.Shape = new RectangleShape2D();

		// Attack and Death are saved with Loop ON in the scene. A looping animation never
		// fires AnimationFinished, so the player would be stuck attacking forever.
		var frames = _sprite.SpriteFrames;
		frames.SetAnimationLoop(AnimAttack, false);
		frames.SetAnimationLoop(AnimDeath, false);

		// Length of one Attack playthrough at speed 1 = frame count / FPS.
		_attackAnimLength = (float)(frames.GetFrameCount(AnimAttack) / frames.GetAnimationSpeed(AnimAttack));

		ApplyRange();
		ApplyFlip();

		_sprite.AnimationFinished += OnAnimationFinished;
		_sprite.FrameChanged += OnFrameChanged;
	}

	public override void _PhysicsProcess(double delta)
	{
		_dashCooldownLeft = Mathf.Max(0.0, _dashCooldownLeft - delta);
		_attackCooldownLeft = Mathf.Max(0.0, _attackCooldownLeft - delta);

		Vector2 input = Input.GetVector("left", "right", "up", "down");

		switch (_state)
		{
			case State.Normal:    HandleNormal(input); break;
			case State.Attacking: HandleAttacking(input); break;
			case State.Dashing:   HandleDashing(delta); break;
			case State.Dead:      Velocity = Vector2.Zero; break;
		}

		MoveAndSlide();
	}

	// ---------- States ----------

	private void HandleNormal(Vector2 input)
	{
		UpdateFacing(input);
		Velocity = input * Stats.MoveSpeed;

		if (WantsDash())
		{
			StartDash(input);
			return;
		}

		if (Input.IsActionJustPressed("attack") && _attackCooldownLeft <= 0.0)
		{
			StartAttack();
			return;
		}

		PlayAnim(input == Vector2.Zero ? AnimIdle : AnimWalk);
	}

	private void HandleAttacking(Vector2 input)
	{
		// Facing is locked during a swing, but you can creep forward and dash-cancel.
		Velocity = input * Stats.MoveSpeed * AttackMoveMultiplier;

		if (WantsDash())
			StartDash(input);
	}

	private void HandleDashing(double delta)
	{
		Velocity = _dashDir * Stats.DashSpeed;
		_dashTimeLeft -= delta;

		if (_dashTimeLeft <= 0.0)
			EndDash();
	}

	// ---------- Facing ----------

	private void UpdateFacing(Vector2 input)
	{
		// Only horizontal input changes facing, so moving up/down keeps the last side.
		if (Mathf.Abs(input.X) > 0.01f)
		{
			_facingRight = input.X > 0f;
			ApplyFlip();
		}
	}

	private void ApplyFlip()
	{
		_sprite.FlipH = SpritesFaceLeft ? _facingRight : !_facingRight;
	}

	// ---------- Attack ----------

	private void StartAttack()
	{
		_state = State.Attacking;
		_attackCooldownLeft = 1.0 / Stats.AttackSpeed;

		ApplyRange();                                            // picks up range changes from upgrades
		_attackPivot.Rotation = _facingRight ? 0f : Mathf.Pi;    // hitbox points the way the sprite faces

		// Stretch/squash the animation so one swing lasts exactly 1 / AttackSpeed seconds.
		PlayAnim(AnimAttack, _attackAnimLength * Stats.AttackSpeed);
	}

	private void OnFrameChanged()
	{
		if (_state != State.Attacking) return;
		if (_sprite.Animation.ToString() != AnimAttack) return;
		if (_sprite.Frame != AttackHitFrame) return;

		foreach (Node2D body in _hitbox.GetOverlappingBodies())
		{
			if (body is IDamageable target)
				target.TakeDamage(Stats.Damage, GlobalPosition);
		}
	}

	private void OnAnimationFinished()
	{
		if (_state == State.Attacking && _sprite.Animation.ToString() == AnimAttack)
			_state = State.Normal;   // PlayAnim resets the speed scale on the next Idle/Walk
	}

	private void ApplyRange()
	{
		var rect = (RectangleShape2D)_hitboxShape.Shape;
		rect.Size = new Vector2(Stats.Range, 32f);
		// Push the shape forward so it starts just in front of the player.
		_hitboxShape.Position = new Vector2(Stats.Range / 2f + 8f, 0f);
	}

	// ---------- Dash ----------

	private bool WantsDash() =>
		Input.IsActionJustPressed("dash") && _dashCooldownLeft <= 0.0;

	private void StartDash(Vector2 input)
	{
		_state = State.Dashing;
		_dashDir = input != Vector2.Zero ? input.Normalized() : (_facingRight ? Vector2.Right : Vector2.Left);
		UpdateFacing(_dashDir);
		_dashTimeLeft = Stats.DashDuration;
		_dashCooldownLeft = Stats.DashCooldown;

		_invulnerable = true;
		SetCollisionMaskValue(3, false);   // pass through enemies while dashing

		// No dash animation exists, so reuse Walk, sped up and slightly transparent.
		_sprite.Modulate = new Color(1f, 1f, 1f, DashAlpha);
		PlayAnim(AnimWalk, DashAnimSpeed);
	}

	private void EndDash()
	{
		_state = State.Normal;
		_invulnerable = false;
		SetCollisionMaskValue(3, true);
		_sprite.Modulate = Colors.White;
	}

	// ---------- Health ----------

	public void TakeDamage(float amount, Vector2 sourcePosition)
	{
		if (_invulnerable || _state == State.Dead) return;
		Stats.TakeDamage(amount);
	}

	private void OnDied()
	{
		_state = State.Dead;
		_invulnerable = false;
		Velocity = Vector2.Zero;
		_sprite.Modulate = Colors.White;
		PlayAnim(AnimDeath);
		GD.Print("Player died");
		// Show a game over screen, respawn, etc.
	}

	// ---------- Animation helper ----------

	private void PlayAnim(string anim, float speed = 1f)
	{
		_sprite.SpeedScale = speed;

		// Only (re)start when the animation changes or has finished.
		if (_sprite.Animation.ToString() != anim || !_sprite.IsPlaying())
			_sprite.Play(anim);
	}
}
