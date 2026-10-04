using Godot;

namespace WolfUI;

[Tool, GlobalClass]
public partial class Backdrop : ColorRect
{
	private ShaderMaterial? _material;
	private double _time;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		_material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/backdrop.gdshader") };
		Material = _material;
		Resized += () => _material.SetShaderParameter("rect_size", Size);
		_material.SetShaderParameter("rect_size", Size);

		if (Engine.IsEditorHint()) return;

		void Refresh() => SetProcess(Effects.IsFull);
		Effects.Changed += Refresh;
		TreeExiting += () => Effects.Changed -= Refresh;
		Refresh();
	}

	// Only ticks in Full effects; in Reduced the backdrop is a still picture and the UI can idle.
	public override void _Process(double delta)
	{
		_time += delta;
		_material?.SetShaderParameter("time", (float)_time);
	}
}
