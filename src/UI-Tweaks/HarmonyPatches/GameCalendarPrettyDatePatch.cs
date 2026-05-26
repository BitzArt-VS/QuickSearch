using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.Common;

namespace BitzArt.UI.Tweaks;

internal static class GameCalendarPrettyDatePatch
{
    private static readonly MethodInfo AdjustCalendarYearMethod = AccessTools.Method(typeof(GameCalendarPrettyDatePatch), nameof(AdjustCalendarYear));

    public static void Patch(Harmony harmony)
    {
        var original = AccessTools.Method(typeof(GameCalendar), nameof(GameCalendar.PrettyDate));
        var transpiler = AccessTools.Method(typeof(GameCalendarPrettyDatePatch), nameof(Transpiler));

        harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
    }

    public static void Unpatch(Harmony harmony)
    {
        var original = AccessTools.Method(typeof(GameCalendar), nameof(GameCalendar.PrettyDate));

        harmony.Unpatch(original, HarmonyPatchType.Transpiler, harmony.Id);
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getYear = AccessTools.PropertyGetter(typeof(GameCalendar), nameof(GameCalendar.Year));

        foreach (var instruction in instructions)
        {
            yield return instruction;

            if (instruction.Calls(getYear))
            {
                yield return new CodeInstruction(OpCodes.Call, AdjustCalendarYearMethod);
            }
        }
    }

    private static int AdjustCalendarYear(int calendarYear)
    {
        return calendarYear + 1;
    }
}
