using HarmonyLib;
using Vintagestory.API.Client;

namespace BetterPlayerMapIcons
{
    /// <summary>
    /// Postfix on PlayerMapLayer.OnMapOpenedClient. That method lazily builds the two player
    /// marker textures (ownPlayerTexture / otherPlayerTexture) by drawing them with Cairo.
    /// After it runs, we overwrite those private fields with textures rasterized from the
    /// user's chosen SVG. If no icon is configured or loading fails, we leave the vanilla
    /// textures untouched.
    ///
    /// The class lives in the game's VSEssentialsMod assembly under the internal namespace
    /// Vintagestory.GameContent, so we resolve it by name rather than a compile-time reference.
    /// </summary>
    [HarmonyPatch]
    public static class PlayerMapLayerPatch
    {
        public static bool Prepare()
        {
            // Only patch if the target type actually exists in this game version.
            return AccessTools.TypeByName("Vintagestory.GameContent.PlayerMapLayer") != null;
        }

        public static System.Reflection.MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("Vintagestory.GameContent.PlayerMapLayer");
            return AccessTools.Method(type, "OnMapOpenedClient");
        }

        public static void Postfix(object __instance)
        {
            var mod = BetterPlayerMapIconsModSystem.Instance;
            if (mod == null) return;

            var config = mod.Config;

            // Replace own-player texture.
            var ownTex = mod.TryLoadIconTexture(config.OwnPlayerColor);
            if (ownTex != null)
            {
                ReplaceTexture(__instance, "ownPlayerTexture", ownTex);
            }

            // Replace other-player texture only if enabled.
            if (config.ApplyToOtherPlayers)
            {
                var otherTex = mod.TryLoadIconTexture(config.OtherPlayerColor);
                if (otherTex != null)
                {
                    ReplaceTexture(__instance, "otherPlayerTexture", otherTex);
                }
            }
        }

        private static void ReplaceTexture(object instance, string fieldName, LoadedTexture newTex)
        {
            var field = AccessTools.Field(instance.GetType(), fieldName);
            if (field == null) return;

            // Dispose the vanilla texture we're replacing to avoid leaking the GL texture.
            if (field.GetValue(instance) is LoadedTexture old && old != newTex)
            {
                old.Dispose();
            }

            field.SetValue(instance, newTex);
        }
    }
}
