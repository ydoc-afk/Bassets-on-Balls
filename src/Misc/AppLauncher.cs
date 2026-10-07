using System.Threading.Tasks;
using Godot;
using Resources.WolfAPI;

namespace WolfUI;

// Lobby building shared by the classic grid (App) and the XMB layout.
public static class AppLauncher
{
	public static string StateFolder(App app) => $"profile-data/{WolfApi.ActiveProfile.Id}/{app.Runner?.Name}";

	// The app's own icon, else the wildlife icon for a games-on-whales image.
	public static string? IconPath(App app)
	{
		if (app.IconPngPath is not null)
			return app.IconPngPath;
		if (app.Runner?.Image is null || !app.Runner.Image.Contains("ghcr.io/games-on-whales/"))
			return null;

		var name = app.Runner.Image.TrimPrefix("ghcr.io/games-on-whales/");
		var idx = name.LastIndexOf(':');
		if (idx >= 0)
			name = name[..idx];
		return $"https://games-on-whales.github.io/wildlife/apps/{name}/assets/icon.png";
	}

	// Is this lobby the active profile's running copy of the app?
	public static bool IsRunning(App app, Resources.WolfAPI.Lobby lobby)
	{
		var profileId = WolfApi.ActiveProfile?.Id;
		if (profileId is null || (lobby.ProfileId != profileId && lobby.StartedByProfileId != profileId))
			return false;
		return lobby.Name == app.Title || (app.Runner?.Name is not null && lobby.RunnerStateFolder == StateFolder(app));
	}

	// A lobby for this app, sized to the current stream. Null when the session can't be read.
	public static async Task<Resources.WolfAPI.Lobby?> NewLobby(App app, bool multiUser)
	{
		var session = await WolfApi.GetSession();
		if (session?.ClientSettings is null)
			return null;

		return new Resources.WolfAPI.Lobby
		{
			ProfileId = WolfApi.ActiveProfile.Id,
			Name = app.Title,
			MultiUser = multiUser,
			IconPngPath = IconPath(app),
			StopWhenEveryoneLeaves = false,
			RunnerStateFolder = StateFolder(app),
			Runner = app.Runner,
			VideoSettings = new VideoSettings
			{
				Width = session.VideoWidth,
				Height = session.VideoHeight,
				RefreshRate = session.VideoRefreshRate,
				RunnerRenderNode = app.RenderNode,
				WaylandRenderNode = app.RenderNode,
				VideoProducerBufferCaps = System.Environment.GetEnvironmentVariable("WOLF_VIDEO_BUFFER_CAPS") ?? ""
			},
			AudioSettings = new AudioSettings
			{
				ChannelCount = session.AudioChannelCount
			},
			ClientSettings = session.ClientSettings
		};
	}
}
