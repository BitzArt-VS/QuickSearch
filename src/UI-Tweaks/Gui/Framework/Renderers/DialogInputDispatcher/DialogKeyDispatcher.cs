using System.Collections.Generic;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class DialogKeyDispatcher
{
    private const float BlinkPeriodSeconds = 0.5f;
    private float _blinkAccumulator;
    private bool _caretBlinkOn = true;

    private readonly List<KeyboardRegion> _keyboardRegions = [];

    internal IGuiNode? FocusedNode { get; private set; }
    internal bool CaretBlinkOn => _caretBlinkOn;

    internal void AddKeyboardRegion(in KeyboardRegion region) => _keyboardRegions.Add(region);
    internal void ClearKeyboardRegions() => _keyboardRegions.Clear();

    internal bool Tick(float deltaTime)
    {
        if (FocusedNode is null) return false;
        _blinkAccumulator += deltaTime;
        if (_blinkAccumulator < BlinkPeriodSeconds) return false;
        _blinkAccumulator -= BlinkPeriodSeconds;
        _caretBlinkOn = !_caretBlinkOn;
        return true;
    }

    internal bool SetFocusedNode(IGuiNode? node)
    {
        if (ReferenceEquals(FocusedNode, node)) return false;
        FocusedNode = node;
        _blinkAccumulator = 0f;
        _caretBlinkOn = true;
        return true;
    }

    internal bool DispatchKeyDown(KeyEvent args) => DispatchKey(GuiKeyEventKind.Down, args);
    internal bool DispatchKeyUp(KeyEvent args) => DispatchKey(GuiKeyEventKind.Up, args);
    internal bool DispatchKeyPress(KeyEvent args) => DispatchKey(GuiKeyEventKind.Press, args);

    private bool DispatchKey(GuiKeyEventKind kind, KeyEvent args)
    {
        if (FocusedNode is null) return false;
        var keyArgs = new GuiKeyEventArgs(args);

        for (int i = 0; i < _keyboardRegions.Count; i++)
        {
            if (!ReferenceEquals(_keyboardRegions[i].Token, FocusedNode)) continue;
            _keyboardRegions[i].Dispatch(kind, keyArgs);
            break;
        }

        DispatchKeyToNode(FocusedNode, kind, keyArgs);
        return true;
    }

    private static void DispatchKeyToNode(IGuiNode node, GuiKeyEventKind kind, GuiKeyEventArgs args)
    {
        switch (kind)
        {
            case GuiKeyEventKind.Down: node.OnKeyDown(args); break;
            case GuiKeyEventKind.Up: node.OnKeyUp(args); break;
            case GuiKeyEventKind.Press: node.OnKeyPress(args); break;
        }
    }
}
