using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using StsCompanion.UI;

namespace StsCompanion;

[ModInitializer(nameof(Initialize))]
public static class Plugin
{
    private static Harmony? _harmony;
    public static Config? CurrentConfig { get; private set; }

    public static void Initialize()
    {
        Log("STS Tracker Companion loading...");

        var config = Config.Load();
        if (config == null)
        {
            Log("Failed to load config.json — mod disabled.");
            return;
        }

        if (string.IsNullOrEmpty(config.ApiToken))
        {
            Log("No API token configured. Set your token in config.json.");
            return;
        }

        CurrentConfig = config;
        HttpService.Init(config);

        _harmony = new Harmony("com.ststracker.companion");
        try
        {
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        catch (System.Exception ex)
        {
            Log($"Harmony PatchAll failed: {ex.Message}");
            Log("Some features may not work. Continuing with what loaded.");
        }

        ScoreOverlay.Create();

        // Register with ModConfig (if installed) for in-game settings UI
        ModConfigBridge.DeferredRegister();

        Log("STS Tracker Companion loaded successfully.");
    }

    public static void Log(string message)
    {
        GD.Print($"[StsCompanion] {message}");
    }
}
