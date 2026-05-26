using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks;

internal static class GetModConfigExtension
{
    public static T GetModConfig<T>(this ICoreClientAPI clientApi, string filename)
        where T : class, new()
    {
        try
        {
            var config = clientApi.LoadModConfig<T>(filename) ?? CreateModConfig<T>(clientApi, filename);
            clientApi.StoreModConfig(config, filename);

            return config;
        }
        catch (Exception ex)
        {
            clientApi.Logger.Error($"Failed to load mod config from file '{filename}'.");
            clientApi.Logger.Error(ex);

            return CreateModConfig<T>(clientApi, filename);
        }
    }

    private static T CreateModConfig<T>(this ICoreClientAPI clientApi, string filename)
        where T : class, new()
    {
        clientApi.Logger.Warning($"Creating new mod config file '{filename}'.");

        T config = new();
        clientApi.StoreModConfig(config, filename);

        return config;
    }

}
