using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

internal class QuickSearchDialog : ModGuiDialog
{
    private int _openCount = 0;
    private readonly ElementBounds _dialogBounds;
    private readonly ElementBounds _bgBounds;

    public override double DrawOrder => 0.3;

    public QuickSearchDialog(ICoreClientAPI clientApi) : base(clientApi)
    {
        _dialogBounds = ElementStdBounds
            .AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle)
            .WithFixedAlignmentOffset(0, ClientApi.Render.FrameHeight * 0.2);

        _bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        _bgBounds.BothSizing = ElementSizing.FitToChildren;

        SingleComposer = GetComposer();
        SingleComposer.Compose();
    }

    private GuiComposer GetComposer() => ClientApi.Gui
        .CreateCompo("quicksearch", _dialogBounds)
        .AddShadedDialogBG(_bgBounds, false)
        .AddDialogTitleBar(Lang.GetMatching("quicksearch-dialog-title"))
        .BeginChildElements(_bgBounds)
        .AddTextInput(ElementBounds.Fixed(0, 16, 280, 32), OnTextInputChanged, CairoFont.TextInput(), "quick-search-input")
        .AddDynamicText("Text to be added", CairoFont.WhiteDetailText(), ElementBounds.Fixed(0, 48, 200, 32));

    public override void OnGuiOpened()
    {
        _openCount++;
        SingleComposer.GetDynamicText("costText")?.SetNewText($"Opened {_openCount} times.");
        InvokeAsync(() => SingleComposer.Compose());

        var textInput = SingleComposer.GetTextInput("quick-search-input");
        Focus();

        ClientApi.ShowChatMessage($"QuickSearch is now: ON");
    }

    public override void OnGuiClosed()
    {
        ClientApi.ShowChatMessage($"QuickSearch is now: OFF");
    }

    private void OnTextInputChanged(string newText)
    {
        ClientApi.ShowChatMessage($"QuickSearch: '{newText}'");
    }
}
