using BitzArt.UI.Tweaks.Config;
using BitzArt.UI.Tweaks.Gui;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks;

public class ModConfigFeature(UiTweaksModSystem modSystem, UiTweaksModConfig config)
    : ModSystemFeature<UiTweaksModSystem, UiTweaksModConfig>(modSystem, config)
{
    private ModConfigDialog? _dialog;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void Start(ICoreClientAPI clientApi)
    {
        _dialog = new(clientApi, Config);

        clientApi.Input.AddHotKey(ModHotKeys.ModConfiguration, (keys) => ToggleDialog());
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
            throw new NullReferenceException("Cairo test dialog is not initialized.");
        }

        if (_dialog.IsOpen)
        {
            _dialog.Close();
            return true;
        }

        _dialog.Open();

        return true;
    }
}
