using Godot;

namespace WolfUI;

public enum XmbGlyph { Profile, Games, Coop, Settings, Theme, Sound, Effects, Exit, App, Join, Info }

// A glass tile with a line icon or a picture. Everything is drawn from Size, so it is sharp at any scale.
public partial class XmbIcon : Control
{
	private XmbGlyph _glyph;
	private Texture2D? _texture;
	private bool _tile = true;
	private bool _selected;
	private Color _accent = Colors.White;

	public XmbGlyph Glyph { get => _glyph; set { _glyph = value; QueueRedraw(); } }
	public Texture2D? Texture { get => _texture; set { _texture = value; QueueRedraw(); } }
	public bool Tile { get => _tile; set { _tile = value; QueueRedraw(); } }
	public bool Selected { get => _selected; set { if (_selected == value) return; _selected = value; QueueRedraw(); } }
	public Color Accent { get => _accent; set { if (_accent == value) return; _accent = value; QueueRedraw(); } }

	public XmbIcon()
	{
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public override void _Draw()
	{
		var s = Size.X;
		var rect = new Rect2(Vector2.Zero, Size);
		var radius = (int)(s * 0.2f);

		if (_tile)
		{
			var glass = new StyleBoxFlat
			{
				BgColor = new Color(1, 1, 1, _selected ? 0.16f : 0.08f),
				BorderColor = _selected ? _accent with { A = 0.9f } : new Color(1, 1, 1, 0.22f),
				ShadowColor = _selected ? _accent with { A = 0.45f } : new Color(0, 0, 0, 0.25f),
				ShadowSize = (int)(s * (_selected ? 0.22f : 0.08f)),
				AntiAliasing = true
			};
			glass.SetBorderWidthAll(Mathf.Max(1, (int)(s * 0.025f)));
			glass.SetCornerRadiusAll(radius);
			DrawStyleBox(glass, rect);
		}

		if (_texture is not null)
		{
			// Centre-crop to a square, inset so the tile's rim shows around it
			var tex = _texture.GetSize();
			var side = Mathf.Min(tex.X, tex.Y);
			var src = new Rect2((tex.X - side) / 2f, (tex.Y - side) / 2f, side, side);
			var inset = _tile ? s * 0.08f : 0f;
			DrawTextureRectRegion(_texture, rect.Grow(-inset), src);
			if (_tile)
			{
				// glossy top half, the "reflective glass" look
				var gloss = new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.10f), AntiAliasing = true };
				gloss.SetCornerRadiusAll((int)(radius * 0.8f));
				DrawStyleBox(gloss, new Rect2(rect.Position + new Vector2(inset, inset), new Vector2(s - 2 * inset, (s - 2 * inset) * 0.45f)));
			}
			return;
		}

