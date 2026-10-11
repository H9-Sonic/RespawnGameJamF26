using Godot;

public partial class TestShop : Node2D
{
	public override void _UnhandledInput(InputEvent e)
	{
		if (e.IsActionPressed("shop"))
			GetNode<ShopMenu>("%ShopMenu").Open();
			GD.Print("shop pressed");
	}
}
