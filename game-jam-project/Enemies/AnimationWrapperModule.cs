using Godot;
using System;

public partial class AnimationWrapperModule : AnimatedSprite2D
{	
	private Vector2 lastDir;

	private bool ChangeAnimation(string anim)
	{
		try
		{
			this.Play(anim);
			return true;
		}
		catch
		{
			GD.PushWarning("Animation: " + anim + "Does not exist!");
			return false;
		}
	}

	private void Freeze()
	{
		SpeedScale = 0;
	}

	private void Go(float s)
	{
		SpeedScale = s;
	}

	public void ChangeOnDir(Vector2 dir)
	{
		if(dir.Length() < .1)
		{
			if(SpeedScale > .1f)
			{
				Freeze();
			}
			return;
		}
		if(SpeedScale < .1f)
		{
			Go(1.0f);
		}
		if(dir.X < 0)
		{
			FlipH = true;
			ChangeAnimation("Walking");
		}
		else
		{
			FlipH = false;
			ChangeAnimation("Walking");
		}
	}
}
