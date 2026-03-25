using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace JordiXIII.WeightHUD
{
    [BepInPlugin("JordiXIII.WeightHUD", "WeightHUD", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        // Configuration entries — editable in-game via F12 or in the config file.
        public static ConfigEntry<bool> EnableMod;
        public static ConfigEntry<int> ExampleInt;
        public static ConfigEntry<float> ExampleFloat;
        public static ConfigEntry<string> ExampleString;
        public static ConfigEntry<ExampleEnum> ExampleEnumSetting;
        public static ConfigEntry<KeyboardShortcut> ExampleKeybind;
        public static ConfigEntry<Color> ExampleColor;

        public enum ExampleEnum
        {
            OptionA,
            OptionB,
            OptionC
        }

        private void Awake()
        {
            LogSource = Logger;

            InitConfiguration();

            LogSource.LogInfo($"WeightHUD loaded!");
        }

        private void InitConfiguration()
        {
            // Boolean setting
            EnableMod = Config.Bind(
                "General",
                "Enable Mod",
                true,
                "Enables or disables the mod entirely."
            );

            // Integer setting with range
            ExampleInt = Config.Bind(
                "General",
                "Example Integer",
                5,
                new ConfigDescription(
                    "An integer setting with a slider in F12 menu.",
                    new AcceptableValueRange<int>(0, 100)
                )
            );

            // Float setting with range
            ExampleFloat = Config.Bind(
                "General",
                "Example Float",
                1.0f,
                new ConfigDescription(
                    "A float setting with a slider.",
                    new AcceptableValueRange<float>(0f, 10f)
                )
            );

            // String setting
            ExampleString = Config.Bind(
                "General",
                "Example String",
                "Hello SPT",
                "A simple text box setting."
            );

            // Enum setting (shows as a dropdown/picker in F12)
            ExampleEnumSetting = Config.Bind(
                "Advanced",
                "Enum Selection",
                ExampleEnum.OptionA,
                "A dropdown selection from an enum."
            );

            // Keyboard shortcut (can be remapped in F12)
            ExampleKeybind = Config.Bind(
                "Keybinds",
                "Action Key",
                new KeyboardShortcut(KeyCode.F10),
                "A keybind that can be used by other systems."
            );

            // Color setting (shows as a color picker in F12)
            ExampleColor = Config.Bind(
                "Visuals",
                "Overlay Color",
                Color.red,
                "A color setting for UI or visuals."
            );
        }
    }
}
