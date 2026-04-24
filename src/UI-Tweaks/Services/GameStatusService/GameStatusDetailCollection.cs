using BitzArt.UI.Tweaks.GameStatus;
using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace BitzArt.UI.Tweaks.Services;

internal sealed class GameStatusDetailCollection
{
    private readonly List<GameStatusDetail> _details = [];
    private readonly Dictionary<string, GameStatusDetail> _lookup;
    private readonly Lock _lock = new();

    public IEnumerable<GameStatusDetail> Details => GetValues();

    public GameStatusDetailCollection()
    {
        _lookup = [];

        Add<float?>("player-health-current", (clientApi, oldValue) =>
        {
            const float hysteresis = 0.1f;
            const int decimalPlaces = 1;

            var playerEntity = clientApi.World?.Player?.Entity;
            var healthAttribute = playerEntity?.WatchedAttributes?.GetTreeAttribute("health");
            var value = healthAttribute?.TryGetFloat("currenthealth");
            float? truncated = value is null ? null : (float)Math.Floor(value.Value * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);

            if (truncated is null || oldValue is null)
            {
                return new(oldValue != truncated, truncated);
            }

            return new(Math.Abs(truncated.Value - oldValue.Value) >= hysteresis, truncated);
        });

        Add<float?>("player-health-max", (clientApi, oldValue) =>
        {
            const float hysteresis = 0.1f;
            const int decimalPlaces = 1;

            var playerEntity = clientApi.World?.Player?.Entity;
            var healthAttribute = playerEntity?.WatchedAttributes?.GetTreeAttribute("health");
            var value = healthAttribute?.TryGetFloat("maxhealth");
            float? truncated = value is null ? null : (float)Math.Floor(value.Value * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);
            
            if (truncated is null || oldValue is null)
            {
                return new(oldValue != truncated, truncated);
            }

            return new(Math.Abs(truncated.Value - oldValue.Value) >= hysteresis, truncated);
        });

        Add<float?>("player-health-percent", (clientApi, oldValue) =>
        {
            const float hysteresis = 1.0f;
            const int decimalPlaces = 0;

            var playerEntity = clientApi.World?.Player?.Entity;
            var healthAttribute = playerEntity?.WatchedAttributes?.GetTreeAttribute("health");
            var currentHealth = healthAttribute?.TryGetFloat("currenthealth");
            var maxHealth = healthAttribute?.TryGetFloat("maxhealth");
            float? percent = null;

            if (currentHealth.HasValue && maxHealth.HasValue && maxHealth.Value > 0)
            {
                var value = currentHealth.Value / maxHealth.Value * 100.0f;
                percent = (float)Math.Floor(value * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);
            }

            if (percent is null || oldValue is null)
            {
                return new(oldValue != percent, percent);
            }

            return new(Math.Abs(percent.Value - oldValue.Value) >= hysteresis, percent);
        });

        Add<float?>("player-satiety-current", (clientApi, oldValue) =>
        {
            const float hysteresis = 0.1f;
            const int decimalPlaces = 0;

            var playerEntity = clientApi.World?.Player?.Entity;
            var hungerAttribute = playerEntity?.WatchedAttributes?.GetTreeAttribute("hunger");
            var value = hungerAttribute?.TryGetFloat("currentsaturation");
            float? truncated = value is null ? null : (float)Math.Floor(value.Value * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);
            if (truncated is null || oldValue is null)
            {
                return new(oldValue != truncated, truncated);
            }
            return new(Math.Abs(truncated.Value - oldValue.Value) >= hysteresis, truncated);
        });

        Add<float?>("player-satiety-max", (clientApi, oldValue) =>
        {
            const float hysteresis = 0.1f;
            const int decimalPlaces = 0;
            var playerEntity = clientApi.World?.Player?.Entity;
            var hungerAttribute = playerEntity?.WatchedAttributes?.GetTreeAttribute("hunger");
            var value = hungerAttribute?.TryGetFloat("maxsaturation");
            float? truncated = value is null ? null : (float)Math.Floor(value.Value * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);
            if (truncated is null || oldValue is null)
            {
                return new(oldValue != truncated, truncated);
            }
            return new(Math.Abs(truncated.Value - oldValue.Value) >= hysteresis, truncated);
        });

        Add<float?>("player-satiety-percent", (clientApi, oldValue) =>
        {
            const float hysteresis = 1.0f;
            const int decimalPlaces = 0;
            var playerEntity = clientApi.World?.Player?.Entity;
            var hungerAttribute = playerEntity?.WatchedAttributes?.GetTreeAttribute("hunger");
            var currentSatiety = hungerAttribute?.TryGetFloat("currentsaturation");
            var maxSatiety = hungerAttribute?.TryGetFloat("maxsaturation");
            float? percent = null;
            if (currentSatiety.HasValue && maxSatiety.HasValue && maxSatiety.Value > 0)
            {
                var value = currentSatiety.Value / maxSatiety.Value * 100.0f;
                percent = (float)Math.Floor(value * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);
            }
            if (percent is null || oldValue is null)
            {
                return new(oldValue != percent, percent);
            }
            return new(Math.Abs(percent.Value - oldValue.Value) >= hysteresis, percent);
        });

        Add<float?>("player-satiety-hunger", (clientApi, oldValue) =>
        {
            const float hysteresis = 0.1f;
            const int decimalPlaces = 0;
            var playerEntity = clientApi.World?.Player?.Entity;
            var hungerRate = playerEntity is null ? (float?)null : (float)playerEntity.Stats.GetBlended("hungerrate");
            float? percent = hungerRate is null ? null : (float)Math.Floor(hungerRate.Value * 100.0f * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);
            
            if (percent is null || oldValue is null)
            {
                return new(oldValue != percent, percent);
            }

            return new(Math.Abs(percent.Value - oldValue.Value) >= hysteresis, percent);
        });

        Add<float?>("player-temporal-stability", (clientApi, oldValue) =>
        {
            const float hysteresis = 1.0f;
            const int decimalPlaces = 0;

            var playerEntity = clientApi.World?.Player?.Entity;
            var temporalStability = playerEntity is null ? (float?)null : (float)playerEntity.WatchedAttributes.GetDouble("temporalStability");
            float? percent = temporalStability is null ? null : (float)Math.Floor(temporalStability.Value * 100.0f * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);
            
            if (percent is null || oldValue is null)
            {
                return new(oldValue != percent, percent);
            }

            return new(Math.Abs(percent.Value - oldValue.Value) >= hysteresis, percent);
        });

        Add<PlayerCoordinates?>("player-location-coordinates", (clientApi, oldValue) =>
        {
            const float hysteresis = 1.0f;

            var playerEntity = clientApi.World?.Player?.Entity;
            var absolutePos = playerEntity?.Pos.AsBlockPos;
            var spawnPos = clientApi.World?.DefaultSpawnPosition.AsBlockPos;

            if (absolutePos is null || spawnPos is null)
            {
                return new(oldValue != null, null);
            }

            int relativeX = absolutePos.X - spawnPos.X;
            int relativeZ = absolutePos.Z - spawnPos.Z;
            var coordinates = new PlayerCoordinates(relativeX, absolutePos.Y, relativeZ);

            if (oldValue is null)
            {
                return new(true, coordinates);
            }

            var anyChanged = Math.Abs(relativeX - oldValue.Value.X) >= hysteresis
                || Math.Abs(absolutePos.Y - oldValue.Value.Y) >= hysteresis
                || Math.Abs(relativeZ - oldValue.Value.Z) >= hysteresis;

            return new(anyChanged, coordinates);
        });

        Add<float?>("player-location-coordinates-x", (clientApi, oldValue) =>
        {
            const float hysteresis = 1.0f;

            var playerEntity = clientApi.World?.Player?.Entity;
            var absolutePos = playerEntity?.Pos.AsBlockPos;
            var spawnPos = clientApi.World?.DefaultSpawnPosition.AsBlockPos;

            if (absolutePos is null || spawnPos is null)
            {
                return new(oldValue != null, null);
            }

            int relativeX = absolutePos.X - spawnPos.X;

            if (oldValue is null)
            {
                return new(true, relativeX);
            }

            return new(Math.Abs(relativeX - oldValue.Value) >= hysteresis, relativeX);
        });

        Add<float?>("player-location-coordinates-y", (clientApi, oldValue) =>
        {
            const float hysteresis = 1.0f;

            var playerEntity = clientApi.World?.Player?.Entity;
            var absolutePos = playerEntity?.Pos.AsBlockPos;

            if (absolutePos is null)
            {
                return new(oldValue != null, null);
            }

            float y = absolutePos.Y;

            if (oldValue is null)
            {
                return new(true, y);
            }

            return new(Math.Abs(y - oldValue.Value) >= hysteresis, y);
        });

        Add<float?>("player-location-coordinates-z", (clientApi, oldValue) =>
        {
            const float hysteresis = 1.0f;

            var playerEntity = clientApi.World?.Player?.Entity;
            var absolutePos = playerEntity?.Pos.AsBlockPos;
            var spawnPos = clientApi.World?.DefaultSpawnPosition.AsBlockPos;

            if (absolutePos is null || spawnPos is null)
            {
                return new(oldValue != null, null);
            }

            int relativeZ = absolutePos.Z - spawnPos.Z;

            if (oldValue is null)
            {
                return new(true, relativeZ);
            }

            return new(Math.Abs(relativeZ - oldValue.Value) >= hysteresis, relativeZ);
        });

        Add<float?>("player-location-temperature-celsius", (clientApi, oldValue) =>
        {
            const float hysteresis = 1.0f;
            const int decimalPlaces = 0;

            var playerEntity = clientApi.World?.Player?.Entity;
            var absolutePos = playerEntity?.Pos.AsBlockPos;

            if (absolutePos is null)
            {
                return new(oldValue != null, null);
            }

            var climate = clientApi.World?.BlockAccessor.GetClimateAt(absolutePos);
            float? value = climate is null ? null : (float)Math.Floor(climate.Temperature * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);

            if (value is null || oldValue is null)
            {
                return new(oldValue != value, value);
            }

            return new(Math.Abs(value.Value - oldValue.Value) >= hysteresis, value);
        });

        Add<float?>("player-location-temperature-fahrenheit", (clientApi, oldValue) =>
        {
            const float hysteresis = 1.0f;
            const int decimalPlaces = 0;

            var playerEntity = clientApi.World?.Player?.Entity;
            var absolutePos = playerEntity?.Pos.AsBlockPos;

            if (absolutePos is null)
            {
                return new(oldValue != null, null);
            }

            var climate = clientApi.World?.BlockAccessor.GetClimateAt(absolutePos);
            float? value = climate is null ? null : (float)Math.Floor(ToFahrenheit(climate.Temperature) * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);

            if (value is null || oldValue is null)
            {
                return new(oldValue != value, value);
            }

            return new(Math.Abs(value.Value - oldValue.Value) >= hysteresis, value);
        });

        Add<float?>("player-location-average-yearly-temperature-celsius", (clientApi, oldValue) =>
        {
            const float hysteresis = 1.0f;
            const int decimalPlaces = 0;

            var playerEntity = clientApi.World?.Player?.Entity;
            var absolutePos = playerEntity?.Pos.AsBlockPos;

            if (absolutePos is null)
            {
                return new(oldValue != null, null);
            }

            var climate = clientApi.World?.BlockAccessor.GetClimateAt(absolutePos);
            float? value = climate is null ? null : (float)Math.Floor(climate.WorldGenTemperature * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);

            if (value is null || oldValue is null)
            {
                return new(oldValue != value, value);
            }

            return new(Math.Abs(value.Value - oldValue.Value) >= hysteresis, value);
        });

        Add<float?>("player-location-average-yearly-temperature-fahrenheit", (clientApi, oldValue) =>
        {
            const float hysteresis = 1.0f;
            const int decimalPlaces = 0;

            var playerEntity = clientApi.World?.Player?.Entity;
            var absolutePos = playerEntity?.Pos.AsBlockPos;

            if (absolutePos is null)
            {
                return new(oldValue != null, null);
            }

            var climate = clientApi.World?.BlockAccessor.GetClimateAt(absolutePos);
            float? value = climate is null ? null : (float)Math.Floor(ToFahrenheit(climate.WorldGenTemperature) * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);

            if (value is null || oldValue is null)
            {
                return new(oldValue != value, value);
            }

            return new(Math.Abs(value.Value - oldValue.Value) >= hysteresis, value);
        });

        Add<float?>("player-location-average-precipitation-percent", (clientApi, oldValue) =>
        {
            const float hysteresis = 0.1f;
            const int decimalPlaces = 1;

            var playerEntity = clientApi.World?.Player?.Entity;
            var absolutePos = playerEntity?.Pos.AsBlockPos;

            if (absolutePos is null)
            {
                return new(oldValue != null, null);
            }

            var climate = clientApi.World?.BlockAccessor.GetClimateAt(absolutePos);
            float? value = climate is null ? null : (float)Math.Floor(climate.Rainfall * 100.0f * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);

            if (value is null || oldValue is null)
            {
                return new(oldValue != value, value);
            }

            return new(Math.Abs(value.Value - oldValue.Value) >= hysteresis, value);
        });

        Add<float?>("player-location-average-forestation-percent", (clientApi, oldValue) =>
        {
            const float hysteresis = 0.1f;
            const int decimalPlaces = 1;

            var playerEntity = clientApi.World?.Player?.Entity;
            var absolutePos = playerEntity?.Pos.AsBlockPos;

            if (absolutePos is null)
            {
                return new(oldValue != null, null);
            }

            var climate = clientApi.World?.BlockAccessor.GetClimateAt(absolutePos);
            float? value = climate is null ? null : (float)Math.Floor(climate.ForestDensity * 100.0f * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);

            if (value is null || oldValue is null)
            {
                return new(oldValue != value, value);
            }

            return new(Math.Abs(value.Value - oldValue.Value) >= hysteresis, value);
        });

        Add<float?>("player-location-average-shrubbery-percent", (clientApi, oldValue) =>
        {
            const float hysteresis = 0.1f;
            const int decimalPlaces = 1;

            var playerEntity = clientApi.World?.Player?.Entity;
            var absolutePos = playerEntity?.Pos.AsBlockPos;

            if (absolutePos is null)
            {
                return new(oldValue != null, null);
            }

            var climate = clientApi.World?.BlockAccessor.GetClimateAt(absolutePos);
            float? value = climate is null ? null : (float)Math.Floor(climate.ShrubDensity * 100.0f * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);

            if (value is null || oldValue is null)
            {
                return new(oldValue != value, value);
            }

            return new(Math.Abs(value.Value - oldValue.Value) >= hysteresis, value);
        });

        SystemTemporalStability? temporalStabilitySystem = null;

        Add<float?>("player-location-temporal-stability", (clientApi, oldValue) =>
        {
            const float hysteresis = 1.0f;
            const int decimalPlaces = 0;

            var playerEntity = clientApi.World?.Player?.Entity;
            var absolutePos = playerEntity?.Pos.AsBlockPos;

            if (absolutePos is null)
            {
                return new(oldValue != null, null);
            }

            temporalStabilitySystem ??= clientApi.ModLoader.GetModSystem<SystemTemporalStability>();
            double? rawValue = temporalStabilitySystem?.GetTemporalStability(absolutePos);
            float? value = rawValue is null ? null : (float)Math.Floor((float)rawValue.Value * 100.0f * (float)Math.Pow(10, decimalPlaces)) / (float)Math.Pow(10, decimalPlaces);

            if (value is null || oldValue is null)
            {
                return new(oldValue != value, value);
            }

            return new(Math.Abs(value.Value - oldValue.Value) >= hysteresis, value);
        });

        Add<DateTime?>("world-date-time", (clientApi, oldValue) =>
        {
            var calendar = clientApi.World?.Calendar;

            if (calendar is null)
            {
                return new(oldValue != null, null);
            }

            var year = calendar.Year + 1;
            var month = calendar.Month;
            var dayOfMonth = calendar.DayOfYear - ((month - 1) * calendar.DaysPerMonth) + 1;

            var date = new DateOnly(year, month, dayOfMonth);
            var elapsedSeconds = (int)(calendar.ElapsedSeconds - ((long)calendar.ElapsedHours * 60 * 60));
            var time = new TimeOnly((int)calendar.HourOfDay, elapsedSeconds / 60);

            var worldDateTime = new DateTime(date, time);

            return new(oldValue != worldDateTime, worldDateTime);
        });
    }

    private record struct PlayerCoordinates(float X, float Y, float Z);

    private static float ToFahrenheit(float celsius) => celsius * 9 / 5 + 32;

    private List<GameStatusDetail> GetValues()
    {
        lock (_lock)
        {
            return _details;
        }
    }

    public void Add<T>(string name, Func<ICoreClientAPI, T?, GameStatusDetail<T>.ValueUpdateResult> resolve)
    {
        var detail = new GameStatusDetail<T>(name, resolve);

        lock (_lock)
        {
            if (!_lookup.TryAdd(name, detail))
            {
                throw new ArgumentException($"A detail with name {detail.Name} already exists.");
            }

            _details.Add(detail);
        }
    }

    public GameStatusDetail Get(string name)
    {
        lock (_lock)
        {
            return _lookup[name];
        }
    }
}
