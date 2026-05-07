using System;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Immutable single-linked list of cascading-value entries visible at a given point in
/// the render tree. A builder's chain reference points at the innermost link; lookup walks
/// toward the root and returns the first match — so an inner scope naturally shadows any
/// outer scope with the same <c>(Type, Name)</c> key.
/// </summary>
/// <remarks>
/// Values are stored as <c>object?</c> so the chain is a homogeneous linked list regardless
/// of the published <typeparamref name="T"/>. The type-equality check in
/// <see cref="TryGet{T}"/> gates a direct unbox/cast, incurring at most one allocation per
/// lookup (for value types) — acceptable because lookups happen at most once per reconcile
/// per consumer component, not every frame.
/// </remarks>
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

    /// <summary>
    /// Walks the chain from innermost to outermost, returning the first entry whose
    /// <c>(Type, Name)</c> matches <typeparamref name="T"/> and <paramref name="name"/>.
    /// Inner scopes shadow outer ones.
    /// </summary>
    public bool TryGet<T>(string? name, out T value)
    {
        for (var node = this; node is not null; node = node._parent)
        {
            if (node._valueType != typeof(T)) continue;
            if (node._name != name) continue;

            value = (T)node._value!;
            return true;
        }

        value = default!;
        return false;
    }
}
