using Godot;

public partial class ShopCard : PanelContainer
{
	[Signal] public delegate void ChosenEventHandler(int slot);
	[Signal] public delegate void LockToggledEventHandler(int slot, bool locked);

	[Export] public Label NameLabel { get; set; }
	[Export] public Label DescriptionLabel { get; set; }
	[Export] public TextureRect IconRect { get; set; }
	[Export] public Button SelectButton { get; set; }
	[Export] public Button LockButton { get; set; }

	public int Slot { get; private set; }

	public void Setup(int slot, Upgrade upgrade, bool locked)
	{
		Slot = slot;

		NameLabel.Text = upgrade.DisplayName;
		IconRect.Texture = upgrade.Icon;
		DescriptionLabel.Text = string.IsNullOrWhiteSpace(upgrade.Description)
			? UpgradeButton.Summarize(upgrade)
			: upgrade.Description;

		LockButton.SetPressedNoSignal(locked);   // set the state without firing Toggled
		UpdateLockVisual(locked);

		SelectButton.Pressed += () => EmitSignal(SignalName.Chosen, Slot);
		LockButton.Toggled += on =>
		{
			UpdateLockVisual(on);
			EmitSignal(SignalName.LockToggled, Slot, on);
		};
	}

	private void UpdateLockVisual(bool locked)
	{
		LockButton.Text = locked ? "Locked" : "Lock";
		Modulate = locked ? new Color(1f, 0.95f, 0.6f) : Colors.White;   // gold tint when locked
	}

	public void FocusSelect() => SelectButton.GrabFocus();
	
	public bool IsSold { get; private set; }

public void MarkSold()
{
	IsSold = true;

	SelectButton.Disabled = true;
	SelectButton.Text = "Sold";

	LockButton.SetPressedNoSignal(false);
	LockButton.Text = "Lock";
	LockButton.Disabled = true;

	Modulate = new Color(1f, 1f, 1f, 0.5f);   // dimmed
}
}
