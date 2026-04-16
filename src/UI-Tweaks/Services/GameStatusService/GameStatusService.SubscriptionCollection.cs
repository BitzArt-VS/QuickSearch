using System;
using System.Collections.Generic;
using System.Linq;

namespace BitzArt.UI.Tweaks.Services;

public partial class GameStatusService
{
    private class SubscriptionCollection
    {
        private readonly Dictionary<DetailRecord, List<DetailsSubscription>> _lookup = [];

        public void Subscribe(List<DetailRecord> details, Action<object[]> callback)
        {
            var subscription = new DetailsSubscription(details, callback);

            foreach (var detail in details)
            {
                if (!_lookup.TryGetValue(detail, out var subscribers))
                {
                    subscribers = [];
                    _lookup[detail] = subscribers;
                }

                subscribers.Add(subscription);
            }

            TryNotify(subscription);
        }

        private static void TryNotify(DetailsSubscription subscription)
        {
            object[] values = new object[subscription.Details.Count];

            for (int i = 0; i < subscription.Details.Count; i++)
            {
                if (subscription.Details[i].Value is null)
                {
                    return;
                }

                values[i] = subscription.Details[i].Value;
            }

            subscription.Callback.Invoke(values);
        }

        public void Unsubscribe(Action<object[]> callback)
        {
            foreach (var subscribers in _lookup.Values)
            {
                subscribers.RemoveAll(s => s.Callback == callback);
            }
        }

        public void OnUpdate(IEnumerable<DetailRecord> details)
        {
            var subscriptions = details.SelectMany(d =>
            {
                if (!_lookup.TryGetValue(d, out var subscriptions))
                {
                    return [];
                }

                return subscriptions.ToHashSet();
            }).ToHashSet();

            foreach (var subscription in subscriptions)
            {
                object[] values = new object[subscription.Details.Count];

                for (int i = 0; i < subscription.Details.Count; i++)
                {
                    var detail = subscription.Details[i];

                    if (detail.Value is null)
                    {
                        return;
                    }

                    values[i] = detail.Value;
                }

                subscription.Callback.Invoke(values);
            }
        }
    }
}
