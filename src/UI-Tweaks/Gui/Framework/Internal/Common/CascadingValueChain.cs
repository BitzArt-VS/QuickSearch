namespace BitzArt.UI.Tweaks.Gui;

internal sealed class CascadingValueChain
{
    private readonly CascadingValueChain? _parent;
    private readonly Type _valueType;
    private readonly string? _name;
    private readonly object? _value;

    public CascadingValueChain(CascadingValueChain? parent, Type valueType, string? name, object? value)
    {
        _parent = parent;
        _valueType = valueType;
        _name = name;
        _value = value;
    }

    public bool TryGet<T>(string? name, out T value)
    {
        for (var node = this; node is not null; node = node._parent)
        {
            if (node._valueType != typeof(T))
            {
                continue;
            }

            if (node._name != name)
            {
                continue;
            }

            value = (T)node._value!;
            return true;
        }

        value = default!;
        return false;
    }
}
