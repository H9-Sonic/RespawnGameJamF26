using System.Collections.Generic;
using Godot;

public partial class ShopMenu : Control
{
	[Export] public Player PlayerNode { get; set; }
	[Export] public PackedScene CardScene { get; set; }        // ShopCard.tscn
	[Export] public Container CardContainer { get; set; }
	[Export] public Button CloseButton { get; set; }
	[Export] public Godot.Collections.Array<Upgrade> UpgradePool { get; set; } = new();
	[Export] public int ChoiceCount { get; set; } = 3;
	[Export] public bool RemoveChosenFromPool { get; set; } = false;
	[Export] public bool OpenOnReady { get; set; } = false;    // handy for testing

	// What each slot currently holds, and whether the player locked it.
	private Upgrade[] _slots;
	private bool[] _locked;
	private ShopCard[] _cards;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;

		_slots = new Upgrade[ChoiceCount];
		_locked = new bool[ChoiceCount];

		CloseButton.Pressed += Close;

		if (OpenOnReady) CallDeferred(MethodName.Open);
		
		_cards = new ShopCard[ChoiceCount];
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (Visible && e.IsActionPressed("ui_cancel"))
		{
			Close();
			GetViewport().SetInputAsHandled();
		}
	}

	public void Open()
	{
		GD.Print($"Open called. Visible={Visible}, pool={UpgradePool.Count}");
		if (Visible || UpgradePool.Count == 0) return;

		RollSlots();
		GD.Print("rolled");
		BuildCards();
		GD.Print("built");

		Visible = true;
		GetTree().Paused = true;
		GD.Print("opened");
	}

	public void Close()
	{
		Visible = false;
		GetTree().Paused = false;
	}

	// Keep locked slots, reroll the rest without duplicates.
	private void RollSlots()
	{
		var available = new List<Upgrade>(UpgradePool);

		// A locked upgrade that's no longer in the pool gets released.
		for (int i = 0; i < _slots.Length; i++)
		{
			if (_locked[i] && (_slots[i] == null || !available.Contains(_slots[i])))
			{
				_locked[i] = false;
				_slots[i] = null;
			}
		}

		// Locked upgrades can't be rolled again into another slot.
		for (int i = 0; i < _slots.Length; i++)
			if (_locked[i]) available.Remove(_slots[i]);

		for (int i = 0; i < _slots.Length; i++)
		{
			if (_locked[i]) continue;

			if (available.Count == 0) { _slots[i] = null; continue; }

			int idx = (int)(GD.Randi() % (uint)available.Count);
			_slots[i] = available[idx];
			available.RemoveAt(idx);
		}
	}

		private void BuildCards()
	{
		foreach (Node child in CardContainer.GetChildren())
		{
			CardContainer.RemoveChild(child);
			child.QueueFree();
		}

		System.Array.Clear(_cards);

		for (int i = 0; i < _slots.Length; i++)
		{
			if (_slots[i] == null) continue;   // pool ran out, leave the slot empty

			var card = CardScene.Instantiate<ShopCard>();
			CardContainer.AddChild(card);
			card.Setup(i, _slots[i], _locked[i]);
			card.Chosen += OnChosen;
			card.LockToggled += OnLockToggled;
			_cards[i] = card;
		}

		FocusFirstAvailable();
	}

	private void OnLockToggled(int slot, bool locked) => _locked[slot] = locked;

	private void OnChosen(int slot)
	{
		Upgrade upgrade = _slots[slot];
		if (upgrade == null) return;

		PlayerNode.Stats.AddUpgrade(upgrade);

		if (RemoveChosenFromPool) UpgradePool.Remove(upgrade);

		// The slot is consumed: it rerolls the next time the shop opens.
		_locked[slot] = false;
		_slots[slot] = null;

		_cards[slot].MarkSold();
		FocusFirstAvailable();   // a disabled button loses focus, so move it
	}
	
	private void FocusFirstAvailable()
	{
		foreach (ShopCard card in _cards)
		{
			if (card != null && !card.IsSold)
			{
				card.FocusSelect();
				return;
			}
		}

		CloseButton.GrabFocus();
	}
}
