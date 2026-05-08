using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace BitzArt.UI.Tweaks;

/// <summary>
/// A <see cref="ModSystem"/> which only starts client-side.
/// </summary>
public abstract class ClientModSystem : ModSystem
{
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    protected sealed override void Start(ICoreServerAPI serverApi)
        => throw new NotSupportedException($"The mod system '{Name}' does not support starting server-side.");
}

/// <summary>
/// A <see cref="ModSystem"/> which only starts server-side.
/// </summary>
public abstract class ServerModSystem : ModSystem
{
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

    protected sealed override void Start(ICoreClientAPI clientApi)
        => throw new NotSupportedException($"The mod system '{Name}' does not support starting client-side.");
}

/// <summary>
/// A custom, safer, and more robust base class for ModSystems,
/// extending the normal <see cref="Vintagestory.API.Common.ModSystem"/>.
/// </summary>
/// <remarks>
/// ModSystems serve as entrypoints for code mods,
/// allowing the developer to run necessary code when the game starts,
/// both client and server-side, and interact with the game engine through the provided APIs.
/// </remarks>
public abstract partial class ModSystem : Vintagestory.API.Common.ModSystem
{
    protected virtual string Name => GetModSystemNamePartRegex().Replace(GetType().Name, string.Empty);

    private string GetStartErrorMessage(string side)
        => $"An error occurred while starting the mod system '{Name}' ({side}). The system will not start.";

    protected List<ModSystemFeature> Features { get; set; } = [];

    public sealed override void StartClientSide(ICoreClientAPI clientApi)
    {
        base.StartClientSide(clientApi);
        try
        {
            Start(clientApi);

            var clientSideFeatures = Features
                .Where(feature => feature.ShouldLoad(EnumAppSide.Client))
                .ToList();

            foreach (var feature in clientSideFeatures)
            {
                try
                {
                    feature.Start(clientApi);
                }
                catch (Exception ex)
                {
                    clientApi.Logger.Error(ex);
                    clientApi.Logger.Error($"An error occurred while starting the feature '{feature.Name}' for mod system '{Name}' (client-side). The feature will remain disabled.");

                    feature.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            clientApi.Logger.Error(ex);
            clientApi.Logger.Error(GetStartErrorMessage("client-side"));

            OnStartFailed(clientApi, ex);
            Dispose();
        }
    }

    public sealed override void StartServerSide(ICoreServerAPI serverApi)
    {
        base.StartServerSide(serverApi);
        try
        {
            Start(serverApi);

            var serverSideFeatures = Features
                .Where(feature => feature.ShouldLoad(EnumAppSide.Server))
                .ToList();

            foreach (var feature in serverSideFeatures)
            {
                try
                {
                    feature.Start(serverApi);

                }
                catch (Exception ex)
                {
                    serverApi.Logger.Error(ex);
                    serverApi.Logger.Error($"An error occurred while starting the feature '{feature.Name}' for mod system '{Name}' (server-side). The feature will remain disabled.");

                    feature.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            serverApi.Logger.Error(ex);
            serverApi.Logger.Error(GetStartErrorMessage("server-side"));

            OnStartFailed(serverApi, ex);
            Dispose();
        }
    }

    public sealed override void Dispose()
    {
        foreach (var feature in Features)
        {
            try
            {
                feature.Dispose();
            }
            catch (Exception ex)
            {
                // Just log the error and continue disposing other features,
                // as we want to dispose as much as possible even if some features fail during disposal.
                Console.Error.WriteLine($"An error occurred while disposing the feature '{feature.Name}' for mod system '{Name}'.");
                Console.Error.WriteLine(ex);
            }
        }

        OnDispose();
    }

    /// <summary>
    /// Starts the mod system client-side.
    /// </summary>
    /// <param name="clientApi">API for interacting with game engine client-side.</param>
    protected abstract void Start(ICoreClientAPI clientApi);

    /// <summary>
    /// A method invoked when an exception is thrown during attempting to start the mod system client-side.
    /// </summary>
    /// <param name="clientApi">API for interacting with game engine client-side.</param>
    /// <param name="ex">The exception thrown during attempting to start the mod system.</param>
    protected virtual void OnStartFailed(ICoreClientAPI clientApi, Exception ex) { }

    /// <summary>
    /// Starts the mod system server-side.
    /// </summary>
    /// <param name="serverApi">API for interacting with game engine server-side.</param>
    protected abstract void Start(ICoreServerAPI serverApi);

    /// <summary>
    /// A method invoked when an exception is thrown during attempting to start the mod system server-side.
    /// </summary>
    /// <param name="serverApi">API for interacting with game engine server-side.</param>
    /// <param name="ex">The exception thrown during attempting to start the mod system.</param>
    protected virtual void OnStartFailed(ICoreServerAPI serverApi, Exception ex) { }

    /// <summary>
    /// Called at the end of the <see cref="Dispose"/> method, after all features have been disposed.
    /// </summary>
    protected virtual void OnDispose() { }

    [GeneratedRegex("(ModSystem|System)$")]
    private static partial Regex GetModSystemNamePartRegex();
}
