// =============================================================================
// ModConfigBridge.cs — Zero-dependency ModConfig integration for STS Companion
// =============================================================================
// If ModConfig mod is installed, exposes settings in the in-game Mods tab.
// If not installed, all GetValue calls return fallbacks (from Config.cs JSON).
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace StsCompanion;

internal static class ModConfigBridge
{
    private const string ModId = "StsCompanion";

    private static bool _available;
    private static bool _registered;
    private static Type? _apiType;
    private static Type? _entryType;
    private static Type? _configTypeEnum;

    internal static bool IsAvailable => _available;

    // ─── Call in Plugin.Initialize() ────────────────────────────

    internal static void DeferredRegister()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.ProcessFrame += OnNextFrame;
    }

    private static void OnNextFrame()
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        tree.ProcessFrame -= OnNextFrame;
        Detect();
        if (_available) Register();
    }

    // ─── Detect ModConfig via reflection ────────────────────────

    private static void Detect()
    {
        try
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .ToArray();

            _apiType = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ModConfigApi");
            _entryType = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ConfigEntry");
            _configTypeEnum = allTypes.FirstOrDefault(t => t.FullName == "ModConfig.ConfigType");
            _available = _apiType != null && _entryType != null && _configTypeEnum != null;

            if (_available)
                Plugin.Log("ModConfig detected — registering settings");
            else
                Plugin.Log("ModConfig not found — using config file only");
        }
        catch
        {
            _available = false;
        }
    }

    // ─── Register config entries ────────────────────────────────

    private static void Register()
    {
        if (_registered) return;
        _registered = true;

        try
        {
            var entries = BuildEntries();

            var displayNames = new Dictionary<string, string>
            {
                ["en"] = "STS Tracker Companion",
                ["zhs"] = "STS Tracker Companion",
            };

            var registerMethod = _apiType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "Register")
                .OrderByDescending(m => m.GetParameters().Length)
                .First();

            if (registerMethod.GetParameters().Length == 4)
            {
                registerMethod.Invoke(null, new object[]
                {
                    ModId,
                    displayNames["en"],
                    displayNames,
                    entries
                });
            }
            else
            {
                registerMethod.Invoke(null, new object[]
                {
                    ModId,
                    displayNames["en"],
                    entries
                });
            }

            Plugin.Log("ModConfig entries registered");
        }
        catch (Exception e)
        {
            Plugin.Log($"ModConfig registration failed: {e.Message}");
        }
    }

    // ─── Read/Write ─────────────────────────────────────────────

    internal static T GetValue<T>(string key, T fallback)
    {
        if (!_available) return fallback;
        try
        {
            var result = _apiType!.GetMethod("GetValue", BindingFlags.Public | BindingFlags.Static)
                ?.MakeGenericMethod(typeof(T))
                ?.Invoke(null, new object[] { ModId, key });
            return result != null ? (T)result : fallback;
        }
        catch { return fallback; }
    }

    internal static void SetValue(string key, object value)
    {
        if (!_available) return;
        try
        {
            _apiType!.GetMethod("SetValue", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new object[] { ModId, key, value });
        }
        catch { }
    }

    // ═══════════════════════════════════════════════════════════
    //  Config entries
    // ═══════════════════════════════════════════════════════════

    private static Array BuildEntries()
    {
        var cfg = Plugin.CurrentConfig!;
        var list = new List<object>();

        // ─── General ────────────────────────────────────────────

        list.Add(Entry(e =>
        {
            Set(e, "Label", "General");
            Set(e, "Type", EnumVal("Header"));
        }));

        list.Add(Entry(e =>
        {
            Set(e, "Key", "overlayEnabled");
            Set(e, "Label", "Show Score Overlay");
            Set(e, "Description", "Show card pick score badges during card selection");
            Set(e, "Type", EnumVal("Toggle"));
            Set(e, "DefaultValue", (object)cfg.OverlayEnabled);
            Set(e, "OnChanged", new Action<object>(v =>
            {
                cfg.OverlayEnabled = Convert.ToBoolean(v);
            }));
        }));

        list.Add(Entry(e =>
        {
            Set(e, "Key", "autoUploadRuns");
            Set(e, "Label", "Auto-Upload Runs");
            Set(e, "Description", "Automatically upload completed runs to STS Tracker");
            Set(e, "Type", EnumVal("Toggle"));
            Set(e, "DefaultValue", (object)cfg.AutoUploadRuns);
            Set(e, "OnChanged", new Action<object>(v =>
            {
                cfg.AutoUploadRuns = Convert.ToBoolean(v);
            }));
        }));

        list.Add(Entry(e =>
        {
            Set(e, "Key", "syncActiveRun");
            Set(e, "Label", "Sync Active Run");
            Set(e, "Description", "Sync current run state to STS Tracker on floor transitions");
            Set(e, "Type", EnumVal("Toggle"));
            Set(e, "DefaultValue", (object)cfg.SyncActiveRun);
            Set(e, "OnChanged", new Action<object>(v =>
            {
                cfg.SyncActiveRun = Convert.ToBoolean(v);
            }));
        }));

        list.Add(Entry(e => Set(e, "Type", EnumVal("Separator"))));

        // ─── Appearance ─────────────────────────────────────────

        list.Add(Entry(e =>
        {
            Set(e, "Label", "Appearance");
            Set(e, "Type", EnumVal("Header"));
        }));

        list.Add(Entry(e =>
        {
            Set(e, "Key", "badgeScale");
            Set(e, "Label", "Badge Scale");
            Set(e, "Description", "Scale multiplier for all score badges (1.0 = default)");
            Set(e, "Type", EnumVal("Slider"));
            Set(e, "DefaultValue", (object)cfg.BadgeScale);
            Set(e, "Min", 0.5f);
            Set(e, "Max", 2.0f);
            Set(e, "Step", 0.1f);
            Set(e, "Format", "F1");
            Set(e, "OnChanged", new Action<object>(v =>
            {
                cfg.BadgeScale = Convert.ToSingle(v);
            }));
        }));

        list.Add(Entry(e =>
        {
            Set(e, "Key", "tooltipScale");
            Set(e, "Label", "Tooltip Scale");
            Set(e, "Description", "Scale multiplier for score breakdown tooltips (1.0 = default)");
            Set(e, "Type", EnumVal("Slider"));
            Set(e, "DefaultValue", (object)cfg.TooltipScale);
            Set(e, "Min", 0.5f);
            Set(e, "Max", 2.0f);
            Set(e, "Step", 0.1f);
            Set(e, "Format", "F1");
            Set(e, "OnChanged", new Action<object>(v =>
            {
                cfg.TooltipScale = Convert.ToSingle(v);
            }));
        }));

        // ─── Pack into typed array ──────────────────────────────

        var result = Array.CreateInstance(_entryType!, list.Count);
        for (int i = 0; i < list.Count; i++)
            result.SetValue(list[i], i);
        return result;
    }

    // ═══════════════════════════════════════════════════════════
    //  Reflection helpers
    // ═══════════════════════════════════════════════════════════

    private static object Entry(Action<object> configure)
    {
        var inst = Activator.CreateInstance(_entryType!)!;
        configure(inst);
        return inst;
    }

    private static void Set(object obj, string name, object value)
        => obj.GetType().GetProperty(name)?.SetValue(obj, value);

    private static object EnumVal(string name)
        => Enum.Parse(_configTypeEnum!, name);
}
