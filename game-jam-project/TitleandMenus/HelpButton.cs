using Godot;
using System;

public partial class HelpButton : Button
{
	public void _on_pressed()
	{
		GetTree().ChangeSceneToFile("res://TitleandMenus/Help.tscn");
	}
}
