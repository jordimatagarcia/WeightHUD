using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace JordiXIII.WeightHUD
{
    [BepInPlugin("JordiXIII.WeightHUD", "WeightHUD", "0.1.1")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource LogSource;

        private WeightHudConfig _config;
        private WeightHudController _controller;
        private WeightHudRenderer _renderer;

        private void Awake()
        {
            LogSource = Logger;

            _config = WeightHudConfig.Bind(Config);
            _controller = new WeightHudController(Logger, _config);
            _renderer = new WeightHudRenderer(_config);

            Logger.LogInfo("WeightHUD loaded.");
        }

        private void Update()
        {
            _controller.Update();
        }

        private void OnGUI()
        {
            if (_controller.ShouldDraw)
            {
                _renderer.Draw(_controller.CurrentSnapshot);
            }
        }
    }
}
