using BitzArt.UI.Tweaks.Gui;
using System;
using System.Collections.Generic;

namespace BitzArt.UI.Tweaks;

/// <summary>
/// Manages the content-page stack for <see cref="ModConfigDialog"/>. Pages push sub-pages
/// onto this stack to drill into deeper navigation levels; the dialog derives the current
/// rendered content and the global breadcrumb trail from the stack.
/// </summary>
internal sealed class ModConfigPageNavigator
{
    private sealed record PageEntry(string Name, GuiRenderFragment Content);

    private readonly List<PageEntry> _stack = [];
    private readonly Action _stateChanged;
    private string[]? _cachedPreviousNames;

    public ModConfigPageNavigator(Action stateChanged, string initialName, GuiRenderFragment initialContent)
    {
        _stateChanged = stateChanged;
        _stack.Add(new(initialName, initialContent));
    }

    public string CurrentPageName => _stack[^1].Name;
    public string[]? BreadcrumbPreviousItems => _cachedPreviousNames;
    public GuiRenderFragment CurrentContent => _stack[^1].Content;

    public bool IsAtRoot(string name) => _stack.Count == 1 && _stack[0].Name == name;

    public void NavigateToRoot(string name, GuiRenderFragment content)
    {
        _stack.Clear();
        _stack.Add(new(name, content));
        _cachedPreviousNames = null;
        _stateChanged();
    }

    public void Push(string name, GuiRenderFragment content)
    {
        _stack.Add(new(name, content));
        RebuildPreviousNamesCache();
        _stateChanged();
    }

    public void Pop()
    {
        if (_stack.Count <= 1) return;
        _stack.RemoveAt(_stack.Count - 1);
        RebuildPreviousNamesCache();
        _stateChanged();
    }

    public void PopToName(string name)
    {
        int index = _stack.FindIndex(e => e.Name == name);
        if (index < 0 || index == _stack.Count - 1) return;
        _stack.RemoveRange(index + 1, _stack.Count - index - 1);
        RebuildPreviousNamesCache();
        _stateChanged();
    }

    private void RebuildPreviousNamesCache()
    {
        if (_stack.Count <= 1)
        {
            _cachedPreviousNames = null;
            return;
        }

        var names = new string[_stack.Count - 1];
        for (int i = 0; i < names.Length; i++)
        {
            names[i] = _stack[i].Name;
        }
        _cachedPreviousNames = names;
    }
}
