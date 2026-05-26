using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks;

public class HudElement(ICoreClientAPI clientApi) : Vintagestory.API.Client.HudElement(clientApi)
{
    protected ICoreClientAPI ClientApi = clientApi;

    public override double DrawOrder => 0.2;

    public override void Dispose()
    {
        base.Dispose();
        ClientApi = null!;

        GC.SuppressFinalize(this);
    }
}
