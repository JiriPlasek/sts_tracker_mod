using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace StsCompanion.Patches;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Open))]
public static class FloorTransitionPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        try
        {
            _ = SyncActiveRun();
        }
        catch (Exception ex)
        {
            Plugin.Log($"FloorTransitionPatch error: {ex.Message}");
        }
    }

    private static async Task SyncActiveRun()
    {
        try
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState == null) return;

            // Serialize using the game's own serializer (produces correct snake_case JSON)
            var serializableRun = RunManager.Instance.ToSave(null);
            var saveJson = await JsonSerializationUtility.SerializeAsync(serializableRun);

            await HttpService.SyncActiveRun(saveJson);
            Plugin.Log("Active run synced.");
        }
        catch (Exception ex)
        {
            Plugin.Log($"Active run sync error: {ex.Message}");
        }
    }
}
