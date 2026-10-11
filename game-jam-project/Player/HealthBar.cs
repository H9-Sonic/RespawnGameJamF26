using Godot;

public partial class HealthBar : Node2D
{
	[Export] public float Width { get; set; } = 24f;
	[Export] public float Height { get; set; } = 3f;
	[Export] public bool HideWhenFull { get; set; } = false;   // handy for enemies: only show once damaged

	[Export] public Color BorderColor { get; set; } = new Color(0f, 0f, 0f, 0.85f);
	[Export] public Color BackColor { get; set; } = new Color(0.15f, 0.15f, 0.15f, 0.85f);
	[Export] public Color FullColor { get; set; } = new Color(0.25f, 0.85f, 0.3f);
	[Export] public Color LowColor { get; set; } = new Color(0.9f, 0.2f, 0.2f);

	private float _ratio = 1f;

	public override void _Ready()
	{
		ZIndex = 100;   // always draw above sprites
	}

	public void SetValue(float current, float max)
	{
		_ratio = max > 0f ? Mathf.Clamp(current / max, 0f, 1f) : 0f;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (HideWhenFull && _ratio >= 1f) return;

		// Centered horizontally on this node's position.
		var origin = new Vector2(-Width / 2f, 0f);

		// 1px border, then background, then the fill.
		DrawRect(new Rect2(origin - Vector2.One, new Vector2(Width + 2f, Height + 2f)), BorderColor);
		DrawRect(new Rect2(origin, new Vector2(Width, Height)), BackColor);

		if (_ratio > 0f)
			DrawRect(new Rect2(origin, new Vector2(Width * _ratio, Height)), LowColor.Lerp(FullColor, _ratio));
	}
}
