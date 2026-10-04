using Godot;

namespace WolfUI;

// Shared state for the glass shaders, used by GlassPanel and GlassContainer. Swaps to the cheap shader when
// effects drop to Reduced.
public sealed class GlassSurface
{
	private static Shader? _full;
	private static Shader? _lite;
	private readonly CanvasItem _owner;
	private ShaderMaterial _material = null!;

	private Vector2 _size;
	private float _radius, _blur, _refraction, _bevel, _rim, _glow;
	private Color _tint = Colors.White;

	public GlassSurface(CanvasItem owner)
	{
		_owner = owner;
		Rebuild();
		Effects.Changed += Rebuild;
		_owner.TreeExiting += () => Effects.Changed -= Rebuild;
	}

	private void Rebuild()
	{
		_full ??= GD.Load<Shader>("res://Shaders/liquid_glass.gdshader");
		_lite ??= GD.Load<Shader>("res://Shaders/glass_lite.gdshader");
		_material = new ShaderMaterial { Shader = Effects.IsFull ? _full : _lite };
		_owner.Material = _material;
		Push();
	}

	public void Update(Vector2 size, float radius, float blur, float refraction, float bevel, Color tint, float rim)
	{
		_size = size;
		_radius = radius;
		_blur = blur;
		_refraction = refraction;
		_bevel = bevel;
		_tint = tint;
		_rim = rim;
		Push();
	}

	public void SetGlow(float glow)
	{
		_glow = glow;
		_material.SetShaderParameter("glow", glow);
	}

	private void Push()
	{
		var scale = _owner.GetViewport()?.GetFinalTransform().Scale.X ?? 1f;
		_material.SetShaderParameter("rect_size", _size);
		_material.SetShaderParameter("ui_scale", scale);
		_material.SetShaderParameter("radius", _radius);
		_material.SetShaderParameter("blur_lod", _blur);
		_material.SetShaderParameter("refraction", _refraction);
		_material.SetShaderParameter("bevel", _bevel);
		_material.SetShaderParameter("tint", _tint);
		_material.SetShaderParameter("rim_strength", _rim);
		_material.SetShaderParameter("glow", _glow);
	}
}
