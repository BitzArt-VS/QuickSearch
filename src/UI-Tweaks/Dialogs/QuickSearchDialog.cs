using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks;

internal partial class QuickSearchDialog : ModGuiDialog
{
    private string _input = string.Empty;

    private CancellationTokenSource? _searchCancellationTokenSource;

    private Action<string, bool, bool>? _setSearchText;
    private readonly QuickSearchService _search;

    public override double DrawOrder => 0.3;

    public QuickSearchDialog(ICoreClientAPI clientApi, QuickSearchService search) : base(clientApi)
    {
        _search = search;

        RegisterQuickSearchHotKey();
        Compose();
    }

    private void Compose()
    {
        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        SingleComposer = ClientApi.Gui
        .CreateCompo("quicksearch-dialog", ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle))
        .AddGrayBG(bgBounds)
        .BeginChildElements(bgBounds)
        .AddTextInput(ElementBounds.Fixed(0, 16, 280, 32), OnTextInputChanged, key: "quick-search-input")
        .AddDynamicText(string.Empty, CairoFont.WhiteDetailText(), ElementBounds.Fixed(0, 64, 200, 128), "resultText")
        .Compose();

        var textInput = SingleComposer.GetTextInput("quick-search-input");
        var selectedTextStartField = typeof(GuiElementEditableTextBase).GetField("selectedTextStart", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Could not find 'selectedTextStart' field in GuiElementEditableTextBase");

        _setSearchText = (text, select, setEnd) =>
        {
            textInput.LoadValue([text]);

            if (setEnd)
            {
                if (select)
                {
                    selectedTextStartField.SetValue(textInput, 0);
                }
                textInput.SetCaretPos(text.Length);
            }
        };
    }

    private void RegisterQuickSearchHotKey()
    {
        ClientApi.Input.AddHotKey(ModHotKeys.QuickSearch, (keys) =>
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);

            if (IsOpened())
            {
                TryClose();
                return true;
            }

            TryOpenOnKeyPress();

            return true;
        });
    }

    public override void OnGuiOpened()
    {
        Compose();

        _setSearchText!.Invoke(_input, true, true);
        RunSearch();

        ClientApi.Logger.VerboseDebug($"QuickSearch is now: ON");
    }

    public override void OnGuiClosed()
    {
        ClientApi.Logger.VerboseDebug($"QuickSearch is now: OFF");
    }

    private void OnTextInputChanged(string newText)
    {
        if (_input == newText)
        {
            return;
        }

        _input = newText;
        ClientApi.Logger.VerboseDebug($"QuickSearch input: '{newText}'");

        ClientApi.Gui.PlaySound("menubutton_press");

        RunSearch();
    }

    private void RunSearch()
    {
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource = new();

        Task.Run(() =>
        {
            if (!string.IsNullOrWhiteSpace(_input) && GetMathRegex().IsMatch(_input))
            {
                try
                {
                    // Replace percentage matches with their decimal equivalents before evaluating the expression.
                    // (DataTable.Compute doesn't support percentages)
                    var input = GetPercentageRegex().Replace(_input, match =>
                    {
                        if (double.TryParse(match.Groups[1].Value, out var number))
                        {
                            return (number / 100).ToString();
                        }
                        return match.Value; // If parsing fails, return the original match
                    });

                    // Quick and dirty way to evaluate simple math expressions without writing a custom parser.
                    // Requires a try-catch to function though, which is unfortunate.
                    var result = new DataTable().Compute(input, null);

                    ClientApi.Event.EnqueueMainThreadTask(() =>
                    {
                        SetResultText(result.ToString() ?? string.Empty);
                    }, "quicksearch-set-results");

                    return;
                }
                catch
                {
                }
            }

            Search(_searchCancellationTokenSource.Token);
        });
    }

    private void Search(CancellationToken cancellationToken)
    {
        ClientApi.Event.EnqueueMainThreadTask(() =>
        {
            SetResultText(string.Empty);
        }, "quicksearch-set-results");

        if (string.IsNullOrWhiteSpace(_input))
        {
            return;
        }

        var resultItems = _search.Search(_input).Take(10).ToList();

        cancellationToken.ThrowIfCancellationRequested();

        if (resultItems.Count == 0)
        {
            return;
        }

        ClientApi.Event.EnqueueMainThreadTask(() =>
        {
            SetResultText(string.Join(", ", resultItems.Select(x => x.GetName())));
        }, "quicksearch-set-results");

        return;
    }

    private void SetResultText(string text)
    {
        SingleComposer.GetDynamicText("resultText")?.SetNewText(text);
    }

    [GeneratedRegex("^(\\d+|[+\\-*\\/^()%,.]|\\s)+$")]
    private static partial Regex GetMathRegex();

    [GeneratedRegex(@"(\d+)%")]
    private static partial Regex GetPercentageRegex();
}
