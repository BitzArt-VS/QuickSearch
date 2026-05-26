using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.GameStatus;

internal class GameStatusDetail<T>(string name, Func<ICoreClientAPI, T?, GameStatusDetail<T>.ValueUpdateResult> resolve) : GameStatusDetail(name)
{
    private readonly List<GameStatusDetailsSubscription> _subscriptions = [];
    private T? _currentValue;
    private T? _notifiedValue;

    public override object? Value => _currentValue;

    public record struct ValueUpdateResult(bool HasChanged, T? Value);

    public override bool ShouldUpdate => _subscriptions.Count > 0;

    internal override IEnumerable<GameStatusDetailsSubscription> Update(ICoreClientAPI clientApi)
    {
        var (hasChanged, value) = resolve.Invoke(clientApi, _notifiedValue);
        _currentValue = value;

        if (hasChanged)
        {
            _notifiedValue = _currentValue;
            return _subscriptions;
        }

        return [];
    }

    internal override void AddSubscription(GameStatusDetailsSubscription subscription)
    {
        if (_subscriptions.Any(x => x.Equals(subscription)))
        {
            throw new InvalidOperationException("The subscription already exists in the subscription list.");
        }

        _subscriptions.Add(subscription);
    }

    internal override void RemoveSubscription(GameStatusDetailsSubscription subscription)
    {
        _subscriptions.Remove(subscription);

        if (_subscriptions.Count == 0)
        {
            _notifiedValue = (T?)(object?)null;
        }
    }
}

public abstract class GameStatusDetail(string name)
{
    public string Name { get; private init; } = name;

    public abstract object? Value { get; }

    public abstract bool ShouldUpdate { get; }

    internal abstract IEnumerable<GameStatusDetailsSubscription> Update(ICoreClientAPI clientApi);

    internal abstract void AddSubscription(GameStatusDetailsSubscription subscription);

    internal abstract void RemoveSubscription(GameStatusDetailsSubscription subscription);
}
