using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace BitzArt.UI.Tweaks;

internal class QuickSearchResultItem : IFlatListItem
{
    private readonly ItemStack? _itemStack;

    private readonly string? _pageCode;
    private readonly LinkTextComponent? _pageLink;
    private readonly InventoryBase? _unspoilableInventory;
    private readonly DummySlot? _dummySlot;
    private ElementBounds? _scissorBounds;

    private LoadedTexture? _texture;

    bool IFlatListItem.Visible => true;

    public string? Text
    {
        get => field;
        set
        {
            field = value;
            _texture?.Dispose();
            _texture = null;
        }
    }

    public QuickSearchResultItem(ItemStack itemStack, ICoreClientAPI clientApi) : this()
    {
        _itemStack = itemStack;
        Text = itemStack.GetName();

        _pageCode = _itemStack.Collectible
            .GetCollectibleInterface<IHandBookPageCodeProvider>()?
            .HandbookPageCodeForStack(clientApi.World, _itemStack)
            ?? GuiHandbookItemStackPage.PageCodeForStack(_itemStack);

        _pageLink = new($"handbook://{_pageCode}");

        _unspoilableInventory = new CreativeInventoryTab(1, "not-used", null);
        _dummySlot = new DummySlot(itemStack, _unspoilableInventory);
    }

    public QuickSearchResultItem(string text) : this()
    {
        Text = text;
    }

    public QuickSearchResultItem() { }

    void IFlatListItem.RenderListEntryTo(ICoreClientAPI clientApi, float dt, double x, double y, double cellWidth, double cellHeight)
    {
        if (_texture is null)
        {
            Recompose(clientApi);
        }

        if (_itemStack is not null)
        {
            RenderItemStack(clientApi, x, y);
            return;
        }
        if (Text is not null)
        {
            RenderText(clientApi, x, y);
            return;
        }
    }

    public bool OnClicked(ICoreClientAPI clientApi)
    {
        if (_itemStack is null || _pageCode is null)
        {
            return false;
        }

        clientApi.Event.EnqueueMainThreadTask(() =>
        {
            clientApi.LinkProtocols["handbook"]!.Invoke(_pageLink);
        }, "quicksearch-open-handbook-entry");

        return true;
    }

    public void Dispose()
    {
        _texture?.Dispose();
        _texture = null;
    }

    private void Recompose(ICoreClientAPI clientApi)
    {
        _texture?.Dispose();
        using var font = _itemStack is null ? CairoFont.WhiteMediumText() : CairoFont.WhiteSmallText();
        _texture = new TextTextureUtil(clientApi).GenTextTexture(Text, font);

        _scissorBounds = ElementBounds.FixedSize(50, 50);
        _scissorBounds.ParentBounds = clientApi.Gui.WindowBounds;
    }

    private void RenderItemStack(ICoreClientAPI clientApi, double x, double y)
    {
        float size = (float)GuiElement.scaled(25);
        float pad = (float)GuiElement.scaled(10);

        _scissorBounds!.fixedX = (pad + x - size / 2) / RuntimeEnv.GUIScale;
        _scissorBounds.fixedY = (y - size / 2) / RuntimeEnv.GUIScale;
        _scissorBounds.CalcWorldBounds();

        if (_scissorBounds.InnerWidth <= 0 || _scissorBounds.InnerHeight <= 0)
        {
            return;
        }

        clientApi.Render.PushScissor(_scissorBounds, true);
        clientApi.Render.RenderItemstackToGui(_dummySlot, x + pad + size / 2, y + size / 2, 100, size, ColorUtil.WhiteArgb, true, false, false);
        clientApi.Render.PopScissor();

        clientApi.Render.Render2DTexturePremultipliedAlpha(
            _texture!.TextureId,
            (x + size + GuiElement.scaled(25)),
            y + size / 4 - GuiElement.scaled(3),
            _texture.Width,
            _texture.Height,
            50
        );
    }

    private void RenderText(ICoreClientAPI clientApi, double x, double y)
    {
        clientApi.Render.Render2DTexturePremultipliedAlpha(
            _texture!.TextureId,
            x,
            y - GuiElement.scaled(3),
            _texture.Width,
            _texture.Height,
            50
        );
    }
}
