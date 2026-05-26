using BitzArt.UI.Tweaks.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks;

public class QuickSearchFeature(UiTweaksModSystem modSystem, QuickSearchConfig config)
    : ModSystemFeature<UiTweaksModSystem, QuickSearchConfig>(modSystem, config)
{
    private QuickSearchGuiDialog? _dialog;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client && Config.Enable;

    public override void Start(ICoreClientAPI clientApi)
    {
        _dialog = new(clientApi, new(clientApi), Config);

        clientApi.Input.AddHotKey(ModHotKeys.QuickSearch, (keys) => ToggleDialog());
    }

    public override void Dispose()
    {
        _dialog?.Dispose();
        _dialog = null;

        GC.SuppressFinalize(this);
    }

    private bool ToggleDialog()
    {
        if (_dialog is null)
        {
            throw new NullReferenceException("QuickSearch dialog is not initialized.");
        }

        if (_dialog.IsOpened())
        {
            _dialog.TryClose();
            return true;
        }

        _dialog.TryOpenOnKeyPress();

        return true;
    }
}
