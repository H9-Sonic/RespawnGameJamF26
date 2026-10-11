using Godot;

// Shown by GameMaster after the player dies. Pauses the game while open.
public partial class GameOverMenu : Control
{
	[Export] public Label TitleLabel { get; set; }
	[Export] public Label StatsLabel { get; set; }
	[Export] public Button RetryButton { get; set; }
	[Export] public Button MainMenuButton { get; set; }
	[Export(PropertyHint.File, "*.tscn")] public string TitleScenePath { get; set; } = "res://TitleandMenus/title_screen.tscn";

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;   // so the buttons work while the tree is paused
		Visible = false;

		RetryButton.Pressed += OnRetry;
		MainMenuButton.Pressed += OnMainMenu;
	}

	public void Open(int wave, int coinsEarned, int kills)
	{
		if (Visible) return;

		StatsLabel.Text =
			$"Reached Wave {wave}\n" +
			$"Enemies defeated: {kills}\n" +
			$"Coins collected: {coinsEarned}";

		Visible = true;
		GetTree().Paused = true;
		RetryButton.GrabFocus();   // keyboard / gamepad support
	}

	private void OnRetry()
	{
		GetTree().Paused = false;
		GetTree().ReloadCurrentScene();
	}

	private void OnMainMenu()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(TitleScenePath);
	}
}
