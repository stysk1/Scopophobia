using HarmonyLib;
using Scopophobia;

[HarmonyPatch(typeof(StartOfRound))]
internal static class StartOfRoundPatch
{
    [HarmonyPatch(typeof(StartOfRound), "Start")]
    [HarmonyPostfix]
    private static void StartPatch()
    {
        EnemyDataManager.Initialize();
    }
}
