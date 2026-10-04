using Godot;
using Resources.WolfAPI;
using System;
using System.Linq;
using System.Threading.Tasks;
using Skerga.GodotNodeUtilGenerator;
using WolfUI.Misc;

namespace WolfUI;

[Tool, GlobalClass, SceneAutoConfigure(GenerateNewMethod = false)]
public partial class AppList : Control
{
    public event EventHandler<Resources.WolfAPI.Lobby>? LobbyCreatedEvent;
    public event EventHandler<string>? LobbyStoppedEvent;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			ThemeChanged += EditorMockupReady;
			EditorMockupReady();
			return;
		}
		
		if (Main.Singleton.controllerMap is not null)
		{
			Main.Singleton.controllerMap.UsedControllerChanged += OnControllerChanged;
		}

		AppScrollContainer.ScrollDeadzone = 12;
		AppScrollContainer.VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever;
		AppScrollContainer.Resized += ApplyLayout;

		VisibilityChanged += RebuildAppList;
		ThemeChanged += RebuildAppList;

		WolfApi.Singleton.LobbyCreatedEvent += OnLobbyStarted;
		WolfApi.Singleton.LobbyStoppedEvent += OnLobbyStopped;
	}

	public override void _ExitTree()
	{
		WolfApi.Singleton.LobbyCreatedEvent -= OnLobbyStarted;
		WolfApi.Singleton.LobbyStoppedEvent -= OnLobbyStopped;
	}
	
	private void OnControllerChanged(ControllerMap.ControllerType  controllerType)
	{
		if (!Visible) return;
		
		// Ensure at least one element is focused when switching to controller.
		var focus = Main.Singleton.GetViewport().GuiGetFocusOwner();
		if (focus is not null || Main.Singleton.TopLayer.GetChildCount() > 0) return;

		if (AppGrid.GetChildren().Select(n => n as App).FirstOrDefault(n => n is not null) is { } ctrl)
			ctrl.GrabFocus();	
		else
			Main.Singleton.OptionsButton.GrabFocus();

	}
	
	private async void RebuildAppList()
	{
		if (!Visible)
		{
			Main.Singleton.BackHint.Hide();
			return;
		}

		ApplyLayout();
		Main.Singleton.BackHint.Visible = true;
		await LoadAppList();

		var lobbies = await WolfApi.GetLobbies();
		lobbies.ForEach(l => OnLobbyStarted(this, l));

		if (AppGrid.GetChildren().Select(n => n as App).FirstOrDefault(n => n is not null) is { } ctrl)
			ctrl.GrabFocus();	
		else
			Main.Singleton.OptionsButton.GrabFocus();
	}

	private void OnLobbyStopped(object? caller, string lobbyId)
	{
		if (!Visible)
			return;

		LobbyStoppedEvent?.Invoke(this, lobbyId);
	}

	private void OnLobbyStarted(object? sender, Resources.WolfAPI.Lobby? lobby)
	{
		if (!Visible) return;

		if (lobby?.ProfileId != WolfApi.ActiveProfile.Id &&
		    lobby?.StartedByProfileId != WolfApi.ActiveProfile.Id) return;
		if (lobby is null)
			return;
		LobbyCreatedEvent?.Invoke(this, lobby);
	}

	public override void _Process(double delta)
	{
		if (!Visible) return;
		
		if (Engine.IsEditorHint())
		{
			return;
		}

		if (InputActions.IsActionJustPressed("ui_select") && Main.Singleton.UserList is Control userList)
		{
			userList.Visible = true;
			SoundEffects.PlayAcceptSound();
		}
	}

	private async Task LoadAppList()
	{
		Main.Singleton.OptionsButton.Visible = true;
		Main.Singleton.HeaderLabel.Text = "Loading...";
		
		
		foreach (var child in AppGrid.GetChildren())
		{
			child.QueueFree();
			AppGrid.RemoveChild(child);
		}
		
		var enumerator = (await WolfApi.GetApps(WolfApi.ActiveProfile))
			.Select((value, i) => (value, i));
		
		foreach (var vi in enumerator)
		{
			vi.value.Name = $"App {vi.i}";
			AddAppEntry(vi.value);
			CardMotion.FadeIn(vi.value, vi.i);
		}

		ApplyLayout();
		
		var firstChildren = AppGrid.GetChildren()[..AppGrid.Columns].OfType<App>();
		foreach(var child in firstChildren)
		{
			child.AppButton.FocusEntered += () =>
			{
				AppScrollContainer.ScrollVertical = 0;
			};
		};
		
		var remainder = AppGrid.GetChildCount() % AppGrid.Columns;
		var idx = AppGrid.GetChildCount() - (remainder == 0 ? AppGrid.Columns : remainder);
		var lastChildren = AppGrid.GetChildren()[idx..].OfType<App>();
		foreach(var child in lastChildren)
		{
			child.AppButton.FocusEntered += () =>
			{
				AppScrollContainer.ScrollVertical = (int)AppScrollContainer.GetChildren().Cast<Control>().First().Size.X;
			};
		};
		
		Main.Singleton.HeaderLabel.Text = "Select Application";
	}

	private void EditorMockupReady()
	{
		
		ApplyLayout();
		foreach (var child in AppGrid.GetChildren())
			child.QueueFree();

		var scene = ResourceLoader.Load<PackedScene>("uid://chspw2lt1qcuc");
		for (var i = 0; i < 6; i++)
		{
			for (var j = 0; j < AppGrid.Columns; j++)
			{
				AppGrid.AddChild(scene.Instantiate());
			}
		}
	}

	private const float MinCardWidth = 250f;
	private const float MaxCardWidth = 360f;
	private const float CardGap = 28f;
	private const float EdgeMargin = 28f;
	private const float CardFrame = 24f;
	private const float CardFooter = 76f;
	private const float CellPadding = 28f; // the two spacers around each AppButton in App.tscn plus the HBox separation

	// Fits as many columns as the width allows and sizes the cards to fill them, so the grid works from phones to 4K.
	private void ApplyLayout()
	{
		var width = Mathf.Max(AppScrollContainer.Size.X - 2f * EdgeMargin, MinCardWidth);
		var columns = Mathf.Clamp(Mathf.FloorToInt((width + CardGap) / (MinCardWidth + CellPadding + CardGap)), 1, 8);
		var cellWidth = (width - CardGap * (columns - 1)) / columns;
		var cardWidth = Mathf.Min(cellWidth - CellPadding, MaxCardWidth);
		var cardHeight = (cardWidth - CardFrame) * 1.5f + CardFooter;

		AppGrid.Columns = columns;
		AppGrid.AddThemeConstantOverride("h_separation", (int)CardGap);
		AppGrid.AddThemeConstantOverride("v_separation", (int)CardGap);

		foreach (var app in AppGrid.GetChildren().OfType<App>())
			app.AppButton.CustomMinimumSize = new Vector2(cardWidth, cardHeight);
	}

	private static Control BuildSpacer()
	{
		return new Control()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
	}

	private void AddAppEntry(App newApp)
	{
		if (AppGrid is not { } gridContainer) return;
		var appEntryCount = gridContainer.GetChildCount();
		var gridColumns = gridContainer.Columns;

		gridContainer.AddChild(newApp);

		if (appEntryCount < gridColumns) return;
		var aboveApp = gridContainer.GetChild<App>(appEntryCount - gridColumns);
		newApp.FocusNeighborTop = aboveApp.GetPath();

		if (appEntryCount % gridColumns != 0) return;
		var app = gridContainer.GetChild<App>(appEntryCount - 1);
		app.FocusNeighborRight = gridContainer.GetChild<App>(-1).GetPath();
		gridContainer.GetChild<App>(-1).FocusNeighborLeft = app.GetPath();
	}
}
