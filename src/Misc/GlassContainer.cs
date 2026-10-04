using Godot;

namespace WolfUI;

[Tool, GlobalClass]
public partial class GlassContainer : PanelContainer
{
	[Export] public float Radius { get; set; } = 32f;
	[Export] public float Inset { get; set; } = 0f;
	[Export(PropertyHint.Range, "0,7,0.1")] public float Blur { get; set; } = 2.6f;
	[Export] public float Refraction { get; set; } = 16f;
	[Export] public float Bevel { get; set; } = 22f;
	[Export] public Color Tint { get; set; } = new(1f, 1f, 1f, 0.09f);
	[Export(PropertyHint.Range, "0,2,0.05")] public float Rim { get; set; } = 0.9f;

	private GlassSurface? _surface;

	public override void _Ready()
	{
		AddThemeStyleboxOverride("panel", new StyleBoxEmpty
		{
			ContentMarginLeft = Inset,
			ContentMarginTop = Inset,
			ContentMarginRight = Inset,
			ContentMarginBottom = Inset
		});
		_surface = new GlassSurface(this);
		Resized += Refresh;
		Refresh();
	}

	public void Refresh()
	{
		_surface?.Update(Size - new Vector2(Inset, Inset) * 2f, Radius, Blur, Refraction, Bevel, Tint, Rim);
		QueueRedraw();
	}

	public override void _Draw() => DrawRect(new Rect2(new Vector2(Inset, Inset), Size - new Vector2(Inset, Inset) * 2f), Colors.White);
}
