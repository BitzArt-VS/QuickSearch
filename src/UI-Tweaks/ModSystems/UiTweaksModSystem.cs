using BitzArt.UI.Tweaks.Config;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks;

public class UiTweaksModSystem : ClientModSystem
{
    protected override string Name => Constants.ModName;

    protected override void Start(ICoreClientAPI clientApi)
    {
        var config = clientApi.GetModConfig<UiTweaksModConfig>(Constants.ModConfigFileName);

        Features =
        [
            new ModConfigFeature(this, config),
            new GameTweaksFeature(this, config),
            new GameStatusFeature(this, config),
            new QuickSearchFeature(this, config.QuickSearch)
        ];
    }
}
