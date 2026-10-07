using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Resources.WolfAPI;

namespace WolfUI;

// The cross-media-bar layout (WOLF_UI_LAYOUT=xmb): categories across, items down, the selected item enlarged where
// the two meet. Everything is sized from one unit, U = min(width / 16, height / 9) (width / 11 on portrait screens),
// so the same layout works from a phone to 4K and ultrawide.
public partial class Xmb : Control
{
	public static bool Enabled =>
		(System.Environment.GetEnvironmentVariable("WOLF_UI_LAYOUT") ?? "classic").Trim().ToLowerInvariant() == "xmb";

	public static Xmb? Instance { get; private set; }

	private enum AppStatus { Checking, Ready, Missing, Downloading, Running }

	private sealed class Item
	{
		public string Title = "";
		public string Subtitle = "";
		public XmbGlyph Glyph = XmbGlyph.App;
		public Texture2D? Texture;
		public Func<Task>? Activate;
		public Profile? Profile;
		public App? App;
		public Resources.WolfAPI.Lobby? Lobby;
		public AppStatus Status = AppStatus.Checking;
		public double Progress = -1;
		public bool Busy;
		public XmbItemView? View;
	}

	private sealed class Category
	{
		public required string Name;
		public required XmbGlyph Glyph;
		public required XmbPalette Palette;
		public List<Item> Items = [];
		public int Selected;
		public XmbCategoryView View = null!;
		public Control Layer = null!;
	}

	private const int ProfilesCat = 0, GamesCat = 1, CoopCat = 2, SettingsCat = 3;

	private readonly List<Category> _cats = [];
	private int _cat;
	private float _u;
	private bool _narrow;
	private bool _settled = true;

	private XmbBackdrop _backdrop = null!;
	private XmbSounds _sounds = null!;
	private Label _brand = null!, _status = null!, _hints = null!, _toast = null!;
	private Tween? _toastTween;
	private Control _popups = null!;

	// Options panel (Y / Tab / right click on a game)
	private PanelContainer _options = null!;
	private VBoxContainer _optionList = null!;
	private readonly List<(string Label, Func<Task> Run)> _optionItems = [];
	private int _optionSel;

	private Vector2 _drag;
	private bool _dragged;
	private float _stickX, _stickY;

	public override void _Ready()
	{
		Instance = this;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		Resized += () => Layout(true);
		MouseFilter = MouseFilterEnum.Stop;
		FocusMode = FocusModeEnum.All;

		_backdrop = new XmbBackdrop();
		AddChild(_backdrop);
		_sounds = new XmbSounds();
		AddChild(_sounds);

		_cats.Add(new Category { Name = "Profiles", Glyph = XmbGlyph.Profile,
			Palette = new(new Color(0.55f, 0.4f, 1.0f), new Color(0.3f, 0.7f, 1.0f)) });
		_cats.Add(new Category { Name = "Games", Glyph = XmbGlyph.Games,
			Palette = new(new Color(0.25f, 0.45f, 1.0f), new Color(0.1f, 0.8f, 0.9f)) });
		_cats.Add(new Category { Name = "Co-op", Glyph = XmbGlyph.Coop,
			Palette = new(new Color(1.0f, 0.5f, 0.2f), new Color(1.0f, 0.3f, 0.5f)) });
		_cats.Add(new Category { Name = "Settings", Glyph = XmbGlyph.Settings,
			Palette = new(new Color(0.6f, 0.65f, 0.8f), new Color(0.3f, 0.5f, 0.9f)) });

		foreach (var cat in _cats)
		{
			cat.Layer = new Control { MouseFilter = MouseFilterEnum.Ignore };
			AddChild(cat.Layer);
			cat.View = new XmbCategoryView().Setup(cat.Name, cat.Glyph);
			AddChild(cat.View);
		}

		_brand = AddLabel("HEELER");
		_brand.Modulate = new Color(1, 1, 1, 0.85f);
		_status = AddLabel("");
		_status.HorizontalAlignment = HorizontalAlignment.Right;
		_hints = AddLabel("");
		_hints.Modulate = new Color(1, 1, 1, 0.7f);
		_toast = AddLabel("");
		_toast.HorizontalAlignment = HorizontalAlignment.Center;
		_toast.Modulate = new Color(1, 1, 1, 0);
		BuildOptionsPanel();

		_popups = Main.Singleton.GetNode<Control>("PopupViewportContainer");
		if (Main.Singleton.controllerMap is { } map)
			map.UsedControllerChanged += _ => UpdateHints();
		UpdateHints();

		var clock = new Timer { WaitTime = 5, Autostart = true };
		clock.Timeout += UpdateStatus;
		AddChild(clock);

		WolfApi.Singleton.LobbyCreatedEvent += OnLobbiesChanged;
		WolfApi.Singleton.LobbyStoppedEvent += OnLobbyStopped;
		WolfApi.Singleton.ImagePullProgress += OnPullProgress;
		WolfApi.Singleton.ImageUpdated += OnPullDone;
		WolfApi.Singleton.ImageAlreadyUptoDate += OnPullDone;
		TreeExiting += () =>
		{
			WolfApi.Singleton.LobbyCreatedEvent -= OnLobbiesChanged;
			WolfApi.Singleton.LobbyStoppedEvent -= OnLobbyStopped;
			WolfApi.Singleton.ImagePullProgress -= OnPullProgress;
			WolfApi.Singleton.ImageUpdated -= OnPullDone;
			WolfApi.Singleton.ImageAlreadyUptoDate -= OnPullDone;
		};

		BuildSettings();
		SetItems(_cats[GamesCat], [Note("Choose a profile first", "Profiles is on the left", XmbGlyph.Info, () => Go(ProfilesCat))]);
		SetItems(_cats[CoopCat], []);
		_cat = ProfilesCat;
		Callable.From(() => Layout(true)).CallDeferred();
		GrabFocus();
		_ = LoadProfiles();
	}

