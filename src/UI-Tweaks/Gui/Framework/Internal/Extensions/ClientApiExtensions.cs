using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks.Gui;

internal static class ClientApiExtensions
{
    private static FieldInfo? _clientMainField;
    private static MethodInfo? _unregisterDialogMethod;
    private static FieldInfo? _platformField;
    private static MethodInfo? _loadMouseCursorMethod;

    public static void UnregisterDialog(this ICoreClientAPI clientApi, Vintagestory.API.Client.GuiDialog dialog)
    {
        _clientMainField ??= clientApi.GetType()
            .GetField("game", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Could not find 'game' field on '{clientApi.GetType().Name}'.");

        var clientMain = _clientMainField.GetValue(clientApi)
            ?? throw new InvalidOperationException("'game' field is null.");

        _unregisterDialogMethod ??= clientMain.GetType()
            .GetMethod("UnregisterDialog", BindingFlags.Public | BindingFlags.Instance, null, [typeof(Vintagestory.API.Client.GuiDialog)], null)
            ?? throw new InvalidOperationException($"Could not find 'UnregisterDialog' method on '{clientMain.GetType().Name}'.");

        _unregisterDialogMethod.Invoke(clientMain, [dialog]);
    }

    public static bool LoadMouseCursor(this ICoreClientAPI clientApi, string code, int hotX, int hotY, BitmapRef bitmap)
    {
        _clientMainField ??= clientApi.GetType()
            .GetField("game", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Could not find 'game' field on '{clientApi.GetType().Name}'.");

        var clientMain = _clientMainField.GetValue(clientApi)
            ?? throw new InvalidOperationException("'game' field is null.");

        _platformField ??= clientMain.GetType()
            .GetField("Platform", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Could not find 'Platform' field on '{clientMain.GetType().Name}'.");

        var platform = _platformField.GetValue(clientMain)
            ?? throw new InvalidOperationException("'Platform' field is null.");

        // GetMethod walks inherited members by default — LoadMouseCursor lives on the
        // abstract base, the runtime instance is the windows/mac subclass.
        _loadMouseCursorMethod ??= platform.GetType()
            .GetMethod("LoadMouseCursor", [typeof(string), typeof(int), typeof(int), typeof(BitmapRef)])
            ?? throw new InvalidOperationException($"Could not find 'LoadMouseCursor' method on '{platform.GetType().Name}'.");

        return _loadMouseCursorMethod.Invoke(platform, [code, hotX, hotY, bitmap]) is true;
    }
}
