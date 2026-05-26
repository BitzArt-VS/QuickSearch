namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// A unified callback delegate that can be assigned from either a synchronous
/// <see cref="Action"/> or an asynchronous <see cref="Func{Task}"/>. Stored as a value type —
/// <c>default(GuiCallback)</c> represents "no handler" and incurs zero allocation.
/// <para>
/// The synchronous invocation path (<see cref="Invoke"/>) does not allocate when assigned
/// from an <see cref="Action"/>: we keep a single <see cref="object"/> reference plus a one-byte
/// shape flag, then dispatch through a single virtual call after a non-virtual cast. This
/// matches the cost of invoking a plain delegate field.
/// </para>
/// <para>
/// Inspired by Blazor's <c>EventCallback</c>: lets API surfaces accept "either flavour" of
/// handler in a single parameter type without forcing callers to wrap synchronous code in
/// <c>Task.CompletedTask</c> or pay an async-state-machine cost on the hot synchronous path.
/// </para>
/// </summary>
public readonly struct GuiCallback
{
    private readonly object? _handler;
    private readonly bool _isAsync;

    private GuiCallback(object? handler, bool isAsync)
    {
        _handler = handler;
        _isAsync = isAsync;
    }

    /// <summary>True when this callback was assigned a non-null handler.</summary>
    public bool HasHandler => _handler is not null;

    public static implicit operator GuiCallback(Action handler) =>
        new(handler, isAsync: false);

    public static implicit operator GuiCallback(Func<Task> handler) =>
        new(handler, isAsync: true);

    internal static GuiCallback Combine(GuiCallback first, GuiCallback second)
    {
        if (!first.HasHandler)
        {
            return second;
        }
        if (!second.HasHandler)
        {
            return first;
        }

        return new Action(() =>
        {
            first.Invoke();
            second.Invoke();
        });
    }

    /// <summary>
    /// Invokes the callback synchronously. For asynchronous handlers the returned <see cref="Task"/>
    /// is discarded — exceptions from a faulting task propagate through the framework's render
    /// thread error handling on observation. No-op when <see cref="HasHandler"/> is false.
    /// </summary>
    public void Invoke()
    {
        if (_handler is null)
        {
            return;
        }

        if (_isAsync)
        {
            _ = ((Func<Task>)_handler).Invoke();
        }
        else
        {
            ((Action)_handler).Invoke();
        }
    }

    /// <summary>
    /// Invokes the callback and returns its <see cref="Task"/>. Synchronous handlers complete
    /// inline and return <see cref="Task.CompletedTask"/>. Returns <see cref="Task.CompletedTask"/>
    /// when <see cref="HasHandler"/> is false.
    /// </summary>
    public Task InvokeAsync()
    {
        if (_handler is null)
        {
            return Task.CompletedTask;
        }

        if (_isAsync)
        {
            return ((Func<Task>)_handler).Invoke();
        } ((Action)_handler).Invoke();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Generic counterpart to <see cref="GuiCallback"/> — a single argument is forwarded to the handler.
/// Accepts either <see cref="Action{T}"/> or <see cref="Func{T,Task}"/>.
/// </summary>
public readonly struct GuiCallback<T>
{
    private readonly object? _handler;
    private readonly bool _isAsync;

    private GuiCallback(object? handler, bool isAsync)
    {
        _handler = handler;
        _isAsync = isAsync;
    }

    public bool HasHandler => _handler is not null;

    public static implicit operator GuiCallback<T>(Action<T> handler) =>
        new(handler, isAsync: false);

    public static implicit operator GuiCallback<T>(Func<T, Task> handler) =>
        new(handler, isAsync: true);

    internal static GuiCallback<T> Combine(GuiCallback<T> first, GuiCallback<T> second)
    {
        if (!first.HasHandler)
        {
            return second;
        }
        if (!second.HasHandler)
        {
            return first;
        }

        return new Action<T>(arg =>
        {
            first.Invoke(arg);
            second.Invoke(arg);
        });
    }

    public void Invoke(T arg)
    {
        if (_handler is null)
        {
            return;
        }

        if (_isAsync)
        {
            _ = ((Func<T, Task>)_handler).Invoke(arg);
        }
        else
        {
            ((Action<T>)_handler).Invoke(arg);
        }
    }

    public Task InvokeAsync(T arg)
    {
        if (_handler is null)
        {
            return Task.CompletedTask;
        }

        if (_isAsync)
        {
            return ((Func<T, Task>)_handler).Invoke(arg);
        } ((Action<T>)_handler).Invoke(arg);
        return Task.CompletedTask;
    }
}