	private Label AddLabel(string text)
	{
		var label = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore };
		AddChild(label);
		return label;
	}

	// ----- data -----

	private async Task LoadProfiles()
	{
		var profiles = await WolfApi.GetProfiles();
		var items = profiles.Select(p => new Item
		{
			Title = p.ProfileName ?? "Profile",
			Subtitle = p.Pin is not null ? "Locked with a PIN" : "",
			Glyph = XmbGlyph.Profile,
			Texture = GD.Load<Texture2D>("res://Icons/default_profile_icon.png"),
			Profile = p
		}).ToList();
		foreach (var item in items)
			item.Activate = () => SelectProfile(item);
		SetItems(_cats[ProfilesCat], items);
		Layout(false);

		foreach (var item in items.Where(i => !string.IsNullOrEmpty(i.Profile!.IconPngPath)))
			SetTexture(item, await WolfApi.GetIcon(item.Profile!.IconPngPath!));

		// One profile without a PIN: nothing to choose, go straight to the games
		if (items.Count == 1 && items[0].Profile!.Pin is null)
			await SelectProfile(items[0], quiet: true);
	}

	private async Task SelectProfile(Item item, bool quiet = false)
	{
		var profile = item.Profile!;
		if (profile.Pin is not null)
		{
			var pin = await PinInput.RequestPin();
			if (!pin.SequenceEqual(profile.Pin))
			{
				_sounds.Back();
				Toast("Wrong PIN");
				return;
			}
		}

		WolfApi.ActiveProfile = profile;
		foreach (var p in _cats[ProfilesCat].Items)
			p.Subtitle = p == item ? "Signed in" : p.Profile!.Pin is not null ? "Locked with a PIN" : "";
		RefreshItems(_cats[ProfilesCat]);
		UpdateStatus();

		await LoadApps();
		if (!quiet)
		{
			Go(GamesCat);
			return;
		}
		_cat = GamesCat;
		Layout(true);
	}

	private async Task LoadApps()
	{
		var apps = await WolfApi.GetApps(WolfApi.ActiveProfile);
		var items = apps.Select(a => new Item { Title = a.Title ?? "App", App = a, Glyph = XmbGlyph.App }).ToList();
		foreach (var item in items)
			item.Activate = () => ActivateApp(item);
		if (items.Count == 0)
			items.Add(Note("No games yet", "Add apps to this profile in Heeler's config", XmbGlyph.Info, null));
		SetItems(_cats[GamesCat], items);
		Layout(false);

		await RefreshLobbies();
		foreach (var item in items.Where(i => i.App is not null))
			_ = CheckImage(item);
		foreach (var item in items.Where(i => i.App is not null))
			SetTexture(item, await WolfApi.GetIcon(item.App!));
	}

	private async Task CheckImage(Item item)
	{
		if (item.App?.Runner?.Image is not { } image)
		{
			SetStatus(item, AppStatus.Ready);
			return;
		}
		var onDisk = await WolfApi.IsImageOnDisk(image);
		if (item.Status is AppStatus.Checking)
			SetStatus(item, onDisk ? AppStatus.Ready : AppStatus.Missing);
		else if (item.Status is AppStatus.Ready or AppStatus.Missing)
			SetStatus(item, onDisk ? AppStatus.Ready : AppStatus.Missing);
	}

	private async Task RefreshLobbies()
	{
		var lobbies = await WolfApi.GetLobbies();

		foreach (var item in _cats[GamesCat].Items.Where(i => i.App is not null))
		{
			item.Lobby = lobbies.FirstOrDefault(l => AppLauncher.IsRunning(item.App!, l));
			if (item.Lobby is not null)
				SetStatus(item, AppStatus.Running);
			else if (item.Status is AppStatus.Running)
				SetStatus(item, AppStatus.Ready);
		}

		var coop = lobbies.Where(l => l.MultiUser).Select(l => new Item
		{
			Title = l.Name ?? "Co-op game",
			Subtitle = $"{l.ConnectedSessions?.Count ?? 0} playing" + (l.IsPinLocked ? " · PIN" : ""),
			Glyph = XmbGlyph.Join,
			Lobby = l
		}).ToList();
		foreach (var item in coop)
			item.Activate = () => JoinCoop(item);
		if (coop.Count == 0)
			coop.Add(Note("No co-op games running", "Host one: pick a game, open its options, choose Co-op", XmbGlyph.Info, null));
		SetItems(_cats[CoopCat], coop);
		Layout(false);

		foreach (var item in coop.Where(i => !string.IsNullOrEmpty(i.Lobby?.IconPngPath)))
			SetTexture(item, await WolfApi.GetIcon(item.Lobby!.IconPngPath!));
	}

	private void OnLobbiesChanged(object? sender, Resources.WolfAPI.Lobby lobby) => _ = RefreshLobbies();
	private void OnLobbyStopped(object? sender, string lobbyId) => _ = RefreshLobbies();

	private void OnPullProgress(string image, double percent)
	{
		foreach (var item in AppItems(image))
		{
			item.Progress = percent / 100.0;
			SetStatus(item, AppStatus.Downloading, force: true);
		}
	}

	private void OnPullDone(string image)
	{
		var any = false;
		foreach (var item in AppItems(image))
		{
			any |= item.Status == AppStatus.Downloading;
			item.Progress = -1;
			SetStatus(item, item.Lobby is not null ? AppStatus.Running : AppStatus.Ready);
		}
		if (!any) return;
		_sounds.Done();
		Toast("Download finished");
	}

	private IEnumerable<Item> AppItems(string image) =>
		_cats[GamesCat].Items.Where(i => i.App?.Runner?.Image == image);

	private void SetStatus(Item item, AppStatus status, bool force = false)
	{
		if (item.Status == status && !force) return;
		item.Status = status;
		item.Subtitle = status switch
		{
			AppStatus.Ready => "Ready",
			AppStatus.Missing => "Not downloaded yet · select to download",
			AppStatus.Downloading => item.Progress >= 0 ? $"Downloading {item.Progress * 100:0}%" : "Downloading",
			AppStatus.Running => "Running · select to return to it",
			_ => ""
		};
		if (item.View is null) return;
		item.View.Subtitle.Text = item.Subtitle;
		item.View.SetProgress(status == AppStatus.Downloading ? Math.Max(0, item.Progress) : -1, Accent);
	}

	private void SetTexture(Item item, Texture2D? texture)
	{
		if (texture is null) return;
		item.Texture = texture;
		if (item.View is not null)
			item.View.Icon.Texture = texture;
	}

	private static Item Note(string title, string subtitle, XmbGlyph glyph, Action? action) => new()
	{
		Title = title,
		Subtitle = subtitle,
		Glyph = glyph,
		Status = AppStatus.Ready,
		Activate = action is null ? null : () =>
		{
			action();
			return Task.CompletedTask;
		}
	};

	// ----- actions -----

	private async Task ActivateApp(Item item)
	{
		switch (item.Status)
		{
			case AppStatus.Downloading:
				Toast("Still downloading");
				return;
			case AppStatus.Missing:
				Pull(item);
				return;
		}
		await Start(item);
	}

	private void Pull(Item item)
	{
		if (item.App?.Runner?.Image is not { } image) return;
		item.Progress = 0;
		SetStatus(item, AppStatus.Downloading, force: true);
		WolfApi.PullImage(image);
	}

	private async Task Start(Item item, bool coop = false)
	{
		var app = item.App!;
		var lobby = coop ? null : item.Lobby;
		var lobbyId = lobby?.Id;
		List<int>? pin = null;

		if (lobby is null)
		{
			lobby = await AppLauncher.NewLobby(app, coop);
			if (lobby is null)
			{
				Toast("Couldn't read this streaming session");
				return;
			}

			if (coop && await QuestionDialogue.OpenDialogue("Co-op", "Protect the lobby with a PIN?",
				    new Dictionary<string, bool> { { "Yes", true }, { "No", false } }))
			{
				pin = await PinInput.RequestPin();
				lobby.Pin = pin;
			}

			lobbyId = await WolfApi.CreateLobby(lobby);
		}

		if (lobbyId is null)
		{
			Toast($"Couldn't start {item.Title}");
			return;
		}

		Toast($"Starting {item.Title}");
		var response = await WolfApi.JoinLobby(lobbyId, WolfApi.SessionId, pin);
		if (response?.Success == false)
			Toast("That lobby is full");
	}

	private async Task Stop(Item item)
	{
		if (item.Lobby?.Id is not { } id) return;
		await WolfApi.StopLobby(id);
		item.Lobby = null;
		SetStatus(item, AppStatus.Ready);
		Toast($"Stopped {item.Title}");
	}

	private async Task JoinCoop(Item item)
	{
		var lobby = item.Lobby!;
		List<int>? pin = null;
		if (lobby.IsPinLocked)
			pin = await PinInput.RequestPin();

		var response = await WolfApi.JoinLobby(lobby.Id!, WolfApi.SessionId, pin);
		if (response?.Success == false)
			Toast(lobby.IsPinLocked ? "Wrong PIN, or the lobby is full" : "That lobby is full");
	}

	// ----- settings -----

	private void BuildSettings()
	{
		var theme = new Item { Title = "Theme", Glyph = XmbGlyph.Theme, Status = AppStatus.Ready };
		theme.Activate = () =>
		{
			XmbStyle.ThemeIndex = (XmbStyle.ThemeIndex + 1) % XmbStyle.Themes.Length;
			_sounds.Theme();
			ShowSettings();
			Layout(false);
			return Task.CompletedTask;
		};

		var sound = new Item { Title = "Sound effects", Glyph = XmbGlyph.Sound, Status = AppStatus.Ready };
		sound.Activate = () =>
		{
			XmbStyle.SoundOn = !XmbStyle.SoundOn;
			_sounds.Accept();
			ShowSettings();
			return Task.CompletedTask;
		};

		var effects = new Item { Title = "Effects", Glyph = XmbGlyph.Effects, Status = AppStatus.Ready };
		effects.Activate = () =>
		{
			Effects.Set(Effects.IsFull ? EffectsLevel.Reduced : EffectsLevel.Full);
			ShowSettings();
			return Task.CompletedTask;
		};

		var exit = new Item { Title = "Exit", Subtitle = "Close the launcher", Glyph = XmbGlyph.Exit, Status = AppStatus.Ready };
		exit.Activate = () =>
		{
			Engine.PrintErrorMessages = false;
			if ((System.Environment.GetEnvironmentVariable("WOLF_UI_AUTOUPDATE") ?? "False") == "True")
				WolfApi.StopSession(WolfApi.SessionId);
			GetTree().Quit();
			return Task.CompletedTask;
		};

		SetItems(_cats[SettingsCat], [theme, sound, effects, exit]);
		ShowSettings();
	}

	private void ShowSettings()
	{
		var items = _cats[SettingsCat].Items;
		items[0].Subtitle = XmbStyle.Themes[XmbStyle.ThemeIndex].Name;
		items[1].Subtitle = XmbStyle.SoundOn ? "On" : "Off";
		items[2].Subtitle = Effects.IsFull
			? "Full: animated glass"
			: "Reduced: still backdrop, lighter on the GPU and the stream";
		RefreshItems(_cats[SettingsCat]);
	}

	// ----- views -----

	private void SetItems(Category cat, List<Item> items)
	{
		var selectedTitle = cat.Items.Count > 0 ? cat.Items[Math.Min(cat.Selected, cat.Items.Count - 1)].Title : null;
		foreach (var old in cat.Items)
			old.View?.QueueFree();

		cat.Items = items;
		cat.Selected = Math.Max(0, items.FindIndex(i => i.Title == selectedTitle));
		foreach (var item in items)
		{
			item.View = new XmbItemView();
			cat.Layer.AddChild(item.View);
		}
		RefreshItems(cat);
		Layout(true, cat);
	}

	private void RefreshItems(Category cat)
	{
		foreach (var item in cat.Items)
		{
			if (item.View is not { } view) continue;
			view.Title.Text = item.Title;
			view.Subtitle.Text = item.Subtitle;
			view.Icon.Glyph = item.Glyph;
			view.Icon.Texture = item.Texture;
			view.SetProgress(item.Status == AppStatus.Downloading ? Math.Max(0, item.Progress) : -1, Accent);
		}
	}

	private XmbPalette Palette => XmbStyle.Themes[XmbStyle.ThemeIndex].Palette ?? _cats[_cat].Palette;

	private Color Accent
	{
		get
		{
			var a = Palette.A;
			return new Color(Mathf.Min(1, a.R * 1.15f), Mathf.Min(1, a.G * 1.15f), Mathf.Min(1, a.B * 1.15f));
		}
	}

	// instant: snap (first paint, resize, or a category whose items were just rebuilt while off screen)
	private void Layout(bool instant, Category? only = null)
	{
		var w = Size.X;
		var h = Size.Y;
		if (w < 2 || h < 2) return;

		_narrow = w / h < 0.8f;
		_u = _narrow ? w / 11f : Mathf.Min(w / 16f, h / 9f);
		var u = _u;
		var selX = Mathf.Max(2.1f * u, w * (_narrow ? 0.2f : 0.16f));
		var catY = h * (_narrow ? 0.24f : 0.25f);
		var textWidth = Mathf.Max(u * 3, (w - selX - u * 1.2f) / 1.3f - u * 1.4f);
		var accent = Accent;

		for (var ci = 0; ci < _cats.Count; ci++)
		{
			var cat = _cats[ci];
			var d = ci - _cat;
			var on = d == 0;
			if (only is null || only == cat)
			{
				var x = selX + d * 2.3f * u + Math.Sign(d) * 0.55f * u;
				cat.View.Resize(u);
				cat.View.Label.Visible = on;
				cat.View.Target(new Vector2(x, catY), on ? 1.3f : 0.9f, on ? 1f : Mathf.Max(0, 0.55f - Math.Abs(d) * 0.1f), instant);
			}
			if (only is not null && only != cat) continue;

			for (var ii = 0; ii < cat.Items.Count; ii++)
			{
				var view = cat.Items[ii].View!;
				var rel = ii - cat.Selected;
				float y, a, s;
				if (!on) { y = catY + 2.1f * u; a = 0; s = 0.8f; }
				else if (rel == 0) { y = catY + 2.15f * u; a = 1; s = 1.3f; }
				else if (rel > 0) { y = catY + 2.6f * u + rel * u; a = Mathf.Max(0, 0.85f - (rel - 1) * 0.22f); s = 0.9f; }
				else { y = catY - 1.2f * u + (rel + 1) * u; a = rel == -1 ? 0.55f : rel == -2 ? 0.2f : 0; s = 0.9f; }
				if (y > h - 1.5f * u || y < 0.9f * u) a = 0; // clear of the hint bar and the top bar

				view.Resize(u, textWidth);
				view.Subtitle.Visible = on && rel == 0;
				view.Icon.Selected = on && rel == 0;
				view.Icon.Accent = accent;
				view.Target(new Vector2(selX, y), s, a, instant);
			}
		}

		var font = Mathf.Max(11, (int)(u * 0.26f));
		foreach (var label in new[] { _brand, _status, _hints, _toast })
			label.AddThemeFontSizeOverride("font_size", font);
		_brand.Position = new Vector2(u * 0.5f, u * 0.3f);
		_brand.Size = new Vector2(w * 0.4f, u * 0.5f);
		_status.Position = new Vector2(w * 0.5f, u * 0.3f);
		_status.Size = new Vector2(w * 0.5f - u * 0.5f, u * 0.5f);
		_hints.Position = new Vector2(u * 0.5f, h - u * 0.75f);
		_hints.Size = new Vector2(w - u, u * 0.5f);
		_toast.Position = new Vector2(0, h - u * 1.45f);
		_toast.Size = new Vector2(w, u * 0.5f);
		LayoutOptions();

		_backdrop.SetPalette(Palette, instant);
		_settled = false;
		UpdateStatus();
	}

	public override void _Process(double delta)
	{
		PollStick(delta);
		if (_settled) return;
		var k = 1f - Mathf.Exp(-(float)delta * 14f);
		var settled = true;
		foreach (var cat in _cats)
		{
			settled &= cat.View.Step(k);
			foreach (var item in cat.Items)
				settled &= item.View!.Step(k);
		}
		_settled = settled;
	}

	private void UpdateStatus()
	{
		var who = WolfApi.ActiveProfile?.ProfileName;
		var time = DateTime.Now.ToString("HH:mm");
		_status.Text = who is null ? time : $"{who}   {time}";
	}

	private void UpdateHints()
	{
		var type = Main.Singleton.controllerMap?.Controller ?? ControllerMap.ControllerType.None;
		_hints.Text = type switch
		{
			ControllerMap.ControllerType.PS => "Cross  Select      Circle  Back      Triangle  Options",
			ControllerMap.ControllerType.Switch => "B  Select      A  Back      X  Options",
			ControllerMap.ControllerType.XBox => "A  Select      B  Back      Y  Options",
			_ => "Enter  Select      Esc  Back      Tab  Options      Arrows  Move"
		};
	}

	private void Toast(string text)
	{
		_toast.Text = text;
		_toastTween?.Kill();
		_toastTween = CreateTween();
		_toastTween.TweenProperty(_toast, "modulate:a", 1f, 0.15f);
		_toastTween.TweenInterval(2.4f);
		_toastTween.TweenProperty(_toast, "modulate:a", 0f, 0.4f);
	}

	// ----- navigation -----

	private void Go(int cat)
	{
		cat = Math.Clamp(cat, 0, _cats.Count - 1);
		if (cat == _cat) return;
		_cat = cat;
		_sounds.Category();
		Layout(false);
	}

	private void Move(int step)
	{
		var cat = _cats[_cat];
		var sel = Math.Clamp(cat.Selected + step, 0, Math.Max(0, cat.Items.Count - 1));
		if (sel == cat.Selected) return;
		cat.Selected = sel;
		_sounds.Item();
		Layout(false);
	}

	private async void Activate()
	{
		var cat = _cats[_cat];
		if (cat.Items.Count == 0) return;
		var item = cat.Items[cat.Selected];
		if (item.Activate is null || item.Busy) return;
		if (cat != _cats[SettingsCat]) _sounds.Accept();
		item.Busy = true;
		try
		{
			await item.Activate();
		}
		catch (Exception e)
		{
			GD.PrintErr($"XMB: {item.Title}: {e.Message}");
			Toast($"Something went wrong: {e.Message}");
		}
		finally
		{
			item.Busy = false;
			GrabFocus();
		}
	}

	private void Back()
	{
		if (_options.Visible)
		{
			CloseOptions();
			return;
		}
		var cat = _cats[_cat];
		if (cat.Selected > 0)
		{
			cat.Selected = 0;
			_sounds.Back();
			Layout(false);
		}
		else if (_cat != GamesCat && WolfApi.ActiveProfile is not null)
		{
			_cat = GamesCat;
			_sounds.Back();
			Layout(false);
		}
	}

	private bool Blocked => _popups.Visible;

	public override void _GuiInput(InputEvent e)
	{
		if (Blocked) return;

		if (e is InputEventKey { Pressed: true } key)
		{
			switch (key.Keycode)
			{
				case Key.Tab or Key.Menu:
					OpenOptions();
					AcceptEvent();
					return;
				case Key.Backspace:
					Back();
					AcceptEvent();
					return;
			}
		}

		if (e is InputEventJoypadButton { Pressed: true, ButtonIndex: JoyButton.Y })
		{
			OpenOptions();
			AcceptEvent();
			return;
		}

		if (_options.Visible)
		{
			if (e.IsActionPressed("ui_up", true)) MoveOption(-1);
			else if (e.IsActionPressed("ui_down", true)) MoveOption(1);
			else if (e.IsActionPressed("ui_accept")) RunOption();
			else if (e.IsActionPressed("ui_cancel") || e.IsActionPressed("ui_left")) CloseOptions();
			else return;
			AcceptEvent();
			return;
		}

		if (e.IsActionPressed("ui_left", true)) Go(_cat - 1);
		else if (e.IsActionPressed("ui_right", true)) Go(_cat + 1);
		else if (e.IsActionPressed("ui_up", true)) Move(-1);
		else if (e.IsActionPressed("ui_down", true)) Move(1);
		else if (e.IsActionPressed("ui_accept")) Activate();
		else if (e.IsActionPressed("ui_cancel")) Back();
		else if (HandlePointer(e)) { }
		else return;
		AcceptEvent();
	}

	// The left stick, with a repeat while it is held (the d-pad and keys repeat through the ui_* actions)
	private double _stickHeld;

	private void PollStick(double delta)
	{
		if (Blocked || !HasFocus()) return;
		var x = Input.GetJoyAxis(0, JoyAxis.LeftX);
		var y = Input.GetJoyAxis(0, JoyAxis.LeftY);
		var dx = Mathf.Abs(x) > 0.6f ? Math.Sign(x) : 0;
		var dy = Mathf.Abs(y) > 0.6f && dx == 0 ? Math.Sign(y) : 0;
		if (dx == 0 && dy == 0)
		{
			_stickX = _stickY = 0;
			return;
		}
		var fresh = dx != _stickX || dy != _stickY;
		_stickX = dx;
		_stickY = dy;
		_stickHeld = fresh ? -0.35 : _stickHeld + delta;
		if (!fresh && _stickHeld < 0.11) return;
		if (!fresh) _stickHeld = 0;

		if (_options.Visible)
		{
			if (dy != 0) MoveOption(dy);
		}
		else if (dx != 0) Go(_cat + dx);
		else Move(dy);
	}

	private bool HandlePointer(InputEvent e)
	{
		switch (e)
		{
			case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true }:
				Move(-1);
				return true;
			case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true }:
				Move(1);
				return true;
			case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
				OpenOptions();
				return true;
			case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }:
				_drag = Vector2.Zero;
				_dragged = false;
				return true;
			case InputEventMouseMotion motion when (motion.ButtonMask & MouseButtonMask.Left) != 0:
				// Swipe: across changes category, up / down moves through the items
				_drag += motion.Relative;
				if (Mathf.Abs(_drag.X) > _u * 0.9f && Mathf.Abs(_drag.X) > Mathf.Abs(_drag.Y))
				{
					Go(_cat - Math.Sign(_drag.X));
					_drag = Vector2.Zero;
					_dragged = true;
				}
				else if (Mathf.Abs(_drag.Y) > _u * 0.8f)
				{
					Move(-Math.Sign(_drag.Y));
					_drag = Vector2.Zero;
					_dragged = true;
				}
				return true;
			case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } click:
				if (!_dragged)
					Tap(click.Position);
				return true;
		}
		return false;
	}

	private void Tap(Vector2 at)
	{
		for (var ci = 0; ci < _cats.Count; ci++)
		{
			var view = _cats[ci].View;
			if (view.TargetAlpha > 0.05f && at.DistanceTo(view.Centre) < _u * 0.75f * view.TargetScale)
			{
				Go(ci);
				return;
			}
		}

		var cat = _cats[_cat];
		for (var ii = 0; ii < cat.Items.Count; ii++)
		{
			var view = cat.Items[ii].View!;
			if (view.TargetAlpha < 0.1f) continue;
			var s = view.TargetScale;
			var rect = new Rect2(view.Centre - new Vector2(_u, _u) * 0.5f * s, new Vector2(_u * 1.2f + view.Title.Size.X, _u) * s);
			if (!rect.HasPoint(at)) continue;
			if (ii == cat.Selected)
				Activate();
			else
				Move(ii - cat.Selected);
			return;
		}
	}

	// ----- options panel -----

	private void BuildOptionsPanel()
	{
		_options = new PanelContainer { Visible = false, MouseFilter = MouseFilterEnum.Stop };
		var style = new StyleBoxFlat
		{
			BgColor = new Color(0.02f, 0.03f, 0.08f, 0.82f),
			BorderColor = new Color(1, 1, 1, 0.18f),
			BorderWidthLeft = 2,
			ShadowColor = new Color(0, 0, 0, 0.4f),
			ShadowSize = 24
		};
		_options.AddThemeStyleboxOverride("panel", style);
		_optionList = new VBoxContainer();
		_options.AddChild(_optionList);
		AddChild(_options);
	}

	private void OpenOptions()
	{
		var cat = _cats[_cat];
		if (cat != _cats[GamesCat] || cat.Items.Count == 0) return;
		var item = cat.Items[cat.Selected];
		if (item.App is null) return;

		_optionItems.Clear();
		switch (item.Status)
		{
			case AppStatus.Running:
				_optionItems.Add(("Return to game", () => Start(item)));
				_optionItems.Add(("Stop", () => Stop(item)));
				break;
			case AppStatus.Missing:
				_optionItems.Add(("Download", () => { Pull(item); return Task.CompletedTask; }));
				break;
			case AppStatus.Ready or AppStatus.Checking:
				_optionItems.Add(("Start", () => Start(item)));
				_optionItems.Add(("Co-op: host a lobby", () => Start(item, coop: true)));
				break;
		}
		if (item.Status is AppStatus.Ready or AppStatus.Checking or AppStatus.Running)
			_optionItems.Add(("Check for update", () => { Pull(item); return Task.CompletedTask; }));
		_optionItems.Add(("Close", () => Task.CompletedTask));

		foreach (var child in _optionList.GetChildren())
			child.QueueFree();
		foreach (var (label, _) in _optionItems)
			_optionList.AddChild(new Label { Text = label, MouseFilter = MouseFilterEnum.Ignore });

		_optionSel = 0;
		_options.Visible = true;
		LayoutOptions();
		PaintOptions();
		_sounds.Accept();
	}

	private void LayoutOptions()
	{
		if (_options is null || _u <= 0) return;
		var w = Mathf.Max(_u * 4.5f, Size.X * (_narrow ? 0.7f : 0.3f));
		_options.Position = new Vector2(Size.X - w, 0);
		_options.Size = new Vector2(w, Size.Y);
		_optionList.AddThemeConstantOverride("separation", (int)(_u * 0.25f));
		var style = (StyleBoxFlat)_options.GetThemeStylebox("panel");
		style.ContentMarginLeft = _u * 0.7f;
		style.ContentMarginTop = Size.Y * 0.3f;
		foreach (var label in _optionList.GetChildren().OfType<Label>())
			label.AddThemeFontSizeOverride("font_size", Mathf.Max(12, (int)(_u * 0.32f)));
	}

	private void PaintOptions()
	{
		var labels = _optionList.GetChildren().OfType<Label>().ToList();
		for (var i = 0; i < labels.Count; i++)
			labels[i].Modulate = i == _optionSel ? Accent.Lightened(0.35f) : new Color(1, 1, 1, 0.6f);
	}

	private void MoveOption(int step)
	{
		var sel = Math.Clamp(_optionSel + step, 0, _optionItems.Count - 1);
		if (sel == _optionSel) return;
		_optionSel = sel;
		_sounds.Item();
		PaintOptions();
	}

	private async void RunOption()
	{
		var run = _optionItems[_optionSel].Run;
		CloseOptions(quiet: true);
		_sounds.Accept();
		try
		{
			await run();
		}
		catch (Exception e)
		{
			GD.PrintErr($"XMB option: {e.Message}");
			Toast($"Something went wrong: {e.Message}");
		}
		GrabFocus();
	}

	private void CloseOptions(bool quiet = false)
	{
		_options.Visible = false;
		if (!quiet) _sounds.Back();
	}

	// ----- dev preview (BASSETS_SCREEN=xmb, xmb-games, xmb-options, xmb-settings) -----

	public async void Preview(string screen)
	{
		if (screen != "xmb" && WolfApi.ActiveProfile is null && _cats[ProfilesCat].Items.FirstOrDefault() is { } first)
			await SelectProfile(first, quiet: true);
		switch (screen)
		{
			case "xmb-games":
				Go(GamesCat);
				break;
			case "xmb-options":
				Go(GamesCat);
				OpenOptions();
				break;
			case "xmb-settings":
				Go(SettingsCat);
				break;
			case "xmb-coop":
				Go(CoopCat);
				break;
		}
		Layout(true);
	}
}
