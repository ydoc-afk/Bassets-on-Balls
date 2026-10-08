using System.Linq;
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

	// Four strands (two near, two far) take their colours from a pool made of the palette. Every 10 seconds the
	// strands are dealt new colours and fade to them.
	private const double ShuffleEvery = 10.0;
	private readonly Color[] _strand = new Color[4];
	private readonly int[] _deal = [0, 1, 2, 3];
	private readonly RandomNumberGenerator _rng = new();
	private double _sinceShuffle;
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
			SnapStrands();
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

	private Color[] Pool() =>
	[
		_targetA,
		_targetB,
		_targetA.Lerp(_targetB, 0.5f).Lerp(Colors.White, 0.25f),
		_targetB.Lerp(Colors.White, 0.65f)
	];

	private void Shuffle()
	{
		var before = (int[])_deal.Clone();
		for (var attempt = 0; attempt < 4; attempt++)
		{
			for (var i = _deal.Length - 1; i > 0; i--)
			{
				var j = _rng.RandiRange(0, i);
				(_deal[i], _deal[j]) = (_deal[j], _deal[i]);
			}
			if (!_deal.SequenceEqual(before)) return;
		}
	}

	private void Refresh()
	{
		SetProcess(Effects.IsFull);
		_viewport.RenderTargetUpdateMode = Effects.IsFull ? SubViewport.UpdateMode.Always : SubViewport.UpdateMode.Once;
		_a = _targetA;
		_b = _targetB;
		_nav = _targetNav;
		SnapStrands();
		Push();
	}

	private void SnapStrands()
	{
		var pool = Pool();
		for (var i = 0; i < _strand.Length; i++)
			_strand[i] = pool[_deal[i]];
	}

	public override void _Process(double delta)
	{
		_time += delta;
		var k = 1f - Mathf.Exp(-(float)delta * 4f);
		_a = _a.Lerp(_targetA, k);
		_b = _b.Lerp(_targetB, k);
		_nav = Mathf.Lerp(_nav, _targetNav, 1f - Mathf.Exp(-(float)delta * 2.2f));

		_sinceShuffle += delta;
		if (_sinceShuffle >= ShuffleEvery)
		{
			_sinceShuffle = 0;
			Shuffle();
		}
		var pool = Pool();
		var fade = 1f - Mathf.Exp(-(float)delta * 2.0f);
		for (var i = 0; i < _strand.Length; i++)
			_strand[i] = _strand[i].Lerp(pool[_deal[i]], fade);
		Push();
	}

	private void Push()
	{
		_material.SetShaderParameter("time", (float)_time);
		_material.SetShaderParameter("nav", _nav);
		_material.SetShaderParameter("colour_a", new Vector3(_a.R, _a.G, _a.B));
		_material.SetShaderParameter("colour_b", new Vector3(_b.R, _b.G, _b.B));
		for (var i = 0; i < _strand.Length; i++)
			_material.SetShaderParameter($"strand{i}", new Vector3(_strand[i].R, _strand[i].G, _strand[i].B));
		Redraw();
	}

	private void Redraw()
	{
		if (!Effects.IsFull)
			_viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
	}
}
