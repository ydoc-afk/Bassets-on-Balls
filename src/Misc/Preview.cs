using System.Collections.Generic;
using System.Linq;
using Godot;

namespace WolfUI;

// Dev helper: BASSETS_SHOT=/path.png [BASSETS_SCREEN=users|apps|menu|dialog|pin] [BASSETS_SHOT_DELAY=seconds]
// saves a screenshot after the given delay and quits.
public static class Preview
{
	public static void Run(Main main)
	{
		var path = System.Environment.GetEnvironmentVariable("BASSETS_SHOT");
		if (string.IsNullOrEmpty(path)) return;

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

		tree.CreateTimer(delay).Timeout += () =>
		{
			main.GetViewport().GetTexture().GetImage().SavePng(path);
			tree.Quit();
		};
	}
}
