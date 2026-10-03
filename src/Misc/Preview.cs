using System.Collections.Generic;
using System.Linq;
using Godot;

namespace WolfUI;

// Dev helper: BASSETS_SHOT=/path.png [BASSETS_SCREEN=users|apps|menu|dialog|pin] [BASSETS_SHOT_DELAY=seconds]
// saves a screenshot after the given delay and quits.
public static class Preview
{
	// BASSETS_STATS=1 prints frames drawn and GPU time per second.
	private static void LogStats(Main main)
	{
		var rid = main.GetViewport().GetViewportRid();
		var renders = 0;
		var lastRenders = 0;
		RenderingServer.FramePostDraw += () => renders++;
		RenderingServer.ViewportSetMeasureRenderTime(rid, true);
		var lastFrames = Engine.GetFramesDrawn();
		var lastProcess = Engine.GetProcessFrames();
		var timer = new Timer { WaitTime = 1.0, Autostart = true };
		timer.Timeout += () =>
		{
			var frames = Engine.GetFramesDrawn();
			var process = Engine.GetProcessFrames();
			GD.Print($"STATS process/s={process - lastProcess} drawn/s={frames - lastFrames} rendered/s={renders - lastRenders} gpu_ms={RenderingServer.ViewportGetMeasuredRenderTimeGpu(rid):0.00} cpu_ms={RenderingServer.ViewportGetMeasuredRenderTimeCpu(rid):0.00}");
			lastFrames = frames;
			lastProcess = process;
			lastRenders = renders;
		};
		main.AddChild(timer);
	}

	public static void Run(Main main)
	{
		var path = System.Environment.GetEnvironmentVariable("BASSETS_SHOT");
		if (string.IsNullOrEmpty(path)) return;

		if (System.Environment.GetEnvironmentVariable("BASSETS_STATS") == "1")
			LogStats(main);

		var screen = System.Environment.GetEnvironmentVariable("BASSETS_SCREEN") ?? "users";
		var delay = double.TryParse(System.Environment.GetEnvironmentVariable("BASSETS_SHOT_DELAY"), out var d) ? d : 3.0;
		var tree = main.GetTree();

		if (screen is "apps" or "menu")
		{
			tree.CreateTimer(delay * 0.35).Timeout += () =>
				main.UserList.FindChildren("*", "Button", true, false).OfType<Profile>().FirstOrDefault()
					?.EmitSignal(BaseButton.SignalName.Pressed);
		}

		tree.CreateTimer(delay * 0.75).Timeout += () =>
		{
			switch (screen)
			{
				case "menu":
					main.AppList.FindChildren("*", "MarginContainer", true, false).OfType<App>().FirstOrDefault()
						?.AppButton.EmitSignal(BaseButton.SignalName.Pressed);
					break;
				case "dialog":
					_ = QuestionDialogue.OpenDialogue("Failed connecting",
						"Failed connecting to the Heeler API: socket not found. Check that Heeler is running and try again.",
						new Dictionary<string, bool> { { "Retry", true }, { "Quit", false } });
					break;
				case "pin":
					_ = PinInput.RequestPin();
					break;
			}
		};

		if (System.Environment.GetEnvironmentVariable("BASSETS_SHOT_LATE") == "1")
		{
			tree.CreateTimer(delay + 5).Timeout += () =>
			{
				RenderingServer.ForceDraw();
				main.GetViewport().GetTexture().GetImage().SavePng(path.Replace(".png", "-late.png"));
				tree.Quit();
			};
		}

		// A window the compositor reports as hidden (e.g. behind other windows) stops drawing, which would leave the
		// capture showing a stale frame, so render one now.
		tree.CreateTimer(delay).Timeout += () =>
		{
			RenderingServer.ForceDraw();
			main.GetViewport().GetTexture().GetImage().SavePng(path);
			if (System.Environment.GetEnvironmentVariable("BASSETS_SHOT_LATE") != "1")
				tree.Quit();
		};
	}
}
