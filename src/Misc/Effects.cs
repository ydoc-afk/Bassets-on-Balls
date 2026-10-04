using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Resources.WolfAPI;

namespace WolfUI;

public enum EffectsLevel
{
	Full,
	Reduced
}

// WOLF_UI_EFFECTS=auto (default) | full | reduced
// Full: live blur/refraction glass, animated backdrop, 60 fps.
// Reduced: flat translucent glass, static backdrop, idle when nothing moves. Costs almost nothing and gives the
// encoder (and weak clients / Wi-Fi) a still picture most of the time.
public static class Effects
{
	private static readonly string[] SoftwareOrMobileGpus =
		["llvmpipe", "softpipe", "swrast", "software", "lavapipe", "v3d", "videocore", "mali", "adreno", "panfrost"];

	private static EffectsLevel? _level;
	private static bool _forced;

	public static event Action? Changed;

	public static EffectsLevel Level => _level ??= Resolve();
	public static bool IsFull => Level == EffectsLevel.Full;
	public static bool IsAuto => !_forced;

	private static EffectsLevel Resolve()
	{
		switch ((System.Environment.GetEnvironmentVariable("WOLF_UI_EFFECTS") ?? "auto").Trim().ToLowerInvariant())
		{
			case "full":
				_forced = true;
				return EffectsLevel.Full;
			case "reduced" or "low":
				_forced = true;
				return EffectsLevel.Reduced;
		}

		var adapter = RenderingServer.GetVideoAdapterName();
		var type = RenderingServer.GetVideoAdapterType();
		var level = type switch
		{
			RenderingDevice.DeviceType.DiscreteGpu => EffectsLevel.Full,
			RenderingDevice.DeviceType.Other when !SoftwareOrMobileGpus.Any(n => adapter.Contains(n, StringComparison.OrdinalIgnoreCase)) => EffectsLevel.Full,
			_ => EffectsLevel.Reduced
		};
		GD.Print($"Effects: {level} (auto, GPU '{adapter}', {type})");
		return level;
	}

	public static void Apply()
	{
		var full = IsFull;
		OS.LowProcessorUsageMode = !full;
		Engine.MaxFps = full ? 60 : 30;
	}

	// Only ever steps down, and only in auto mode, so a struggling session doesn't flip back and forth.
	public static void StepDown(string reason)
	{
		if (_forced || Level == EffectsLevel.Reduced) return;
		GD.Print($"Effects: Reduced ({reason})");
		_level = EffectsLevel.Reduced;
		Apply();
		Changed?.Invoke();
	}

	public static async Task ConsiderStreamAsync()
	{
		if (_forced) return;
		var session = await WolfApi.GetSession();
		if (session?.VideoRefreshRate is > 0 and < 50)
			StepDown($"stream is {session.VideoRefreshRate} Hz");
	}
}
