using Godot;

namespace WolfUI;

[Tool, GlobalClass]
public partial class RoundedTexture : TextureRect
{
	[Export] public float Radius { get; set; } = 24f;

	private ShaderMaterial? _material;

	public override void _Ready()
	{
		_material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/rounded_texture.gdshader") };
		Material = _material;
		Resized += Refresh;
		Refresh();
	}

	private void Refresh()
	{
		_material?.SetShaderParameter("rect_size", Size);
		_material?.SetShaderParameter("radius", Radius);
	}
}
