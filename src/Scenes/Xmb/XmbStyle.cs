using System;
using Godot;

namespace WolfUI;

public readonly record struct XmbPalette(Color A, Color B);

public readonly record struct XmbTheme(string Name, XmbPalette? Palette);

// Themes and the settings the XMB remembers (user://heeler-ui.cfg, so per client state folder).
public static class XmbStyle
{
	public static readonly XmbTheme[] Themes =
	[
		new("Follows category", null),
		new("Aurora", new(new Color(0.15f, 0.55f, 1.0f), new Color(0.2f, 0.95f, 0.85f))),
		new("Ember", new(new Color(1.0f, 0.45f, 0.15f), new Color(1.0f, 0.2f, 0.35f))),
		new("Orchid", new(new Color(0.75f, 0.35f, 1.0f), new Color(1.0f, 0.45f, 0.75f))),
		new("Jade", new(new Color(0.1f, 0.8f, 0.5f), new Color(0.75f, 0.95f, 0.3f))),
		new("Crimson", new(new Color(1.0f, 0.1f, 0.16f), new Color(1.0f, 0.95f, 0.93f))),
		new("Graphite", new(new Color(0.62f, 0.68f, 0.8f), new Color(0.35f, 0.4f, 0.55f)))
	];

	private const string Path = "user://heeler-ui.cfg";
	private static ConfigFile? _cfg;

	private static ConfigFile Cfg
	{
		get
		{
			if (_cfg is not null) return _cfg;
			_cfg = new ConfigFile();
			_cfg.Load(Path); // missing on a first start, the defaults below apply
			return _cfg;
		}
	}

	public static int ThemeIndex
	{
		get => Math.Clamp(Cfg.GetValue("xmb", "theme", 0).AsInt32(), 0, Themes.Length - 1);
		set => Save("theme", value);
	}

	public static bool SoundOn
	{
		get => Cfg.GetValue("xmb", "sound", true).AsBool();
		set => Save("sound", value);
	}

	private static void Save(string key, Variant value)
	{
		Cfg.SetValue("xmb", key, value);
		Cfg.Save(Path);
	}
}

// Short synthesized blips, generated once at start-up: no audio files, a few KB of PCM.
public partial class XmbSounds : Node
{
	private const int Rate = 22050;
	private AudioStreamPlayer? _cat, _item, _ok, _back, _done, _theme;

	public override void _Ready()
	{
		_cat = Add(Blip(520, 980, 0.07f, false, 0.07f));
		_item = Add(Blip(380, 620, 0.05f, false, 0.06f));
		_ok = Add(Mix(Blip(523, 1046, 0.12f, true, 0.10f), Blip(784, 1568, 0.14f, false, 0.05f), 0.07f));
		_back = Add(Blip(620, 300, 0.10f, true, 0.07f));
		_done = Add(Mix(Blip(659, 988, 0.14f, false, 0.08f), Blip(988, 1318, 0.20f, false, 0.07f), 0.11f));
		_theme = Add(Blip(300, 700, 0.18f, false, 0.06f));
	}

	public void Category() => Play(_cat);
	public void Item() => Play(_item);
	public void Accept() => Play(_ok);
	public void Back() => Play(_back);
	public void Done() => Play(_done);
	public void Theme() => Play(_theme);

	private static void Play(AudioStreamPlayer? player)
	{
		if (player is not null && XmbStyle.SoundOn)
			player.Play();
	}

	private AudioStreamPlayer Add(float[] samples)
	{
		var pcm = new byte[samples.Length * 2];
		for (var i = 0; i < samples.Length; i++)
		{
			var v = (short)Math.Clamp(samples[i] * 32767f, -32768f, 32767f);
			pcm[2 * i] = (byte)(v & 0xFF);
			pcm[2 * i + 1] = (byte)((v >> 8) & 0xFF);
		}

		var player = new AudioStreamPlayer
		{
			Stream = new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = Rate, Data = pcm },
			MaxPolyphony = 2
		};
		AddChild(player);
		return player;
	}

	// An exponential sweep from f0 to f1 with an exponential fade, like an oscillator into a decaying gain.
	private static float[] Blip(float f0, float f1, float duration, bool triangle, float volume)
	{
		var n = (int)(duration * Rate);
		var samples = new float[n];
		var phase = 0.0;
		for (var i = 0; i < n; i++)
		{
			var t = (float)i / n;
			var f = f0 * MathF.Pow(f1 / f0, t);
			phase += f / Rate;
			var p = phase % 1.0;
			var wave = triangle ? (float)(4.0 * Math.Abs(p - 0.5) - 1.0) : MathF.Sin((float)(p * Math.Tau));
			var gain = volume * MathF.Pow(0.001f / volume, t);
			var attack = Math.Min(1f, i / (Rate * 0.002f)); // 2 ms ramp so it doesn't click
			samples[i] = wave * gain * attack * 2.5f;
		}
		return samples;
	}

	private static float[] Mix(float[] a, float[] b, float offset)
	{
		var start = (int)(offset * Rate);
		var mixed = new float[Math.Max(a.Length, start + b.Length)];
		a.CopyTo(mixed, 0);
		for (var i = 0; i < b.Length; i++)
			mixed[start + i] += b[i];
		return mixed;
	}
}