		DrawGlyph(s);
	}

	private void DrawGlyph(float s)
	{
		var c = new Color(1, 1, 1, 0.92f);
		var w = Mathf.Max(1.5f, s * 0.055f);
		var m = new Vector2(s, s) / 2f;

		switch (_glyph)
		{
			case XmbGlyph.Profile:
				DrawArc(m + new Vector2(0, -s * 0.1f), s * 0.13f, 0, Mathf.Tau, 32, c, w, true);
				DrawArc(m + new Vector2(0, s * 0.3f), s * 0.24f, Mathf.Pi * 1.08f, Mathf.Pi * 1.92f, 24, c, w, true);
				break;
			case XmbGlyph.Coop:
				DrawArc(m + new Vector2(-s * 0.12f, -s * 0.08f), s * 0.1f, 0, Mathf.Tau, 28, c, w, true);
				DrawArc(m + new Vector2(-s * 0.12f, s * 0.3f), s * 0.18f, Mathf.Pi * 1.1f, Mathf.Pi * 1.9f, 20, c, w, true);
				DrawArc(m + new Vector2(s * 0.14f, -s * 0.12f), s * 0.09f, 0, Mathf.Tau, 28, c with { A = 0.7f }, w, true);
				DrawArc(m + new Vector2(s * 0.14f, s * 0.24f), s * 0.16f, Mathf.Pi * 1.12f, Mathf.Pi * 1.88f, 20, c with { A = 0.7f }, w, true);
				break;
			case XmbGlyph.Games:
			{
				var body = new StyleBoxFlat { DrawCenter = false, BorderColor = c, AntiAliasing = true };
				body.SetBorderWidthAll((int)w);
				body.SetCornerRadiusAll((int)(s * 0.14f));
				DrawStyleBox(body, new Rect2(s * 0.2f, s * 0.34f, s * 0.6f, s * 0.32f));
				var d = new Vector2(s * 0.33f, s * 0.5f);
				DrawLine(d - new Vector2(s * 0.06f, 0), d + new Vector2(s * 0.06f, 0), c, w, true);
				DrawLine(d - new Vector2(0, s * 0.06f), d + new Vector2(0, s * 0.06f), c, w, true);
				DrawCircle(new Vector2(s * 0.62f, s * 0.46f), w * 0.8f, c);
				DrawCircle(new Vector2(s * 0.69f, s * 0.54f), w * 0.8f, c);
				break;
			}
			case XmbGlyph.Settings:
				for (var i = 0; i < 8; i++)
				{
					var a = i * Mathf.Tau / 8f;
					var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
					DrawLine(m + dir * s * 0.2f, m + dir * s * 0.29f, c, w * 1.4f, true);
				}
				DrawArc(m, s * 0.2f, 0, Mathf.Tau, 32, c, w, true);
				DrawArc(m, s * 0.07f, 0, Mathf.Tau, 20, c, w, true);
				break;
			case XmbGlyph.Theme:
				DrawArc(m, s * 0.26f, 0, Mathf.Tau, 36, c, w, true);
				DrawCircle(m + new Vector2(-s * 0.1f, -s * 0.08f), s * 0.05f, new Color(1f, 0.45f, 0.3f));
				DrawCircle(m + new Vector2(s * 0.08f, -s * 0.1f), s * 0.05f, new Color(0.3f, 0.75f, 1f));
				DrawCircle(m + new Vector2(s * 0.1f, s * 0.08f), s * 0.05f, new Color(0.5f, 1f, 0.55f));
				break;
			case XmbGlyph.Sound:
				DrawColoredPolygon([
					new Vector2(s * 0.24f, s * 0.42f), new Vector2(s * 0.34f, s * 0.42f), new Vector2(s * 0.48f, s * 0.3f),
					new Vector2(s * 0.48f, s * 0.7f), new Vector2(s * 0.34f, s * 0.58f), new Vector2(s * 0.24f, s * 0.58f)
				], c);
				DrawArc(new Vector2(s * 0.5f, s * 0.5f), s * 0.12f, -0.9f, 0.9f, 12, c, w, true);
				DrawArc(new Vector2(s * 0.5f, s * 0.5f), s * 0.22f, -0.9f, 0.9f, 16, c, w, true);
				break;
			case XmbGlyph.Effects:
				for (var i = 0; i < 4; i++)
				{
					var a = i * Mathf.Pi / 4f;
					var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s * 0.24f;
					DrawLine(m - dir, m + dir, c with { A = i % 2 == 0 ? 0.92f : 0.5f }, w, true);
				}
				break;
			case XmbGlyph.Exit:
				DrawArc(m, s * 0.24f, -Mathf.Pi * 0.5f + 0.6f, Mathf.Pi * 1.5f - 0.6f, 32, c, w, true);
				DrawLine(m - new Vector2(0, s * 0.3f), m - new Vector2(0, s * 0.04f), c, w, true);
				break;
			case XmbGlyph.Join:
			case XmbGlyph.App:
				DrawColoredPolygon([
					new Vector2(s * 0.4f, s * 0.32f), new Vector2(s * 0.68f, s * 0.5f), new Vector2(s * 0.4f, s * 0.68f)
				], c);
				break;
			case XmbGlyph.Info:
				DrawArc(m, s * 0.26f, 0, Mathf.Tau, 36, c, w, true);
				DrawLine(m + new Vector2(0, -s * 0.02f), m + new Vector2(0, s * 0.14f), c, w * 1.2f, true);
				DrawCircle(m + new Vector2(0, -s * 0.12f), w * 0.8f, c);
				break;
		}
	}
}

// Shared easing for the category and item nodes: they glide toward a target centre / scale / opacity.
public partial class XmbNode : Control
{
	public Vector2 Pivot;
	private Vector2 _centre, _targetCentre;
	private float _scale = 1f, _targetScale = 1f, _alpha, _targetAlpha;

