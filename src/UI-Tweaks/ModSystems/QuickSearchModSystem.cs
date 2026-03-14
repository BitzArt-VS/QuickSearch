using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks;

public class QuickSearchModSystem : ModSystem
{
    private QuickSearchDialog? _dialog;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        _dialog = new(api, new(api));
    }

    public override void Dispose()
    {
        _dialog?.Dispose();
        _dialog = null;
    }
}
