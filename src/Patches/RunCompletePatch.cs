using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace StsCompanion.Patches;

[HarmonyPatch(typeof(RunHistorySaveManager), nameof(RunHistorySaveManager.SaveHistory))]
public static class RunCompletePatch
{
    [HarmonyPostfix]
    public static void Postfix(RunHistory history)
    {
        try
        {
            Plugin.Log($"Run completed (start_time={history.StartTime}). Uploading...");

            // Serialize using the game's own serializer (correct snake_case JSON)
            var json = JsonSerializationUtility.ToJson(history);
            var filename = $"{history.StartTime}.run";

            _ = UploadRun(filename, json);
        }
        catch (Exception ex)
        {
            Plugin.Log($"RunCompletePatch error: {ex.Message}");
        }
    }

    private static async Task UploadRun(string filename, string content)
    {
        try
        {
            var result = await HttpService.UploadRun(filename, content);

            if (result != null)
            {
                Plugin.Log($"Upload result: {result.Imported} imported, {result.Skipped} skipped, {result.Errors.Length} errors.");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log($"Run upload error: {ex.Message}");
        }
    }
}
