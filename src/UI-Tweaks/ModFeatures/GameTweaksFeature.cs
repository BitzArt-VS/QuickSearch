using BitzArt.UI.Tweaks.Config;
using HarmonyLib;
using System.ComponentModel;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks;

public class GameTweaksFeature(UiTweaksModSystem modSystem, GameTweaksConfig config)
    : ModSystemFeature<UiTweaksModSystem, GameTweaksConfig>(modSystem, config)
{
    private const string HarmonyId = $"{Constants.ModId}.game-tweaks";

    private Harmony? _harmony;
    private bool _isCalendarYearPatchApplied;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void Start(ICoreClientAPI clientApi)
    {
        _harmony = new Harmony(HarmonyId);
        Config.PropertyChanged += OnConfigPropertyChanged;

        ApplyCalendarYearPatchState();
    }

    public override void Dispose()
    {
        Config.PropertyChanged -= OnConfigPropertyChanged;
        RemoveCalendarYearPatch();

        _harmony = null;

        GC.SuppressFinalize(this);
    }

    private void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(GameTweaksConfig.CorrectCalendarYear))
        {
            ApplyCalendarYearPatchState();
        }
    }

    private void ApplyCalendarYearPatchState()
    {
        if (Config.CorrectCalendarYear)
        {
            AddCalendarYearPatch();
            return;
        }

        RemoveCalendarYearPatch();
    }

    private void AddCalendarYearPatch()
    {
        if (_harmony is null || _isCalendarYearPatchApplied)
        {
            return;
        }

        GameCalendarPrettyDatePatch.Patch(_harmony);
        _isCalendarYearPatchApplied = true;
    }

    private void RemoveCalendarYearPatch()
    {
        if (_harmony is null || !_isCalendarYearPatchApplied)
        {
            return;
        }

        GameCalendarPrettyDatePatch.Unpatch(_harmony);
        _isCalendarYearPatchApplied = false;
    }
}
