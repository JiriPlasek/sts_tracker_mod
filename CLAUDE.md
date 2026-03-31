# Project Overview
STS Tracker Companion Mod — a Slay the Spire 2 C# mod (Godot 4.5 / MegaDot, .NET 9.0) that shows card pick recommendations in-game and auto-uploads runs to [STS Tracker](https://ststracker.app). Published on [Nexus Mods](https://www.nexusmods.com/slaythespire2/mods/340).

## Architecture
- **Entry point**: `Plugin.cs` — `[ModInitializer]`, loads config, inits Harmony patches and ScoreOverlay.
- **Patches**: Harmony postfix patches on game screen classes to intercept card selection events. See Harmony Gotchas below.
- **UI**: `ScoreOverlay.cs` — singleton that creates Godot UI nodes (badges, tooltips) and attaches them to card holders.
- **HTTP**: `HttpService.cs` — async calls to STS Tracker API with Bearer token auth.
- **Config**: `sts_companion_config.cfg` (JSON) loaded from same directory as DLL.
- **Inspect tool**: `tools/` — decompilation utility for exploring game DLL APIs. Run with `dotnet run -- "Full.Type.Name"` or `dotnet run -- --search "keyword"`.

## Harmony Gotchas
- **Can't patch inherited methods on subclasses**: If a method isn't explicitly overridden by the subclass, Harmony can't find it. Patch the base class instead (e.g., `NCardGridSelectionScreen._ExitTree` not `NSimpleCardSelectScreen._ExitTree`).
- **Can't patch Godot engine methods**: `SceneTree._Process`, `Node._Input` etc. are native — Harmony can't hook them. Use game-level methods instead.
- **PatchAll auto-discovers all `[HarmonyPatch]` classes** in the assembly. One bad patch attribute kills the entire mod. Test incrementally.

## Game Card Selection Screens
The game uses different screen classes for different card selection contexts:
- `NCardRewardSelectionScreen` — post-combat 3-card reward. Container: `UI/CardRow` with `NGridCardHolder` children.
- `NChooseACardSelectionScreen` — event "choose a card to add". Container: `CardRow` with `NGridCardHolder` children.
- `NSimpleCardSelectScreen` (extends `NCardGridSelectionScreen`) — "choose N out of M" grid (e.g., '?' room events). Container: `%CardGrid` (`NCardGrid`) with holders nested in row sub-containers. **Grid sorts cards** so visual order ≠ input order.
- `NMerchantInventory` — shop. Containers: `%CharacterCards` and `%ColorlessCards` with `NMerchantCard` children.

## Key Gotchas
- **Card order**: The game can reorder cards (grid sorts alphabetically, shop has its own layout). Always match badges to cards by reading `holder.CardModel.Id`, never by position index.
- **Async grid init**: `NCardGrid.InitGrid()` is async — card holders don't exist yet when `_Ready` fires. Hook `AfterOverlayOpened` + add a delay (0.5s timer) before attaching badges.
- **Deferred UI updates**: Always use `Callable.From(...).CallDeferred()` when attaching badges to ensure the node tree is stable.
- **Config file name**: `sts_companion_config.cfg` (not `config.json`). Auto-created with defaults if missing.

## Build & Deploy
```bash
dotnet build -c Release
# DLL output: bin/Release/net9.0/StsCompanion.dll
# Deploy to: Steam/.../Slay the Spire 2/mods/StsCompanion/
```

# Project Rules

## Git
- **Never commit, push, or perform any git operations.** The user handles all git themselves.

## Code Style
- Use arrow function declarations where idiomatic C# allows.
- Wrap all patch logic in try/catch — an unhandled exception in a Harmony patch can crash the game.
