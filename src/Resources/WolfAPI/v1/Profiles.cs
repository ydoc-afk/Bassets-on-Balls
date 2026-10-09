using System.Threading.Tasks;
using System.Collections.Generic;
using WolfUI;

namespace Resources.WolfAPI;


public partial class WolfApi
{
    public static async Task<List<Profile>> GetProfiles()
    {
        var profiles = await GetAsync<ProfilesResponse>($"{Api}/profiles");

        if (profiles?.Profiles is not null && profiles.Success) return profiles.Profiles;
        Logger.LogError("Error retrieving Profiles");
        return [];
    }

    // Creates an account. With no apps given, Heeler starts it with the default set.
    public static async Task<bool> AddProfile(string id, string name, List<int>? pin)
    {
        var body = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["name"] = name,
            ["icon_png_path"] = "",
            ["apps"] = new List<object>()
        };
        if (pin is { Count: > 0 }) body["pin"] = pin;

        var result = await PostAsync("/profiles/add", body);
        return result is not null && result.Contains("\"success\":true");
    }
}
