using BepInEx.Configuration;
using UnityEngine;

namespace JordiXIII.WeightHUD
{
    internal enum BreakdownPlacement
    {
        Right,
        Bottom,
        Left,
        Top
    }

    internal sealed class WeightHudConfig
    {
        public ConfigEntry<bool> EnableHud { get; private set; }
        public ConfigEntry<bool> ShowInMainMenu { get; private set; }
        public ConfigEntry<KeyboardShortcut> ToggleHudShortcut { get; private set; }
        public ConfigEntry<int> RefreshIntervalMs { get; private set; }

        public ConfigEntry<float> AnchorX { get; private set; }
        public ConfigEntry<float> AnchorY { get; private set; }
        public ConfigEntry<float> Scale { get; private set; }
        public ConfigEntry<BreakdownPlacement> BreakdownAnchor { get; private set; }
        public ConfigEntry<bool> ShowGaugeThresholdLabels { get; private set; }

        public ConfigEntry<string> PreferredFontName { get; private set; }
        public ConfigEntry<Color> PanelBackgroundColor { get; private set; }
        public ConfigEntry<Color> PanelAccentColor { get; private set; }
        public ConfigEntry<Color> PrimaryTextColor { get; private set; }
        public ConfigEntry<Color> SecondaryTextColor { get; private set; }
        public ConfigEntry<Color> TrackColor { get; private set; }
        public ConfigEntry<Color> SafeWeightColor { get; private set; }
        public ConfigEntry<Color> NormalWeightColor { get; private set; }
        public ConfigEntry<Color> OverweightColor { get; private set; }
        public ConfigEntry<Color> SlowWalkColor { get; private set; }
        public ConfigEntry<Color> MaxWeightColor { get; private set; }
        public ConfigEntry<Color> PmcBadgeColor { get; private set; }
        public ConfigEntry<Color> ScavBadgeColor { get; private set; }

        public static WeightHudConfig Bind(ConfigFile config)
        {
            return new WeightHudConfig
            {
                EnableHud = config.Bind(
                    "General",
                    "Enable HUD",
                    true,
                    "Shows the weight HUD overlay."
                ),
                ShowInMainMenu = config.Bind(
                    "General",
                    "Show In Main Menu",
                    true,
                    "Draws the HUD for the PMC profile while in the main menu."
                ),
                ToggleHudShortcut = config.Bind(
                    "General",
                    "Toggle HUD Shortcut",
                    new KeyboardShortcut(KeyCode.F10),
                    "Toggles the HUD without disabling the plugin."
                ),
                RefreshIntervalMs = config.Bind(
                    "General",
                    "Refresh Interval (ms)",
                    150,
                    new ConfigDescription(
                        "How often the HUD data is refreshed.",
                        new AcceptableValueRange<int>(50, 1000)
                    )
                ),
                AnchorX = config.Bind(
                    "Layout",
                    "Anchor X",
                    24f,
                    new ConfigDescription(
                        "Horizontal HUD offset in pixels.",
                        new AcceptableValueRange<float>(0f, 3840f)
                    )
                ),
                AnchorY = config.Bind(
                    "Layout",
                    "Anchor Y",
                    24f,
                    new ConfigDescription(
                        "Vertical HUD offset in pixels.",
                        new AcceptableValueRange<float>(0f, 2160f)
                    )
                ),
                Scale = config.Bind(
                    "Layout",
                    "Scale",
                    1f,
                    new ConfigDescription(
                        "Overall HUD scale.",
                        new AcceptableValueRange<float>(0.7f, 1.8f)
                    )
                ),
                BreakdownAnchor = config.Bind(
                    "Layout",
                    "Breakdown Placement",
                    BreakdownPlacement.Right,
                    "Shows the equipment, weapons and backpack rows to the Right, Bottom, Left or Top of the gauge."
                ),
                ShowGaugeThresholdLabels = config.Bind(
                    "Layout",
                    "Show Gauge Threshold Labels",
                    true,
                    "Shows the numeric threshold values next to the circular gauge tick marks."
                ),
                PreferredFontName = config.Bind(
                    "Visuals",
                    "Preferred Font Name",
                    "Bender",
                    "Preferred in-game font to use when it is loaded."
                ),
                PanelBackgroundColor = config.Bind(
                    "Visuals",
                    "Panel Background",
                    new Color(0.05f, 0.07f, 0.09f, 0.82f),
                    "Background color of the HUD panel."
                ),
                PanelAccentColor = config.Bind(
                    "Visuals",
                    "Panel Accent",
                    new Color(0.90f, 0.78f, 0.41f, 1f),
                    "Accent line and separators."
                ),
                PrimaryTextColor = config.Bind(
                    "Visuals",
                    "Primary Text",
                    new Color(0.97f, 0.97f, 0.95f, 1f),
                    "Primary text color."
                ),
                SecondaryTextColor = config.Bind(
                    "Visuals",
                    "Secondary Text",
                    new Color(0.71f, 0.74f, 0.78f, 1f),
                    "Secondary text color."
                ),
                TrackColor = config.Bind(
                    "Visuals",
                    "Gauge Track",
                    new Color(0.21f, 0.25f, 0.29f, 0.85f),
                    "Background ring color."
                ),
                SafeWeightColor = config.Bind(
                    "Visuals",
                    "Safe Weight Color",
                    new Color(0.62f, 0.65f, 0.69f, 1f),
                    "Color used while below the first threshold."
                ),
                NormalWeightColor = config.Bind(
                    "Visuals",
                    "First Threshold Color",
                    new Color(0.95f, 0.88f, 0.58f, 1f),
                    "Color used for the first threshold marker."
                ),
                OverweightColor = config.Bind(
                    "Visuals",
                    "Overweight Color",
                    new Color(0.92f, 0.58f, 0.24f, 1f),
                    "Color used between overweight and slow walk."
                ),
                SlowWalkColor = config.Bind(
                    "Visuals",
                    "Slow Walk Color",
                    new Color(0.87f, 0.33f, 0.16f, 1f),
                    "Color used between slow walk and max carry."
                ),
                MaxWeightColor = config.Bind(
                    "Visuals",
                    "Maximum Weight Color",
                    new Color(0.66f, 0.09f, 0.12f, 1f),
                    "Color used when at or above the maximum carry threshold."
                ),
                PmcBadgeColor = config.Bind(
                    "Visuals",
                    "PMC Badge Color",
                    new Color(0.28f, 0.53f, 0.79f, 1f),
                    "Badge color used for PMC."
                ),
                ScavBadgeColor = config.Bind(
                    "Visuals",
                    "SCAV Badge Color",
                    new Color(0.53f, 0.72f, 0.30f, 1f),
                    "Badge color used for SCAV."
                )
            };
        }
    }
}
