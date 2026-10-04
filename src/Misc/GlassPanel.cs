using Godot;

namespace WolfUI;

[Tool, GlobalClass]
public partial class GlassPanel : Control
{
	[Export] public float Radius { get; set; } = 32f;
	[Export(PropertyHint.Range, "0,7,0.1")] public float Blur { get; set; } = 2.6f;
	[Export] public float Refraction { get; set; } = 16f;
	[Export] public float Bevel { get; set; } = 22f;
	[Export] public Color Tint { get; set; } = new(1f, 1f, 1f, 0.09f);
	[Export(PropertyHint.Range, "0,2,0.05")] public float Rim { get; set; } = 0.9f;

	private GlassSurface? _surface;
	private float _glow;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public float Glow
	{
		get => _glow;
		set
		{
			_glow = value;
			_surface?.SetGlow(value);
		}
	}

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		_surface = new GlassSurface(this);
		_surface.SetGlow(_glow);
		Resized += Refresh;
		Refresh();
	}

	public void Refresh()
	{
		_surface?.Update(Size, Radius, Blur, Refraction, Bevel, Tint, Rim);
		QueueRedraw();
	}

	public override void _Draw() => DrawRect(new Rect2(Vector2.Zero, Size), Colors.White);
}
