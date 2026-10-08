using Godot;

namespace WolfUI;

// The helix shader, rendered into a half-resolution viewport and scaled up: a quarter of the pixels, and it is soft
// anyway. Full effects animate it; reduced effects draw one still frame per change, so the UI can idle.
public partial class XmbBackdrop : SubViewportContainer
{
	private readonly SubViewport _viewport = new() { Disable3D = true, TransparentBg = false };
	private readonly ShaderMaterial _material = new() { Shader = GD.Load<Shader>("res://Shaders/xmb_helix.gdshader") };
	private Color _a, _b, _targetA, _targetB;
	private float _nav, _targetNav;
	private double _time = 7.0; // a nice starting pose for the still frame

	public override void _Ready()
	{
		Stretch = true;
		StretchShrink = 2;
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		var rect = new ColorRect { Material = _material, MouseFilter = MouseFilterEnum.Ignore };
		rect.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_viewport.AddChild(rect);
		AddChild(_viewport);

		_viewport.SizeChanged += () =>
		{
			_material.SetShaderParameter("rect_size", (Vector2)_viewport.Size);
			Redraw();
		};

		Effects.Changed += Refresh;
		TreeExiting += () => Effects.Changed -= Refresh;
		Refresh();
	}

	public void SetPalette(XmbPalette palette, bool instant)
	{
		_targetA = palette.A;
		_targetB = palette.B;
		if (instant || !Effects.IsFull)
		{
			_a = _targetA;
			_b = _targetB;
		}
		Push();
	}

	// Where the menu is (category + position): the helix twists and the bokeh slides as you move around it
	public void SetNav(float nav, bool instant)
	{
		_targetNav = nav;
		if (instant || !Effects.IsFull) _nav = nav;
		Push();
	}

	private void Refresh()
	{
		SetProcess(Effects.IsFull);
		_viewport.RenderTargetUpdateMode = Effects.IsFull ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Once;
		_a = _targetA;
		_b = _targetB;
		_nav = _targetNav;
		Push();
	}

	public override void _Process(double delta)
	{
		_time += delta;
		var k = 1f - Mathf.Exp(-(float)delta * 4f);
		_a = _a.Lerp(_targetA, k);
		_b = _b.Lerp(_targetB, k);
		_nav = Mathf.Lerp(_nav, _targetNav, 1f - Mathf.Exp(-(float)delta * 2.2f));
		Push();
	}

	private void Push()
	{
		_material.SetShaderParameter("time", (float)_time);
		_material.SetShaderParameter("nav", _nav);
		_material.SetShaderParameter("colour_a", new Vector3(_a.R, _a.G, _a.B));
		_material.SetShaderParameter("colour_b", new Vector3(_b.R, _b.G, _b.B));
		Redraw();
	}

	private void Redraw()
	{
		if (!Effects.IsFull)
			_viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
	}
}
