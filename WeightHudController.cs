using BepInEx.Logging;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using System;
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
        private bool _forceRefresh = true;
        private string _lastVerificationSignature;

        private Inventory _subscribedInventory;
        private SkillManager _subscribedSkills;
        private IHealthController _subscribedHealthController;
        private Action _unsubscribeInventoryWeightUpdated;
        private Action _unsubscribeStrengthLevelUp;

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
                UnbindContextEvents();
                CurrentSnapshot = WeightSnapshot.Empty;
                return;
            }

            if (!_forceRefresh && Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            RefreshSnapshot();
        }

        private void RefreshSnapshot()
        {
            _nextRefreshTime = Time.unscaledTime + (_config.RefreshIntervalMs.Value / 1000f);

            try
            {
                var context = _contextResolver.Resolve();
                LogContextVerification(context);
                RebindContextEvents(context);
                CurrentSnapshot = _snapshotBuilder.Build(context);
                _forceRefresh = false;
                _loggedFailure = false;
            }
            catch (Exception ex)
            {
                CurrentSnapshot = WeightSnapshot.Empty;

                if (!_loggedFailure)
                {
                    _loggedFailure = true;
                    _logger?.LogError($"Failed to update weight snapshot: {ex}");
                }
            }
        }

        private void LogContextVerification(WeightRuntimeContext context)
        {
            if (!_config.ScavVerificationLogging.Value || context == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(context.RaidProfileId) && context.ContextType != HudContextType.Raid)
            {
                return;
            }

            var signature = context.DiagnosticSignature;
            if (string.Equals(_lastVerificationSignature, signature, StringComparison.Ordinal))
            {
                return;
            }

            _lastVerificationSignature = signature;
            _logger?.LogInfo($"WeightHUD context verification: {context.DiagnosticSummary}");
        }

        private void RebindContextEvents(WeightRuntimeContext context)
        {
            var inventory = context?.Inventory;
            var skills = context?.Skills;
            var healthController = context?.HealthController;

            if (!ReferenceEquals(_subscribedInventory, inventory))
            {
                _unsubscribeInventoryWeightUpdated?.Invoke();
                _unsubscribeInventoryWeightUpdated = null;
                _subscribedInventory = inventory;
                if (_subscribedInventory?.OnWeightUpdated != null)
                {
                    _unsubscribeInventoryWeightUpdated = _subscribedInventory.OnWeightUpdated.Subscribe(OnWeightUpdated);
                }
            }

            if (!ReferenceEquals(_subscribedSkills, skills))
            {
                _unsubscribeStrengthLevelUp?.Invoke();
                _unsubscribeStrengthLevelUp = null;
                _subscribedSkills = skills;
                if (_subscribedSkills?.Strength?.OnLevelUp != null)
                {
                    _unsubscribeStrengthLevelUp = _subscribedSkills.Strength.OnLevelUp.Subscribe(OnStrengthLevelUp);
                }
            }

            if (!ReferenceEquals(_subscribedHealthController, healthController))
            {
                if (_subscribedHealthController != null)
                {
                    _subscribedHealthController.EffectStartedEvent -= OnHealthEffect;
                    _subscribedHealthController.EffectResidualEvent -= OnHealthEffect;
                    _subscribedHealthController.EffectRemovedEvent -= OnHealthEffect;
                }

                _subscribedHealthController = healthController;
                if (_subscribedHealthController != null)
                {
                    _subscribedHealthController.EffectStartedEvent += OnHealthEffect;
                    _subscribedHealthController.EffectResidualEvent += OnHealthEffect;
                    _subscribedHealthController.EffectRemovedEvent += OnHealthEffect;
                }
            }
        }

        private void UnbindContextEvents()
        {
            _unsubscribeInventoryWeightUpdated?.Invoke();
            _unsubscribeInventoryWeightUpdated = null;
            _subscribedInventory = null;

            _unsubscribeStrengthLevelUp?.Invoke();
            _unsubscribeStrengthLevelUp = null;
            _subscribedSkills = null;

            if (_subscribedHealthController != null)
            {
                _subscribedHealthController.EffectStartedEvent -= OnHealthEffect;
                _subscribedHealthController.EffectResidualEvent -= OnHealthEffect;
                _subscribedHealthController.EffectRemovedEvent -= OnHealthEffect;
                _subscribedHealthController = null;
            }
        }

        private void OnWeightUpdated()
        {
            RequestRefresh();
        }

        private void OnStrengthLevelUp()
        {
            RequestRefresh();
        }

        private void OnHealthEffect(IEffect _)
        {
            RequestRefresh();
        }

        private void RequestRefresh()
        {
            _forceRefresh = true;
            _nextRefreshTime = 0f;
        }
    }
}
