using System.Threading.Tasks;
using Godot;

namespace WolfUI;

// A small dialogue with a text box, built in code. Returns the trimmed text, or null when cancelled.
public static class NameInput
{
	public static async Task<string?> Request(string title, string prompt, bool allowCancel)
	{
		var focusOwner = Main.Singleton.GetViewport().GuiGetFocusOwner();
		var done = new TaskCompletionSource<string?>();

		var root = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Stop };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);

		var panel = new PanelContainer { CustomMinimumSize = new Vector2(640, 0) };
		var margin = new MarginContainer();
		foreach (var side in new[] { "left", "top", "right", "bottom" })
			margin.AddThemeConstantOverride($"margin_{side}", 28);
		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 16);

		var titleLabel = new Label { Text = title };
		titleLabel.AddThemeFontSizeOverride("font_size", 34);
		var promptLabel = new Label { Text = prompt, AutowrapMode = TextServer.AutowrapMode.WordSmart };
		var edit = new LineEdit { PlaceholderText = "Name", MaxLength = 24, CaretBlink = true };
		edit.AddThemeFontSizeOverride("font_size", 28);

		var buttons = new HBoxContainer();
		buttons.AddThemeConstantOverride("separation", 12);
		var ok = new Button { Text = "Create", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		buttons.AddChild(ok);
		if (allowCancel)
		{
			var cancel = new Button { Text = "Cancel", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
			cancel.Pressed += () => done.TrySetResult(null);
			buttons.AddChild(cancel);
		}

		void Accept()
		{
			var text = edit.Text.Trim();
			if (text.Length > 0) done.TrySetResult(text);
		}
		ok.Pressed += Accept;
		edit.TextSubmitted += _ => Accept();

		box.AddChild(titleLabel);
		box.AddChild(promptLabel);
		box.AddChild(edit);
		box.AddChild(buttons);
		margin.AddChild(box);
		panel.AddChild(margin);
		root.AddChild(panel);

		Main.Singleton.TopLayer.CallDeferred(Node.MethodName.AddChild, root);
		edit.CallDeferred(Control.MethodName.GrabFocus);
		root.Ready += () => CardMotion.PopIn(panel);

		var result = await done.Task;
		root.QueueFree();
		if (GodotObject.IsInstanceValid(focusOwner))
			focusOwner.GrabFocus();
		return result;
	}
}
