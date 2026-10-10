using System.Collections.Generic;
using Godot;

// Attach to the root Control of your menu scene. Pauses the game while open.
public partial class UpgradeMenu : Control
{
	[Export] public Player PlayerNode { get; set; }
	[Export] public PackedScene CardScene { get; set; }       // scene whose root has UpgradeButton.cs
	[Export] public Container CardContainer { get; set; }     // where the cards get added (HBoxContainer, GridContainer, ...)
	[Export] public Godot.Collections.Array<Upgrade> UpgradePool { get; set; } = new();
	[Export] public int ChoiceCount { get; set; } = 3;
	[Export] public bool RemoveChosenFromPool { get; set; } = false;
	[Export] public bool OpenOnReady { get; set; } = false;   // handy for testing

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;   // so buttons work while the tree is paused
		Visible = false;
		if (OpenOnReady) CallDeferred(MethodName.Open);
	}

	public void Open()
	{
		if (Visible || UpgradePool.Count == 0) return;

		foreach (Node child in CardContainer.GetChildren())
			child.QueueFree();

		var remaining = new List<Upgrade>(UpgradePool);
		UpgradeButton first = null;

		for (int i = 0; i < ChoiceCount && remaining.Count > 0; i++)
		{
			int idx = (int)(GD.Randi() % (uint)remaining.Count);
			Upgrade pick = remaining[idx];
			remaining.RemoveAt(idx);

			var card = CardScene.Instantiate<UpgradeButton>();
			CardContainer.AddChild(card);
			card.Setup(pick);
			card.UpgradeChosen += OnUpgradeChosen;
			first ??= card;
		}

		Visible = true;
		GetTree().Paused = true;
		first?.GrabFocus();   // keyboard / gamepad support
	}

	private void OnUpgradeChosen(Upgrade upgrade)
	{
		PlayerNode.Stats.AddUpgrade(upgrade);
		if (RemoveChosenFromPool) UpgradePool.Remove(upgrade);

		Visible = false;
		GetTree().Paused = false;
	}
}
