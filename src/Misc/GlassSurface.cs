using Godot;

namespace WolfUI;

// Shared state for the liquid glass shader, used by GlassPanel and GlassContainer.
public sealed class GlassSurface
{
	private static Shader? _shader;
	private readonly CanvasItem _owner;
	private readonly ShaderMaterial _material;

	public GlassSurface(CanvasItem owner)
	{
		_owner = owner;
		_shader ??= GD.Load<Shader>("res://Shaders/liquid_glass.gdshader");
		_material = new ShaderMaterial { Shader = _shader };
		_owner.Material = _material;
	}

	public void Update(Vector2 size, float radius, float blur, float refraction, float bevel, Color tint, float rim)
	{
		var scale = _owner.GetViewport()?.GetFinalTransform().Scale.X ?? 1f;
		_material.SetShaderParameter("rect_size", size);
		_material.SetShaderParameter("ui_scale", scale);
		_material.SetShaderParameter("radius", radius);
		_material.SetShaderParameter("blur_lod", blur);
		_material.SetShaderParameter("refraction", refraction);
		_material.SetShaderParameter("bevel", bevel);
		_material.SetShaderParameter("tint", tint);
		_material.SetShaderParameter("rim_strength", rim);
	}

	public void SetGlow(float glow) => _material.SetShaderParameter("glow", glow);
}
