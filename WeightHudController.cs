using BepInEx.Logging;
using UnityEngine;

namespace JordiXIII.WeightHUD
{
    internal sealed class WeightHudController
    {
        private readonly ManualLogSource _logger;
        private readonly WeightHudConfig _config;
        private readonly WeightContextResolver _contextResolver;
        private readonly WeightSnapshotBuilder _snapshotBuilder;

        private bool _isVisible = true;
        private float _nextRefreshTime;
        private bool _loggedFailure;

        public WeightHudController(ManualLogSource logger, WeightHudConfig config)
        {
            _logger = logger;
            _config = config;
            _contextResolver = new WeightContextResolver();
            _snapshotBuilder = new WeightSnapshotBuilder(WeightThresholdGlobals.Load(logger));
            CurrentSnapshot = WeightSnapshot.Empty;
        }

        public WeightSnapshot CurrentSnapshot { get; private set; }

        public bool ShouldDraw =>
            _config.EnableHud.Value &&
            _isVisible &&
            CurrentSnapshot != null &&
            CurrentSnapshot.IsValid &&
            (CurrentSnapshot.ContextType != HudContextType.MainMenu || _config.ShowInMainMenu.Value);

        public void Update()
        {
            if (_config.ToggleHudShortcut.Value.MainKey != KeyCode.None && _config.ToggleHudShortcut.Value.IsDown())
            {
                _isVisible = !_isVisible;
            }

            if (!_config.EnableHud.Value)
            {
                CurrentSnapshot = WeightSnapshot.Empty;
                return;
            }

            if (Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + (_config.RefreshIntervalMs.Value / 1000f);

            try
            {
                var context = _contextResolver.Resolve();
                CurrentSnapshot = _snapshotBuilder.Build(context);
                _loggedFailure = false;
            }
            catch (System.Exception ex)
            {
                CurrentSnapshot = WeightSnapshot.Empty;

                if (!_loggedFailure)
                {
                    _loggedFailure = true;
                    _logger?.LogError($"Failed to update weight snapshot: {ex}");
                }
            }
        }
    }
}
