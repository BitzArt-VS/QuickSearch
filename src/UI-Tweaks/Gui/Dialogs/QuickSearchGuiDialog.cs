using BitzArt.UI.Tweaks.Config;
using BitzArt.UI.Tweaks.Gui;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace BitzArt.UI.Tweaks;

internal partial class QuickSearchGuiDialog : Gui.VanillaGuiDialog
{
    private const string SearchInputKey = "quick-search-input";
    private const string ResultListKey = "resultList";
    private const string ScrollbarKey = "scrollbar";

    private readonly QuickSearchConfig _config;

    private readonly QuickSearchService _searchService;
    private readonly QuickSearchResultItem _calculatorResultItem = new();

    private Action<string, bool, bool>? _setSearchText;
    private string _input = string.Empty;

    public override double DrawOrder => 0.3;

    public QuickSearchGuiDialog(ICoreClientAPI clientApi, QuickSearchService search, QuickSearchConfig config) : base(clientApi)
    {
        _searchService = search;
        _config = config;

        Compose();

        _config.PropertyChanged += (_, _) => Compose(true);
    }

    public override void OnGuiOpened()
    {
        Compose();

        _setSearchText!.Invoke(_input, true, true);
        Search();
    }

    public void OnItemClicked(int index)
    {
        Task.Run(() =>
        {
            var resultList = SingleComposer.GetFlatList(ResultListKey);
            var item = (QuickSearchResultItem)resultList.Elements.ElementAtOrDefault(index)!;

            if (item is null)
            {
                return;
            }

            if (item.OnClicked(ClientApi))
            {
                ClientApi.Event.EnqueueMainThreadTask(() =>
                {
                    TryClose();
                }, "quicksearch-close");
            }
        });
    }

    public override void OnMouseDown(MouseEvent args)
    {
        if (!IsInDialog(args))
        {
            TryClose();
        }

        base.OnMouseDown(args);
    }

    protected void OnNewScrollbarValue(float value)
    {
        var resultList = SingleComposer.GetFlatList(ResultListKey);

        resultList.insideBounds.fixedY = 3 - value;
        resultList.insideBounds.CalcWorldBounds();
    }

    protected void Compose(bool enqueueOnMainThread)
    {
        if (enqueueOnMainThread)
        {
            ClientApi.Event.EnqueueMainThreadTask(() =>
            {
                Compose();
            }, "quicksearch-compose");

            return;
        }

        Compose();
    }

    private void Compose()
    {
        var searchFieldBounds = ElementBounds.Fixed(0, 20, 424, 32);
        var resultListBounds = ElementBounds.Fixed(0, 60, 400, _config.ResultListHeight);

        var clipBounds = resultListBounds.ForkBoundingParent();

        var scrollbarBounds = resultListBounds.CopyOffsetedSibling(resultListBounds.fixedWidth + 4).WithFixedWidth(20);

        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        SingleComposer = ClientApi.Gui
            .CreateCompo("quicksearch-dialog", ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle))
            .AddShadedDialogBG(bgBounds, true)
            .AddDialogTitleBar(Lang.Get($"{Constants.ModId}:quicksearch"), () => TryClose())
            .BeginChildElements(bgBounds)
                .AddTextInput(searchFieldBounds, OnTextInputChanged, key: SearchInputKey)
                .AddInset(resultListBounds)
                .BeginClip(clipBounds)
                    .AddFlatList(resultListBounds, OnItemClicked, key: ResultListKey)
                .EndClip()
                .AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, ScrollbarKey)
            .EndChildElements()
            .Compose();

        var scrollbar = SingleComposer.GetScrollbar(ScrollbarKey);
        var flatList = SingleComposer.GetFlatList(ResultListKey);

        if (scrollbar is not null && flatList is not null)
        {
            scrollbar.SetHeights(_config.ResultListHeight, (float)flatList.insideBounds.fixedHeight);
        }

        var textInput = SingleComposer.GetTextInput(SearchInputKey);
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

    private void OnTextInputChanged(string newText)
    {
        if (_input == newText)
        {
            return;
        }

        _input = newText;
        ClientApi.Logger.VerboseDebug($"QuickSearch input: '{newText}'");

        ClientApi.Gui.PlaySound("menubutton_press");

        Task.Run(Search);
    }

    private void Search()
    {
        // Check if the input looks like a math expression before trying to evaluate it
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
                // A custom parser would be more versatile and efficient.
                var result = new DataTable().Compute(input, null);

                ClientApi.Event.EnqueueMainThreadTask(() =>
                {
                    _calculatorResultItem.Text = $"={result}";
                    var resultList = SingleComposer.GetFlatList(ResultListKey);
                    resultList.Elements = [_calculatorResultItem];
                }, $"{Constants.ModId}-quicksearch-set-calc-result");

                return;
            }
            catch
            {
            }
        }

        SearchItems();
    }

    private void SearchItems()
    {
        List<IFlatListItem> results = [.. _searchService.Search(_input)];

        ClientApi.Event.EnqueueMainThreadTask(() =>
        {
            var resultList = SingleComposer.GetFlatList(ResultListKey);
            resultList.Elements = results;

            resultList.CalcTotalHeight();

            var scrollbar = SingleComposer.GetScrollbar(ScrollbarKey);
            scrollbar.SetHeights(_config.ResultListHeight, (float)resultList.insideBounds.fixedHeight);
        }, $"{Constants.ModId}-quicksearch-set-results");

        return;
    }

    private bool IsInDialog(MouseEvent args)
    {
        var dialogBounds = SingleComposer.Bounds;

        return args.X >= dialogBounds.absX
            && args.X <= dialogBounds.absX + dialogBounds.OuterWidth
            && args.Y >= dialogBounds.absY
            && args.Y <= dialogBounds.absY + dialogBounds.OuterHeight;
    }

    [GeneratedRegex("^(\\d+|[+\\-*\\/^()%,.]|\\s)+$")]
    private static partial Regex GetMathRegex();

    [GeneratedRegex(@"(\d+)%")]
    private static partial Regex GetPercentageRegex();
}