	public XmbNode()
	{
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public Vector2 Centre => _targetCentre;
	public float TargetScale => _targetScale;
	public float TargetAlpha => _targetAlpha;

	public void Target(Vector2 centre, float scale, float alpha, bool instant)
	{
		_targetCentre = centre;
		_targetScale = scale;
		_targetAlpha = alpha;
		if (instant)
		{
			_centre = centre;
			_scale = scale;
			_alpha = alpha;
			Apply();
		}
	}

	// Returns true once settled
	public bool Step(float k)
	{
		if (_centre.DistanceSquaredTo(_targetCentre) < 0.04f && Mathf.Abs(_scale - _targetScale) < 0.002f &&
		    Mathf.Abs(_alpha - _targetAlpha) < 0.004f)
		{
			if (_centre == _targetCentre && _scale == _targetScale && _alpha == _targetAlpha) return true;
			_centre = _targetCentre;
			_scale = _targetScale;
			_alpha = _targetAlpha;
			Apply();
			return true;
		}

		_centre = _centre.Lerp(_targetCentre, k);
		_scale = Mathf.Lerp(_scale, _targetScale, k);
		_alpha = Mathf.Lerp(_alpha, _targetAlpha, k);
		Apply();
		return false;
	}

	private void Apply()
	{
		PivotOffset = Pivot;
		Position = _centre - Pivot;
		Scale = new Vector2(_scale, _scale);
		Modulate = new Color(1, 1, 1, _alpha);
		Visible = _alpha > 0.01f;
	}
}

public partial class XmbCategoryView : XmbNode
{
	public readonly XmbIcon Icon = new() { Tile = false };
	public readonly Label Label = new() { HorizontalAlignment = HorizontalAlignment.Center };

	public XmbCategoryView()
	{
		AddChild(Icon);
		AddChild(Label);
	}

	public XmbCategoryView Setup(string name, XmbGlyph glyph)
	{
		Icon.Glyph = glyph;
		Label.Text = name;
		return this;
	}

	public void Resize(float u)
	{
		Size = new Vector2(u, u);
		Pivot = Size / 2f;
		Icon.Size = Size;
		Label.AddThemeFontSizeOverride("font_size", Mathf.Max(10, (int)(u * 0.24f)));
		Label.Size = new Vector2(u * 3f, u * 0.4f);
		Label.Position = new Vector2(-u, u * 0.92f);
	}
}

public partial class XmbItemView : XmbNode
{
	public readonly XmbIcon Icon = new();
	public readonly Label Title = new() { TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
	public readonly Label Subtitle = new() { TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
	private readonly ColorRect _track = new() { Color = new Color(1, 1, 1, 0.15f), MouseFilter = MouseFilterEnum.Ignore };
	private readonly ColorRect _fill = new() { MouseFilter = MouseFilterEnum.Ignore };

	public XmbItemView()
	{
		AddChild(Icon);
		AddChild(Title);
		AddChild(Subtitle);
		AddChild(_track);
		_track.AddChild(_fill);
		Subtitle.Modulate = new Color(1, 1, 1, 0.72f);
		_track.Hide();
	}

	public void Resize(float u, float textWidth)
	{
		Size = new Vector2(u, u);
		Pivot = new Vector2(u, u) / 2f;
		Icon.Size = Size;
		Title.AddThemeFontSizeOverride("font_size", Mathf.Max(10, (int)(u * 0.34f)));
		Subtitle.AddThemeFontSizeOverride("font_size", Mathf.Max(9, (int)(u * 0.22f)));
		Title.Position = new Vector2(u * 1.25f, u * 0.02f);
		Title.Size = new Vector2(textWidth, u * 0.5f);
		Subtitle.Position = new Vector2(u * 1.27f, u * 0.5f);
		Subtitle.Size = new Vector2(textWidth, u * 0.36f);
		_track.Position = new Vector2(u * 1.27f, u * 0.9f);
		_track.Size = new Vector2(Mathf.Min(textWidth, u * 3.2f), Mathf.Max(2f, u * 0.05f));
		_fill.Size = _track.Size with { X = _fill.Size.X };
	}

	// progress 0..1, negative hides the bar
	public void SetProgress(double progress, Color accent)
	{
		_track.Visible = progress >= 0;
		_fill.Color = accent;
		_fill.Size = new Vector2(_track.Size.X * (float)Mathf.Clamp(progress, 0, 1), _track.Size.Y);
	}
}
