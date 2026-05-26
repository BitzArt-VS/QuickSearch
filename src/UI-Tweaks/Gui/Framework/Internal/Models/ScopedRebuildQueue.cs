namespace BitzArt.UI.Tweaks.Gui;

internal sealed class ScopedRebuildQueue
{
    private Dictionary<GuiRenderFragment, GuiRenderTreeBuilder> _pending = [];
    private Dictionary<GuiRenderFragment, GuiRenderTreeBuilder> _active = [];

    internal void Schedule(GuiRenderFragment fragment, GuiRenderTreeBuilder builder) => _pending[fragment] = builder;

    internal void Cancel(GuiRenderFragment fragment)
    {
        _pending.Remove(fragment);
        _active.Remove(fragment);
    }

    internal bool Drain()
    {
        if (_pending.Count == 0)
        {
            return false;
        }

        (_pending, _active) = (_active, _pending);
        while (_active.Count > 0)
        {
            var enumerator = _active.GetEnumerator();
            enumerator.MoveNext();
            var (fragment, builder) = enumerator.Current;
            _active.Remove(fragment);
            builder.Run(fragment);
        }
        return true;
    }
}
