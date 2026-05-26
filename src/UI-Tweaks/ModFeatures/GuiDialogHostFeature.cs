using BitzArt.UI.Tweaks.Gui;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks;

public class GuiDialogHostFeature(UiTweaksModSystem modSystem)
    : ModSystemFeature<UiTweaksModSystem>(modSystem)
{
    private bool _isInitialized;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void Start(ICoreClientAPI clientApi)
    {
        GuiDialogHost.Initialize(clientApi);
        _isInitialized = true;
    }

    public override void Dispose()
    {
        if (_isInitialized)
        {
            GuiDialogHost.Instance.Dispose();
            _isInitialized = false;
        }

        base.Dispose();
    }
}
