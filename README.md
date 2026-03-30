# STS Tracker Companion Mod

A Slay the Spire 2 mod that provides real-time card pick recommendations and automatic run uploads to [STS Tracker](https://ststracker.app).

## Features

- **In-game card ratings** — See scores below each card on reward screens, event card choices, and shops. Hover for a detailed breakdown of synergy, archetype fit, and more.
- **Auto-upload runs** — Completed runs are automatically uploaded to your STS Tracker account
- **Active run sync** — Your current run state syncs to the browser companion page in real-time

## Recommended Mods

- [UnifiedSavePath](https://www.nexusmods.com/slaythespire2/mods/6) — Consolidates save files across Steam profiles. Recommended if you play on multiple profiles.

### Manual Install

1. Download the mod from [Nexus Mods](https://www.nexusmods.com/slaythespire2)
2. Navigate to your STS2 mods directory (e.g., `Steam/steamapps/common/Slay the Spire 2/mods`)
3. Copy `StsCompanion` folder inside `mods/`

## Setup

1. Go to [STS Tracker](https://ststracker.app) and log in with Steam
2. Go to Settings and generate an API token
3. Open `sts_companion_config.cfg` in the mod folder and paste your token:

```json
{
  "apiToken": "sts_your_token_here",
  "apiUrl": "https://ststracker.app",
  "overlayEnabled": true,
  "autoUploadRuns": true,
  "syncActiveRun": true
}
```

4. Launch STS2 — the mod loads automatically

## Configuration

| Setting          | Default                    | Description                               |
| ---------------- | -------------------------- | ----------------------------------------- |
| `apiToken`       | `""`                       | Your STS Tracker API token (required)     |
| `apiUrl`         | `"https://ststracker.app"` | API server URL                            |
| `overlayEnabled` | `true`                     | Show score overlay on card selections     |
| `autoUploadRuns` | `true`                     | Auto-upload completed runs                |
| `syncActiveRun`  | `true`                     | Sync active run to browser companion page |

## Card Rating Screens

The overlay shows scores on:

- **Post-combat card rewards** — The standard 3-card pick after a fight
- **Event card choices** — "Choose N cards" events in '?' rooms
- **Shop cards** — Character and colorless cards at the merchant

Scores are color-coded: green (strong pick), yellow (average), red (weak). The best card gets a gold border and star.

## Important

If you're using [rusty-sts](https://github.com/JiriPlasek/rusty_sts) for auto-sync, disable it when using this mod to avoid duplicate uploads. Use one or the other, not both.
