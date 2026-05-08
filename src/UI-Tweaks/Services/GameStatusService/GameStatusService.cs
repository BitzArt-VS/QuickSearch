using BitzArt.UI.Tweaks.GameStatus;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Services;

/// <summary>
/// A service that aggregates game status updates
/// and allows other components to dynamically subscribe to arbitrary subsets of these updates,
/// allowing updates from different sources in a single callback.
/// </summary>
/// <remarks>
/// This service runs on a separate thread, and will notify subscribers of changes from that thread.
/// Dispatching to the UI thread is necessary when doing any UI updates in the callback.
/// </remarks>
public sealed partial class GameStatusService : IDisposable
{
    private readonly ICoreClientAPI _clientApi;

    private readonly CultureInfo _formatCulture;

    private long? _tickListenerId;
    private CancellationTokenSource? _updateThreadCts;
    private Thread? _updateThread;

    private readonly GameStatusDetailCollection _details;

    public GameStatusService(ICoreClientAPI clientApi)
    {
        _clientApi = clientApi;
        _details = new();

        string[] months = [.. Enumerable.Range(1, 12).Select(i => Lang.Get("month-" + (EnumMonth)i)), string.Empty];

        _formatCulture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        _formatCulture.DateTimeFormat.MonthNames = months;
        _formatCulture.DateTimeFormat.AbbreviatedMonthNames = [.. months.Select(m => m.Length > 3 ? m[..3] : m)];
        _formatCulture.DateTimeFormat.MonthGenitiveNames = _formatCulture.DateTimeFormat.MonthNames;
        _formatCulture.DateTimeFormat.AbbreviatedMonthGenitiveNames = _formatCulture.DateTimeFormat.AbbreviatedMonthNames;

        _tickListenerId = _clientApi.Event.RegisterGameTickListener(_ =>
        {
            if (_clientApi.World?.Player?.Entity?.WatchedAttributes is null)
            {
                return;
            }

            if (_updateThread is not null)
            {
                _clientApi.Logger.Warning("GameStatusService: Update thread already running.");

                _clientApi.Event.UnregisterGameTickListener(_tickListenerId!.Value);
                return;
            }

            _updateThreadCts = new();
            var token = _updateThreadCts.Token;

            _updateThread = new Thread(async () => await RunUpdateLoopAsync(token))
            {
                IsBackground = true,
                Name = "UI-Tweaks Status Updates Thread"
            };

            _updateThread.Start();

            _clientApi.Event.UnregisterGameTickListener(_tickListenerId!.Value);

        }, 100);
    }

    public void Dispose()
    {
        if (_tickListenerId is not null)
        {
            _clientApi.Event.UnregisterGameTickListener(_tickListenerId.Value);
            _tickListenerId = null;
        }

        _updateThreadCts?.Cancel();
        _updateThreadCts?.Dispose();
        _updateThreadCts = null;

        _updateThread = null;

        GC.SuppressFinalize(this);
    }

    public GameStatusDetailsSubscription? Subscribe(string format, Action<string> callback)
    {
        var placeholderRegex = GetFormatPlaceholderRegex();
        var matches = placeholderRegex.Matches(format);

        // No subscription is necessary if
        // there are no variable placeholders in the format string.
        if (matches.Count == 0)
        {
            return null;
        }

        var variableNames = matches.Select(m => m.Groups["name"].Value).ToList();

        int i = 0;
        var resultingFormat = placeholderRegex.Replace(format, match =>
        {
            // match.Groups["format"].Value will contain the alignment/format string (e.g. ":MMMM dd, yyyy")
            // If there is no format string, it will be empty, cleanly resulting in just {0}, {1}, etc.
            return $"{{{i++}{match.Groups["format"].Value}}}";
        });

        return Subscribe(variableNames, (values) =>
        {
            callback.Invoke(string.Format(_formatCulture, resultingFormat, [.. values]));
        });
    }

    // Captures the variable name in the "name" group, and any following alignment/formatting in the "format" group
    [GeneratedRegex(@"\{(?<name>[a-zA-Z0-9\-]+)(?<format>[,:][^}]+)?\}")]
    private static partial Regex GetFormatPlaceholderRegex();

    public GameStatusDetailsSubscription Subscribe(List<string> parameterNames, Action<object?[]> callback)
    {
        var details = parameterNames.Select(x => _details.Get(x)).ToList();
        var subscription = new GameStatusDetailsSubscription(details, callback);

        foreach (var detail in details)
        {
            detail.AddSubscription(subscription);
        }

        Notify(subscription);

        return subscription;
    }

    private async Task RunUpdateLoopAsync(CancellationToken token)
    {
        _clientApi.Logger.Debug("GameStatusService: Update thread started.");

        while (!token.IsCancellationRequested)
        {
            try
            {
                RunUpdateIteration();

                await Task.Delay(50, CancellationToken.None);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _clientApi.Logger.Error("Error updating game status details: " + ex);

                await Task.Delay(1000, CancellationToken.None);
            }
        }

        _clientApi.Logger.Debug("GameStatusService: Update thread exiting.");
    }

    private void RunUpdateIteration()
    {
        var toNotify = new HashSet<GameStatusDetailsSubscription>();

        foreach (var detail in _details.Details)
        {
            if (!detail.ShouldUpdate)
            {
                continue;
            }

            try
            {
                foreach (var subscriptionToNotify in detail.Update(_clientApi))
                {
                    toNotify.Add(subscriptionToNotify);
                }
            }
            catch (Exception ex)
            {
                _clientApi.Logger.Error($"Error updating game status detail '{detail.Name}':");
                _clientApi.Logger.Error(ex);
            }
        }

        foreach (var subscription in toNotify)
        {
            try
            {
                Notify(subscription);
            }
            catch (Exception ex)
            {
                _clientApi.Logger.Error("An unexpected error occurred while notifying a game status subscription:");
                _clientApi.Logger.Error(ex);
            }
        }
    }

    private static void Notify(GameStatusDetailsSubscription subscription)
    {
        object?[] values = new object?[subscription.Details.Count];

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
}
