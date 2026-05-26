namespace BitzArt.UI.Tweaks.GameStatus;

public record GameStatusDetailsSubscription : IDisposable
{
    public List<GameStatusDetail> Details { get; init; }
    public Action<object?[]> Callback { get; init; }

    public void Dispose()
    {
        foreach (var detail in Details)
        {
            detail.RemoveSubscription(this);
        }

        GC.SuppressFinalize(this);
    }

    internal GameStatusDetailsSubscription(List<GameStatusDetail> details, Action<object?[]> callback)
    {
        Details = details;
        Callback = callback;
    }
}
