using BepInEx.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace JordiXIII.WeightHUD
{
    internal sealed class WeightThresholdGlobals
    {
        public static readonly WeightThresholdGlobals Defaults = new WeightThresholdGlobals(26f, 45f, 86f, false);

        public WeightThresholdGlobals(float baseOverweightThreshold, float slowWalkThreshold, float maxCarryThreshold, bool loadedFromFile)
        {
            BaseOverweightThreshold = baseOverweightThreshold;
            SlowWalkThreshold = slowWalkThreshold;
            MaxCarryThreshold = maxCarryThreshold;
            LoadedFromFile = loadedFromFile;
        }

        public float BaseOverweightThreshold { get; }
        public float SlowWalkThreshold { get; }
        public float MaxCarryThreshold { get; }
        public bool LoadedFromFile { get; }

        public static WeightThresholdGlobals Load(ManualLogSource logger)
        {
            try
            {
                var gameRoot = AppDomain.CurrentDomain.BaseDirectory;
                var globalsPath = Path.Combine(gameRoot, "SPT", "SPT_Data", "database", "globals.json");
                if (!File.Exists(globalsPath))
                {
                    logger?.LogWarning($"Weight thresholds file not found at '{globalsPath}'. Using defaults.");
                    return Defaults;
                }

                var root = JToken.Parse(File.ReadAllText(globalsPath));
                var baseOverweight = root.SelectToken("$..BaseOverweightLimits") as JObject;
                var walkOverweight = root.SelectToken("$..WalkOverweightLimits") as JObject;

                if (baseOverweight == null || walkOverweight == null)
                {
                    logger?.LogWarning("Weight thresholds were not found in globals.json. Using defaults.");
                    return Defaults;
                }

                return new WeightThresholdGlobals(
                    baseOverweight.Value<float?>("x") ?? Defaults.BaseOverweightThreshold,
                    walkOverweight.Value<float?>("x") ?? Defaults.SlowWalkThreshold,
                    walkOverweight.Value<float?>("y") ?? Defaults.MaxCarryThreshold,
                    true
                );
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Failed to load weight thresholds from globals.json: {ex.Message}");
                return Defaults;
            }
        }
    }
}
