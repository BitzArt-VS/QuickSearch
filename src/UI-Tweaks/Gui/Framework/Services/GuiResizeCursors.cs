using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Lazy on-demand registration of resize-edge cursors with the game's platform layer.
/// Vanilla only ships <c>normal</c>/<c>textselect</c>/<c>linkselect</c>/<c>move</c>/<c>busy</c>
/// (see <c>F:\VintageStory\assets\game\textures\gui\cursors\</c>) — no NS/EW/diagonal
/// resize sprites — so the mod ships its own PNGs at
/// <c>resources/assets/bitzartuitweaks/textures/gui/cursors/</c> alongside a
/// <c>coords.json</c> declaring hot-points (mirrors the vanilla layout exactly).
/// <para>
/// Loading mirrors vanilla <c>ScreenManager.LoadCursor</c>: pull the asset via
/// <see cref="IAssetManager.TryGet(AssetLocation)"/>, decode through
/// <see cref="IAsset.ToBitmap(ICoreClientAPI)"/>, then forward to
/// <c>ClientPlatformAbstract.LoadMouseCursor</c> via
/// <see cref="ClientApiExtensions.LoadMouseCursor"/>. Skipped on macOS to match vanilla,
/// where the platform implementation rejects custom cursors anyway.
/// </para>
/// </summary>
internal static class GuiResizeCursors
{
    /// <summary>Cursor code for the East/West edge — horizontal double-arrow.</summary>
    internal const string Horizontal = "bitzart-uitw-resize-h";
    /// <summary>Cursor code for the North/South edge — vertical double-arrow.</summary>
    internal const string Vertical = "bitzart-uitw-resize-v";
    /// <summary>Cursor code for the NW↔SE diagonal corner — diagonal double-arrow.</summary>
    internal const string DiagonalNwSe = "bitzart-uitw-resize-nwse";

    private const string Domain = "bitzartuitweaks";
    private const string CursorAssetDir = "textures/gui/cursors";

    private static bool _loaded;

    /// <summary>
    /// Registers the resize cursors with the platform layer if not already loaded. Safe
    /// to call from every <see cref="GuiDialog"/> ctor — guarded by a static flag so the
    /// asset I/O and platform call only fire once per process.
    /// </summary>
    internal static void EnsureLoaded(ICoreClientAPI api)
    {
        if (_loaded) return;
        _loaded = true;

        // Vanilla short-circuits cursor loading on macOS (the SDL/GLFW cursor API
        // misbehaves with custom cursors on Cocoa). Mirror that — the resize gesture
        // still works, just without the custom cursor visuals; vanilla "move" stays as
        // a passable fallback.
        if (RuntimeEnv.OS == OS.Mac) return;

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
        if (coords is null) return;

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
