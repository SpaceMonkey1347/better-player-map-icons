# Better Player Map Icons

A client-side Vintage Story mod that replaces the world map player marker with a larger, brighter, **user-selectable** icon so players are easier to spot.

## How it works

Vanilla draws the player marker programmatically (`PlayerMapLayer.OnMapOpenedClient` calls `capi.Gui.Icons.DrawMapPlayer(...)`), so there is no asset to simply override. This mod uses Harmony to postfix that method and swap the marker textures with ones rasterized from an SVG you choose.

## Adding your own icons

1. Drop SVG files into:
   ```
   assets/betterplayermapicons/textures/playericons/
   ```
2. Each file's name (without `.svg`) is its icon name. Two samples are included: `arrow` and `dot`.
3. Design tips:
   - Use a `viewBox` (e.g. `0 0 64 64`); the game rasterizes to a square.
   - The icon is tinted by the configured color, so a white fill with a black outline reads well.
   - For directional markers, point the icon "up" (north). The map rotates it to the player's facing.

## Selecting an icon

Edit `ModConfig/betterplayermapicons.json` in your Vintage Story data folder, or use the in-game client command:

- `.playericon list` — list available icons and show the current one
- `.playericon set <name>` — choose an icon (e.g. `.playericon set arrow`)
- `.playericon size <8-256>` — set the rendered size (vanilla is ~32)
- `.playericon vanilla` — restore the default marker

Reopen the world map after changing the icon to see the update.

## Config options (`betterplayermapicons.json`)

| Field | Meaning |
| --- | --- |
| `IconName` | SVG file name (no extension). Empty = vanilla marker. |
| `IconSize` | Rendered size in pixels (before GUI scaling). |
| `OwnPlayerColor` | Hex tint for your own marker. |
| `OtherPlayerColor` | Hex tint for other players' markers. |
| `ApplyToOtherPlayers` | If false, only your own marker changes. |

## Building and packaging

Set the `VINTAGE_STORY` environment variable to your game install folder once:

```
export VINTAGE_STORY=/home/domo/.local/share/vintagestory
```

Then build. This compiles the DLL **and** produces a ready-to-share zip in `dist/`:

```
dotnet build -c Release
```

Output: `dist/betterplayermapicons_1.0.0.zip` containing `modinfo.json`, `BetterPlayerMapIcons.dll`, and `assets/` — nothing else.

To bump the version, edit `<ModVersion>` in `BetterPlayerMapIcons.csproj` (and `version` in `modinfo.json`); the zip name updates automatically.

## Sharing / distributing

The zip in `dist/` is the entire mod. To give it to someone:

1. Send them `dist/betterplayermapicons_<version>.zip`.
2. They drop it into their Vintage Story mods folder (no compiling on their end):
   - Linux: `~/.config/VintagestoryData/Mods/`
   - Windows: `%appdata%/VintagestoryData/Mods/`
   - macOS: `~/Library/Application Support/VintagestoryData/Mods/`
3. They restart the game. Done.

To publish publicly, upload the same zip to the [Vintage Story Mod DB](https://mods.vintagestory.at/).

## Installing locally (for your own testing)

Copy the zip into your own Mods folder and restart the game:

```
cp dist/betterplayermapicons_1.0.0.zip ~/.config/VintagestoryData/Mods/
```
