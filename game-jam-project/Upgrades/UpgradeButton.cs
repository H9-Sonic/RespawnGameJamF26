using System.Collections.Generic;
using Godot;

// Attach to the root Button of your card scene. Assign the labels/icon in the inspector
// (all optional, leave any empty if your layout doesn't have it).
public partial class UpgradeButton : Button
{
	[Signal] public delegate void UpgradeChosenEventHandler(Upgrade upgrade);

	[Export] public Label NameLabel { get; set; }
	[Export] public Label DescriptionLabel { get; set; }
	[Export] public TextureRect IconRect { get; set; }

	public Upgrade Upgrade { get; private set; }

	public void Setup(Upgrade upgrade)
	{
		Upgrade = upgrade;

		if (NameLabel != null) NameLabel.Text = upgrade.DisplayName;
		if (IconRect != null) IconRect.Texture = upgrade.Icon;
		if (DescriptionLabel != null)
			DescriptionLabel.Text = string.IsNullOrWhiteSpace(upgrade.Description) ? Summarize(upgrade) : upgrade.Description;

		Pressed += () => EmitSignal(SignalName.UpgradeChosen, Upgrade);
	}

	// Auto text from the modifiers, used when Description is empty.
	public static string Summarize(Upgrade upgrade)
	{
		var lines = new List<string>();
		foreach (StatModifier m in upgrade.Modifiers)
		{
			lines.Add(m.Type switch
			{
				ModifierType.Flat       => $"{(m.Value >= 0 ? "+" : "")}{m.Value:0.##} {m.Stat}",
				ModifierType.PercentAdd => $"{(m.Value >= 0 ? "+" : "")}{m.Value * 100f:0.##}% {m.Stat}",
				ModifierType.Multiplier => $"x{m.Value:0.##} {m.Stat}",
				_ => "",
			});
		}
		return string.Join("\n", lines);
	}
}
