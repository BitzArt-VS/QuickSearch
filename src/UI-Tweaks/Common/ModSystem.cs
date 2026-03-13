using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks;

public class UiTweaksModSystem : ModSystem
{
    private QuickSearchService? _quickSearchService;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api)
    {
        _quickSearchService = new(api);
        _quickSearchService.Initialize();
    }

    public override void Dispose()
    {
        _quickSearchService?.Dispose();
        _quickSearchService = null;
    }
}
