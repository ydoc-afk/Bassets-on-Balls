using Godot;

namespace WolfUI;

[Tool, GlobalClass]
public partial class Backdrop : ColorRect
{
	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/backdrop.gdshader") };
		Material = material;
		Resized += () => material.SetShaderParameter("rect_size", Size);
		material.SetShaderParameter("rect_size", Size);
	}
}
