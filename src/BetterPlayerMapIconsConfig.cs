namespace BetterPlayerMapIcons
{
    /// <summary>
    /// Serialized to ModConfig/betterplayermapicons.json in the game's data folder.
    /// The user edits this file (or uses the /playericon command) to pick which icon
    /// is drawn for players on the world map.
    /// </summary>
    public class BetterPlayerMapIconsConfig
    {
        /// <summary>
        /// Name of the SVG file (without extension) inside
        /// assets/betterplayermapicons/textures/playericons/ to use for the player marker.
        /// Set to null or empty to keep the vanilla drawn marker.
        /// </summary>
        public string IconName = "arrow";

        /// <summary>
        /// Rendered size of the icon in (unscaled) pixels. The game multiplies this by
        /// the GUI scale. Vanilla uses 32. Larger = easier to see.
        /// </summary>
        public int IconSize = 48;

        /// <summary>
        /// Hex color (e.g. "#FFFFFF") used to tint your own player marker.
        /// </summary>
        public string OwnPlayerColor = "#FF3030";

        /// <summary>
        /// Hex color used to tint the markers of other players.
        /// </summary>
        public string OtherPlayerColor = "#30A0FF";

        /// <summary>
        /// When true, the chosen icon also replaces the marker used for other players.
        /// When false, only your own marker changes and other players keep the vanilla look.
        /// </summary>
        public bool ApplyToOtherPlayers = true;
    }
}
