using Godot;

namespace WolfUI;

public static class CardMotion
{
	private const float HoverScale = 1.045f;
	private const float PressScale = 0.965f;

	public static void Attach(Control target, BaseButton trigger, GlassPanel? glass)
	{
		Tween? tween = null;
		var hovered = false;

		void MoveTo(float scale, float glow, float seconds, Tween.TransitionType trans)
		{
			tween?.Kill();
			tween = target.CreateTween().SetParallel().SetTrans(trans).SetEase(Tween.EaseType.Out);
			tween.TweenProperty(target, "scale", Vector2.One * scale, seconds);
			if (glass is not null)
				tween.TweenMethod(Callable.From<float>(v => glass.Glow = v), glass.Glow, glow, seconds);
			target.ZIndex = scale > 1f ? 2 : 0;
		}

		void Settle()
		{
			var active = hovered || trigger.HasFocus();
			MoveTo(active ? HoverScale : 1f, active ? 1f : 0f, 0.28f, Tween.TransitionType.Back);
		}

		target.Resized += () => target.PivotOffset = target.Size / 2f;
		target.PivotOffset = target.Size / 2f;
		trigger.FocusEntered += Settle;
		trigger.FocusExited += Settle;
		trigger.MouseEntered += () => { hovered = true; Settle(); };
		trigger.MouseExited += () => { hovered = false; Settle(); };
		trigger.ButtonDown += () => MoveTo(PressScale, 1f, 0.10f, Tween.TransitionType.Cubic);
		trigger.ButtonUp += Settle;
	}

	public static void PopIn(Control panel)
	{
		panel.Modulate = new Color(1f, 1f, 1f, 0f);
		panel.Scale = new Vector2(0.92f, 0.92f);
		panel.Resized += () => panel.PivotOffset = panel.Size / 2f;
		var tween = panel.CreateTween().SetParallel().SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(panel, "modulate:a", 1f, 0.2);
		tween.TweenProperty(panel, "scale", Vector2.One, 0.3);
	}

	public static void FadeIn(CanvasItem item, int index)
	{
		if (!Effects.IsFull) return;
		item.Modulate = new Color(1f, 1f, 1f, 0f);
		var tween = item.CreateTween();
		tween.TweenInterval(0.035 * index);
		tween.TweenProperty(item, "modulate:a", 1f, 0.35).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
	}
}
