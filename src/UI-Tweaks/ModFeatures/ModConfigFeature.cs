using BitzArt.UI.Tweaks.Config;
using BitzArt.UI.Tweaks.Gui;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks;

public class ModConfigFeature(UiTweaksModSystem modSystem, UiTweaksModConfig config)
    : ModSystemFeature<UiTweaksModSystem, UiTweaksModConfig>(modSystem, config)
{
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void Start(ICoreClientAPI clientApi)
    {
        clientApi.Input.AddHotKey(ModHotKeys.ModConfiguration, (keys) => ToggleDialog());
    }

    private bool ToggleDialog()
    {
        return GuiDialogHost.Toggle<ModConfigDialog>(dialog => dialog.Configure(Config));
    }
}
