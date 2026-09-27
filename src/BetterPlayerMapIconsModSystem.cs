using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace BetterPlayerMapIcons
{
    /// <summary>
    /// Client-side mod that replaces the programmatically drawn player marker on the world map
    /// with a user-selectable SVG icon.
    ///
    /// The vanilla marker is generated in Vintagestory.GameContent.PlayerMapLayer.OnMapOpenedClient
    /// via capi.Gui.Icons.DrawMapPlayer(...). We Harmony-postfix that method and swap the two
    /// private LoadedTexture fields (ownPlayerTexture / otherPlayerTexture) with textures we
    /// rasterize from the chosen SVG asset.
    /// </summary>
    public class BetterPlayerMapIconsModSystem : ModSystem
    {
        private const string ConfigFileName = "betterplayermapicons.json";
        private const string IconAssetPath = "betterplayermapicons:textures/playericons/";
        private const string HarmonyId = "com.example.betterplayermapicons";

        internal static BetterPlayerMapIconsModSystem? Instance;

        private Harmony? harmony;
        private ICoreClientAPI? capi;

        public BetterPlayerMapIconsConfig Config { get; private set; } = new();

        // Only makes sense on the client. The world map / marker layer is client-only.
        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            Instance = this;
            capi = api;

            LoadConfig(api);

            harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(BetterPlayerMapIconsModSystem).Assembly);

            RegisterCommands(api);
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll(HarmonyId);
            harmony = null;
            if (Instance == this) Instance = null;
            base.Dispose();
        }

        #region Config

        private void LoadConfig(ICoreClientAPI api)
        {
            try
            {
                var loaded = api.LoadModConfig<BetterPlayerMapIconsConfig>(ConfigFileName);
                if (loaded == null)
                {
                    Config = new BetterPlayerMapIconsConfig();
                    api.StoreModConfig(Config, ConfigFileName);
                }
                else
                {
                    Config = loaded;
                }
            }
            catch (Exception e)
            {
                api.Logger.Warning("[BetterPlayerMapIcons] Failed to load config, using defaults: {0}", e);
                Config = new BetterPlayerMapIconsConfig();
            }
        }

        internal void SaveConfig()
        {
            capi?.StoreModConfig(Config, ConfigFileName);
        }

        #endregion

        #region Icon loading

        /// <summary>
        /// Lists the icon names (SVG file names without extension) available in the icon folder.
        /// </summary>
        internal List<string> ListAvailableIcons()
        {
            if (capi == null) return new List<string>();

            var assets = capi.Assets.GetMany(AssetCategory.textures.Code + "/playericons/", "betterplayermapicons", false);
            var names = new List<string>();
            foreach (var asset in assets)
            {
                if (!asset.Name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) continue;
                names.Add(asset.Name.Substring(0, asset.Name.LastIndexOf('.')));
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        /// <summary>
        /// Rasterizes the currently configured SVG icon into a LoadedTexture, tinted with the
        /// given hex color. Returns null if no icon is configured or the asset is missing, in
        /// which case the caller should keep the vanilla texture.
        /// </summary>
        internal LoadedTexture? TryLoadIconTexture(string hexColor)
        {
            if (capi == null) return null;
            if (string.IsNullOrWhiteSpace(Config.IconName)) return null;

            var loc = new AssetLocation(IconAssetPath + Config.IconName + ".svg");
            if (capi.Assets.TryGet(loc) == null)
            {
                capi.Logger.Warning("[BetterPlayerMapIcons] Icon '{0}' not found at {1}, keeping vanilla marker.", Config.IconName, loc);
                return null;
            }

            int res = (int)Math.Ceiling(Math.Max(8, Config.IconSize) * RuntimeEnv.GUIScale);
            int color = ParseColor(hexColor);

            try
            {
                LoadedTexture tex = capi.Gui.LoadSvg(loc, res, res, res, res, color);
                // Width/Height control the on-screen render size in PlayerMapLayer.Render
                // (it scales the quad by tex.Width/Height then by 0.5). Vanilla uses size/2,
                // so we mirror that relationship to keep the marker proportional.
                tex.Width = res / 2;
                tex.Height = res / 2;
                return tex;
            }
            catch (Exception e)
            {
                capi.Logger.Warning("[BetterPlayerMapIcons] Failed to rasterize icon '{0}': {1}", Config.IconName, e);
                return null;
            }
        }

        private static int ParseColor(string hex)
        {
            try
            {
                return ColorUtil.Hex2Int(hex) | (255 << 24);
            }
            catch
            {
                return ColorUtil.WhiteArgb;
            }
        }

        #endregion

        #region Commands

        private void RegisterCommands(ICoreClientAPI api)
        {
            var parsers = api.ChatCommands.Parsers;

            api.ChatCommands.Create("playericon")
                .WithDescription("Configure the world map player marker icon")
                .BeginSubCommand("list")
                    .WithDescription("List available icons")
                    .HandleWith(OnCmdList)
                .EndSubCommand()
                .BeginSubCommand("set")
                    .WithDescription("Set the icon by name")
                    .WithArgs(parsers.Word("iconName"))
                    .HandleWith(OnCmdSet)
                .EndSubCommand()
                .BeginSubCommand("size")
                    .WithDescription("Set the icon size in pixels")
                    .WithArgs(parsers.Int("size"))
                    .HandleWith(OnCmdSize)
                .EndSubCommand()
                .BeginSubCommand("vanilla")
                    .WithDescription("Restore the vanilla player marker")
                    .HandleWith(OnCmdVanilla)
                .EndSubCommand();
        }

        private TextCommandResult OnCmdList(TextCommandCallingArgs args)
        {
            var icons = ListAvailableIcons();
            if (icons.Count == 0)
            {
                return TextCommandResult.Success("No icons found. Add SVG files to assets/betterplayermapicons/textures/playericons/");
            }
            return TextCommandResult.Success("Available icons: " + string.Join(", ", icons) +
                "\nCurrent: " + (string.IsNullOrEmpty(Config.IconName) ? "(vanilla)" : Config.IconName));
        }

        private TextCommandResult OnCmdSet(TextCommandCallingArgs args)
        {
            string name = (string)args[0];
            var icons = ListAvailableIcons();
            if (!icons.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                return TextCommandResult.Error($"Icon '{name}' not found. Use '.playericon list' to see options.");
            }

            Config.IconName = name;
            SaveConfig();
            return TextCommandResult.Success($"Player map icon set to '{name}'. Reopen the map to see the change.");
        }

        private TextCommandResult OnCmdSize(TextCommandCallingArgs args)
        {
            int size = (int)args[0];
            if (size < 8 || size > 256)
            {
                return TextCommandResult.Error("Size must be between 8 and 256.");
            }
            Config.IconSize = size;
            SaveConfig();
            return TextCommandResult.Success($"Player map icon size set to {size}. Reopen the map to see the change.");
        }

        private TextCommandResult OnCmdVanilla(TextCommandCallingArgs args)
        {
            Config.IconName = "";
            SaveConfig();
            return TextCommandResult.Success("Restored vanilla player marker. Reopen the map to see the change.");
        }

        #endregion
    }
}
