using Godot;

// One pickable upgrade. Create these as .tres files in the editor and add one or more modifiers.
[GlobalClass]
public partial class Upgrade : Resource
{
	[Export] public string DisplayName { get; set; } = "New Upgrade";
	[Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
	[Export] public Texture2D Icon { get; set; }
	[Export] public Godot.Collections.Array<StatModifier> Modifiers { get; set; } = new();
	[Export]
	public int Cost { get; set; } = 10; //added for gamemaster
}
