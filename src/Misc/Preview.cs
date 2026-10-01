using System.Linq;
using Godot;

namespace WolfUI;

// Dev helper: BASSETS_SHOT=/path.png [BASSETS_SCREEN=users|apps] [BASSETS_SHOT_DELAY=seconds] saves a screenshot and quits.
public static class Preview
{
	public static void Run(Main main)
	{
		var path = System.Environment.GetEnvironmentVariable("BASSETS_SHOT");
		if (string.IsNullOrEmpty(path)) return;

		var screen = System.Environment.GetEnvironmentVariable("BASSETS_SCREEN") ?? "users";
		var delay = double.TryParse(System.Environment.GetEnvironmentVariable("BASSETS_SHOT_DELAY"), out var d) ? d : 3.0;
		var tree = main.GetTree();

		if (screen == "apps")
		{
			tree.CreateTimer(delay / 2).Timeout += () =>
			{
				var profile = main.UserList.FindChildren("*", "Button", true, false).OfType<Profile>().FirstOrDefault();
				profile?.EmitSignal(BaseButton.SignalName.Pressed);
			};
		}

		tree.CreateTimer(delay).Timeout += () =>
		{
			main.GetViewport().GetTexture().GetImage().SavePng(path);
			tree.Quit();
		};
	}
}
