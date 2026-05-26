using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace BitzArt.UI.Tweaks.Gui;

internal static class GuiResizeCursors
{
    internal const string Horizontal = "bitzart-uitw-resize-h";
    internal const string Vertical = "bitzart-uitw-resize-v";
    internal const string DiagonalNwSe = "bitzart-uitw-resize-nwse";

    private const string Domain = "bitzartuitweaks";
    private const string CursorAssetDir = "textures/gui/cursors";

    private static bool _loaded;

    internal static void EnsureLoaded(ICoreClientAPI api)
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;

        // Vanilla short-circuits cursor loading on macOS (the SDL/GLFW cursor API
        // misbehaves with custom cursors on Cocoa). Mirror that — the resize gesture
        // still works, just without the custom cursor visuals; vanilla "move" stays as
        // a passable fallback.
        if (RuntimeEnv.OS == OS.Mac)
        {
            return;
        }

        // coords.json mirrors the vanilla format: { "code": { x: int, y: int } } where
        // (x, y) is the hot-point in pixels.
        var coordsAsset = api.Assets.TryGet(new AssetLocation(Domain, $"{CursorAssetDir}/coords.json"));
        if (coordsAsset is null)
        {
            api.World.Logger.Warning("[UI-Tweaks] Resize cursor coords.json missing — resize cursors will not be available.");
            return;
        }

        Dictionary<string, Vec2i>? coords;
        try
        {
            coords = coordsAsset.ToObject<Dictionary<string, Vec2i>>();
        }
        catch (Exception e)
        {
            api.World.Logger.Warning("[UI-Tweaks] Failed to parse resize cursor coords.json: {0}", e.Message);
            return;
        }
        if (coords is null)
        {
            return;
        }

        TryLoad(api, coords, "resize-h", Horizontal);
        TryLoad(api, coords, "resize-v", Vertical);
        TryLoad(api, coords, "resize-nwse", DiagonalNwSe);
    }

    private static void TryLoad(ICoreClientAPI api, Dictionary<string, Vec2i> coords, string assetName, string registerCode)
    {
        if (!coords.TryGetValue(assetName, out var hotPoint))
        {
            api.World.Logger.Warning("[UI-Tweaks] coords.json has no entry for '{0}'.", assetName);
            return;
        }

        var asset = api.Assets.TryGet(new AssetLocation(Domain, $"{CursorAssetDir}/{assetName}.png"));
        if (asset is null)
        {
            api.World.Logger.Warning("[UI-Tweaks] Resize cursor asset '{0}.png' missing.", assetName);
            return;
        }

        // ToBitmap returns a BitmapExternal (SKBitmap-backed BitmapRef) — exactly what
        // ClientPlatformWindows.LoadMouseCursor expects. The platform layer copies the
        // pixel data into its own buffer before returning, so we don't keep a reference;
        // dispose immediately to free the SKBitmap.
        BitmapRef? bitmap = null;
        try
        {
            bitmap = asset.ToBitmap(api);
            api.LoadMouseCursor(registerCode, hotPoint.X, hotPoint.Y, bitmap);
        }
        catch (Exception e)
        {
            api.World.Logger.Warning("[UI-Tweaks] Failed to register resize cursor '{0}': {1}", registerCode, e.Message);
        }
        finally
        {
            bitmap?.Dispose();
        }
    }
}
