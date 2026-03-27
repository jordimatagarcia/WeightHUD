using System;
using UnityEngine;

namespace JordiXIII.WeightHUD
{
    internal sealed class WeightHudRenderer
    {
        private const float GaugeStartAngle = -225f;
        private const float GaugeSweepAngle = 270f;

        private readonly WeightHudConfig _config;
        private readonly GameFontResolver _fontResolver;

        private Texture2D _pixel;
        private Material _vectorMaterial;
        private Font _cachedFont;
        private float _cachedScale = -1f;
        private GUIStyle _titleStyle;
        private GUIStyle _badgeStyle;
        private GUIStyle _centerValueStyle;
        private GUIStyle _centerUnitStyle;
        private GUIStyle _breakdownLabelStyle;
        private GUIStyle _breakdownValueStyle;
        private GUIStyle _thresholdLabelStyle;

        public WeightHudRenderer(WeightHudConfig config)
        {
            _config = config;
            _fontResolver = new GameFontResolver();
        }

        public void Draw(WeightSnapshot snapshot)
        {
            if (snapshot == null || !snapshot.IsValid || Event.current.type != EventType.Repaint)
            {
                return;
            }

            EnsureResources();

            var scale = _config.Scale.Value;
            var layout = _config.MinimalHud.Value ? BuildMinimalLayout(scale) : BuildFullLayout(scale);
            layout.PanelRect.x = Mathf.Clamp(_config.AnchorX.Value, 0f, Mathf.Max(0f, Screen.width - layout.PanelRect.width - 8f));
            layout.PanelRect.y = Mathf.Clamp(_config.AnchorY.Value, 0f, Mathf.Max(0f, Screen.height - layout.PanelRect.height - 8f));
            ApplyAnchoring(ref layout);

            if (!_config.MinimalHud.Value)
            {
                DrawPanel(layout.PanelRect, layout, scale);
                DrawHeader(layout.HeaderRect, layout.BadgeRect, snapshot, scale);
                DrawBreakdownRows(layout.BreakdownRect, snapshot, scale);
            }

            DrawGauge(layout.GaugeRect, snapshot, scale, _config.MinimalHud.Value);
        }

        private Layout BuildMinimalLayout(float scale)
        {
            var padding = 8f * scale;
            var gaugeSize = 142f * scale;
            var panelRect = new Rect(0f, 0f, gaugeSize + (padding * 2f), gaugeSize + (padding * 2f));

            return new Layout
            {
                Placement = BreakdownPlacement.Right,
                PanelRect = panelRect,
                GaugeRect = new Rect(padding, padding, gaugeSize, gaugeSize)
            };
        }

        private Layout BuildFullLayout(float scale)
        {
            var padding = 12f * scale;
            var sectionGap = 10f * scale;
            var headerHeight = 22f * scale;
            var headerGap = 6f * scale;
            var gaugeSize = 132f * scale;
            var breakdownWidth = 134f * scale;
            var breakdownHeight = 62f * scale;
            var badgeWidth = 58f * scale;

            var placement = _config.BreakdownAnchor.Value;
            var panelSize = Vector2.zero;
            var gaugeRect = new Rect();
            var breakdownRect = new Rect();

            switch (placement)
            {
                case BreakdownPlacement.Left:
                case BreakdownPlacement.Right:
                    panelSize = new Vector2((padding * 2f) + gaugeSize + sectionGap + breakdownWidth, (padding * 2f) + headerHeight + headerGap + Mathf.Max(gaugeSize, breakdownHeight));
                    break;
                default:
                    panelSize = new Vector2((padding * 2f) + Mathf.Max(gaugeSize, breakdownWidth), (padding * 2f) + headerHeight + headerGap + gaugeSize + sectionGap + breakdownHeight);
                    break;
            }

            var panelRect = new Rect(0f, 0f, panelSize.x, panelSize.y);
            var contentTop = padding + headerHeight + headerGap;

            if (placement == BreakdownPlacement.Right)
            {
                gaugeRect = new Rect(padding, contentTop, gaugeSize, gaugeSize);
                breakdownRect = new Rect(gaugeRect.xMax + sectionGap, contentTop + ((gaugeSize - breakdownHeight) * 0.5f), breakdownWidth, breakdownHeight);
            }
            else if (placement == BreakdownPlacement.Left)
            {
                breakdownRect = new Rect(padding, contentTop + ((gaugeSize - breakdownHeight) * 0.5f), breakdownWidth, breakdownHeight);
                gaugeRect = new Rect(breakdownRect.xMax + sectionGap, contentTop, gaugeSize, gaugeSize);
            }
            else if (placement == BreakdownPlacement.Top)
            {
                breakdownRect = new Rect(padding, contentTop, panelRect.width - (padding * 2f), breakdownHeight);
                gaugeRect = new Rect(padding + ((breakdownRect.width - gaugeSize) * 0.5f), breakdownRect.yMax + sectionGap, gaugeSize, gaugeSize);
            }
            else
            {
                gaugeRect = new Rect(padding + ((panelRect.width - (padding * 2f) - gaugeSize) * 0.5f), contentTop, gaugeSize, gaugeSize);
                breakdownRect = new Rect(padding, gaugeRect.yMax + sectionGap, panelRect.width - (padding * 2f), breakdownHeight);
            }

            return new Layout
            {
                Placement = placement,
                PanelRect = panelRect,
                HeaderRect = new Rect(padding, padding - (1f * scale), panelRect.width - (padding * 2f) - badgeWidth - (6f * scale), headerHeight),
                BadgeRect = new Rect(panelRect.width - padding - badgeWidth, padding - (1f * scale), badgeWidth, headerHeight),
                GaugeRect = gaugeRect,
                BreakdownRect = breakdownRect
            };
        }

        private void ApplyAnchoring(ref Layout layout)
        {
            var panelOrigin = layout.PanelRect.position;
            layout.HeaderRect.position += panelOrigin;
            layout.BadgeRect.position += panelOrigin;
            layout.GaugeRect.position += panelOrigin;
            layout.BreakdownRect.position += panelOrigin;
        }

        private void EnsureResources()
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _pixel.SetPixel(0, 0, Color.white);
                _pixel.Apply();
            }

            if (_vectorMaterial == null)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                _vectorMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                _vectorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _vectorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _vectorMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                _vectorMaterial.SetInt("_ZWrite", 0);
            }

            var font = _fontResolver.Resolve(_config.PreferredFontName.Value);
            if (_cachedFont != font || !Mathf.Approximately(_cachedScale, _config.Scale.Value))
            {
                _cachedFont = font;
                _cachedScale = _config.Scale.Value;
                RebuildStyles(font, _cachedScale);
            }
        }

        private void RebuildStyles(Font font, float scale)
        {
            _titleStyle = BuildStyle(font, Mathf.RoundToInt(14f * scale), TextAnchor.MiddleLeft, FontStyle.Bold);
            _badgeStyle = BuildStyle(font, Mathf.RoundToInt(11f * scale), TextAnchor.MiddleCenter, FontStyle.Bold);
            _centerValueStyle = BuildStyle(font, Mathf.RoundToInt(25f * scale), TextAnchor.MiddleCenter, FontStyle.Bold);
            _centerUnitStyle = BuildStyle(font, Mathf.RoundToInt(10f * scale), TextAnchor.UpperCenter, FontStyle.Normal);
            _breakdownLabelStyle = BuildStyle(font, Mathf.RoundToInt(11f * scale), TextAnchor.MiddleLeft, FontStyle.Normal);
            _breakdownValueStyle = BuildStyle(font, Mathf.RoundToInt(14f * scale), TextAnchor.MiddleRight, FontStyle.Bold);
            _thresholdLabelStyle = BuildStyle(font, Mathf.RoundToInt(10f * scale), TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        private static GUIStyle BuildStyle(Font font, int fontSize, TextAnchor alignment, FontStyle fontStyle)
        {
            return new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = fontSize,
                alignment = alignment,
                fontStyle = fontStyle,
                clipping = TextClipping.Overflow,
                richText = false
            };
        }

        private void DrawPanel(Rect panelRect, Layout layout, float scale)
        {
            DrawRect(panelRect, _config.PanelBackgroundColor.Value);
            DrawRect(new Rect(panelRect.x, panelRect.y, panelRect.width, 2f * scale), _config.PanelAccentColor.Value);
            DrawRect(new Rect(panelRect.x, panelRect.y + panelRect.height - 1f, panelRect.width, 1f), new Color(1f, 1f, 1f, 0.06f));

            var separatorColor = new Color(1f, 1f, 1f, 0.07f);
            var gap = 6f * scale;
            if (layout.Placement == BreakdownPlacement.Left)
            {
                DrawRect(new Rect(layout.GaugeRect.x - gap, layout.GaugeRect.y + (8f * scale), 1f, layout.GaugeRect.height - (16f * scale)), separatorColor);
            }
            else if (layout.Placement == BreakdownPlacement.Right)
            {
                DrawRect(new Rect(layout.BreakdownRect.x - gap, layout.GaugeRect.y + (8f * scale), 1f, layout.GaugeRect.height - (16f * scale)), separatorColor);
            }
            else if (layout.Placement == BreakdownPlacement.Top)
            {
                DrawRect(new Rect(layout.BreakdownRect.x + (8f * scale), layout.GaugeRect.y - gap, layout.BreakdownRect.width - (16f * scale), 1f), separatorColor);
            }
            else
            {
                DrawRect(new Rect(layout.BreakdownRect.x + (8f * scale), layout.BreakdownRect.y - gap, layout.BreakdownRect.width - (16f * scale), 1f), separatorColor);
            }
        }

        private void DrawHeader(Rect headerRect, Rect badgeRect, WeightSnapshot snapshot, float scale)
        {
            DrawShadowedLabel(headerRect, "WEIGHT", _titleStyle, _config.PrimaryTextColor.Value, scale);

            var badgeColor = snapshot.Role == HudPlayerRole.Scav ? _config.ScavBadgeColor.Value : _config.PmcBadgeColor.Value;
            DrawRect(badgeRect, new Color(badgeColor.r, badgeColor.g, badgeColor.b, 0.18f));
            DrawRect(new Rect(badgeRect.x, badgeRect.yMax - (2f * scale), badgeRect.width, 2f * scale), badgeColor);
            DrawShadowedLabel(badgeRect, snapshot.RoleLabel, _badgeStyle, Color.Lerp(Color.white, badgeColor, 0.2f), scale);
        }

        private void DrawGauge(Rect rect, WeightSnapshot snapshot, float scale, bool minimalHud)
        {
            var center = rect.center;
            var radius = rect.width * 0.34f;
            var thickness = minimalHud ? 12f * scale : 10f * scale;
            var maxWeight = Mathf.Max(1f, snapshot.MaxWeightThreshold);

            if (minimalHud)
            {
                DrawRing(center, radius + (4f * scale), 2f * scale, GaugeStartAngle, GaugeSweepAngle, new Color(0f, 0f, 0f, 0.45f));
                DrawRing(center, radius - thickness - (3f * scale), 2f * scale, GaugeStartAngle, GaugeSweepAngle, new Color(1f, 1f, 1f, 0.15f));
            }

            DrawRing(center, radius, thickness, GaugeStartAngle, GaugeSweepAngle, _config.TrackColor.Value);
            DrawThresholdBand(center, radius, thickness, snapshot.CurrentWeight, maxWeight, 0f, snapshot.OverweightThreshold, _config.SafeWeightColor.Value);
            DrawThresholdBand(center, radius, thickness, snapshot.CurrentWeight, maxWeight, snapshot.OverweightThreshold, snapshot.CriticalOverweightThreshold, _config.OverweightColor.Value);
            DrawThresholdBand(center, radius, thickness, snapshot.CurrentWeight, maxWeight, snapshot.CriticalOverweightThreshold, snapshot.MaxWeightThreshold, _config.CriticalWeightColor.Value);

            if (snapshot.CurrentWeight >= snapshot.MaxWeightThreshold)
            {
                DrawRing(center, radius + (6f * scale), 3f * scale, GaugeStartAngle, GaugeSweepAngle, new Color(_config.MaxWeightColor.Value.r, _config.MaxWeightColor.Value.g, _config.MaxWeightColor.Value.b, 0.40f));
            }

            DrawTick(center, radius, thickness, maxWeight, snapshot.OverweightThreshold, _config.OverweightColor.Value, 11f * scale, 2f * scale);
            DrawTick(center, radius, thickness, maxWeight, snapshot.CriticalOverweightThreshold, _config.CriticalWeightColor.Value, 13f * scale, 2f * scale);
            DrawTick(center, radius, thickness, maxWeight, snapshot.MaxWeightThreshold, _config.MaxWeightColor.Value, 16f * scale, 3f * scale);

            if (!minimalHud && _config.ShowGaugeThresholdLabels.Value)
            {
                DrawThresholdLabel(center, radius, thickness, maxWeight, snapshot.OverweightThreshold, _config.OverweightColor.Value, scale);
                DrawThresholdLabel(center, radius, thickness, maxWeight, snapshot.CriticalOverweightThreshold, _config.CriticalWeightColor.Value, scale);
                DrawThresholdLabel(center, radius, thickness, maxWeight, snapshot.MaxWeightThreshold, _config.MaxWeightColor.Value, scale);
            }

            DrawShadowedLabel(new Rect(rect.x, center.y - (23f * scale), rect.width, 32f * scale), snapshot.CurrentWeight.ToString("0.0"), _centerValueStyle, GetCurrentWeightColor(snapshot.State), scale);
            DrawShadowedLabel(new Rect(rect.x, center.y + (7f * scale), rect.width, 18f * scale), "kg", _centerUnitStyle, _config.SecondaryTextColor.Value, scale);
        }

        private void DrawBreakdownRows(Rect rect, WeightSnapshot snapshot, float scale)
        {
            var rowHeight = rect.height / 3f;
            DrawBreakdownRow(new Rect(rect.x, rect.y, rect.width, rowHeight), "Equipment", snapshot.EquipmentWeight, _config.EquipmentColor.Value, scale);
            DrawBreakdownRow(new Rect(rect.x, rect.y + rowHeight, rect.width, rowHeight), "Weapons", snapshot.WeaponWeight, snapshot.HasEliteStrength ? _config.SecondaryTextColor.Value : _config.WeaponsColor.Value, scale);
            DrawBreakdownRow(new Rect(rect.x, rect.y + (rowHeight * 2f), rect.width, rowHeight), "Backpack", snapshot.BackpackWeight, _config.BackpackColor.Value, scale);
        }

        private void DrawBreakdownRow(Rect rowRect, string label, float value, Color accent, float scale)
        {
            DrawRect(new Rect(rowRect.x, rowRect.y, rowRect.width, 1f), new Color(1f, 1f, 1f, 0.05f));
            DrawRect(new Rect(rowRect.x, rowRect.y + (4f * scale), 2f * scale, rowRect.height - (8f * scale)), accent);
            DrawShadowedLabel(new Rect(rowRect.x + (8f * scale), rowRect.y, rowRect.width * 0.52f, rowRect.height), label, _breakdownLabelStyle, _config.SecondaryTextColor.Value, scale);
            DrawShadowedLabel(new Rect(rowRect.x, rowRect.y, rowRect.width - (4f * scale), rowRect.height), $"{value:0.0} kg", _breakdownValueStyle, accent, scale);
        }

        private void DrawThresholdLabel(Vector2 center, float radius, float thickness, float maxValue, float value, Color color, float scale)
        {
            if (value <= 0f || maxValue <= 0f)
            {
                return;
            }

            var angle = ValueToAngle(value, maxValue);
            var direction = AngleToDirection(angle);
            var offset = radius + thickness + (8f * scale);
            var labelCenter = center + (direction * offset);
            var labelRect = new Rect(labelCenter.x - (18f * scale), labelCenter.y - (8f * scale), 36f * scale, 16f * scale);
            DrawShadowedLabel(labelRect, value.ToString("0"), _thresholdLabelStyle, color, scale);
        }

        private void DrawThresholdBand(Vector2 center, float radius, float thickness, float currentWeight, float maxValue, float startValue, float endValue, Color color)
        {
            var clampedStart = Mathf.Clamp(startValue, 0f, maxValue);
            var clampedEnd = Mathf.Clamp(endValue, 0f, maxValue);
            var current = Mathf.Clamp(currentWeight, 0f, maxValue);
            if (current <= clampedStart)
            {
                return;
            }

            var effectiveEnd = Mathf.Min(clampedEnd, current);
            if (effectiveEnd <= clampedStart)
            {
                return;
            }

            DrawRing(center, radius, thickness, ValueToAngle(clampedStart, maxValue), ValueToAngle(effectiveEnd, maxValue) - ValueToAngle(clampedStart, maxValue), color);
        }

        private void DrawTick(Vector2 center, float radius, float thickness, float maxValue, float value, Color color, float length, float width)
        {
            if (value <= 0f || maxValue <= 0f)
            {
                return;
            }

            var direction = AngleToDirection(ValueToAngle(value, maxValue));
            var tangent = new Vector2(-direction.y, direction.x);
            var start = center + (direction * (radius - (thickness * 0.5f) - (1f * width)));
            var end = center + (direction * (radius + length));
            DrawQuad(start - (tangent * (width * 0.5f)), start + (tangent * (width * 0.5f)), end + (tangent * (width * 0.5f)), end - (tangent * (width * 0.5f)), color);
        }

        private void DrawRing(Vector2 center, float radius, float thickness, float startAngle, float sweepAngle, Color color)
        {
            var absSweep = Mathf.Abs(sweepAngle);
            if (absSweep <= 0.01f || thickness <= 0.01f)
            {
                return;
            }

            BeginTriangles();

            var segments = Mathf.Max(48, Mathf.CeilToInt(absSweep / 2.5f));
            var delta = sweepAngle / segments;
            var innerRadius = Mathf.Max(0f, radius - thickness);
            for (var index = 0; index < segments; index++)
            {
                var angle0 = startAngle + (delta * index);
                var angle1 = angle0 + delta;
                var outer0 = center + (AngleToDirection(angle0) * radius);
                var outer1 = center + (AngleToDirection(angle1) * radius);
                var inner0 = center + (AngleToDirection(angle0) * innerRadius);
                var inner1 = center + (AngleToDirection(angle1) * innerRadius);

                DrawTriangle(outer0, outer1, inner1, color);
                DrawTriangle(outer0, inner1, inner0, color);
            }

            EndTriangles();
        }

        private void DrawQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            BeginTriangles();
            DrawTriangle(a, b, c, color);
            DrawTriangle(a, c, d, color);
            EndTriangles();
        }

        private void BeginTriangles()
        {
            _vectorMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);
            GL.Begin(GL.TRIANGLES);
        }

        private static void DrawTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            GL.Color(color);
            GL.Vertex3(a.x, a.y, 0f);
            GL.Vertex3(b.x, b.y, 0f);
            GL.Vertex3(c.x, c.y, 0f);
        }

        private static void EndTriangles()
        {
            GL.End();
            GL.PopMatrix();
        }

        private float ValueToAngle(float value, float maxValue)
        {
            var normalized = Mathf.Clamp01(value / maxValue);
            return GaugeStartAngle + (GaugeSweepAngle * normalized);
        }

        private static Vector2 AngleToDirection(float angle)
        {
            var radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(radians), -Mathf.Cos(radians));
        }

        private Color GetCurrentWeightColor(WeightState state)
        {
            switch (state)
            {
                case WeightState.Overweight:
                    return _config.OverweightColor.Value;
                case WeightState.CriticallyOverweight:
                    return _config.CriticalWeightColor.Value;
                case WeightState.MaxWeight:
                    return _config.MaxWeightColor.Value;
                default:
                    return _config.SafeWeightColor.Value;
            }
        }

        private void DrawShadowedLabel(Rect rect, string text, GUIStyle style, Color color, float scale)
        {
            var shadowRect = rect;
            shadowRect.position += new Vector2(1f, 1f) * scale;

            style.normal.textColor = new Color(0f, 0f, 0f, 0.55f);
            GUI.Label(shadowRect, text, style);

            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        private void DrawRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _pixel);
            GUI.color = previousColor;
        }

        private struct Layout
        {
            public BreakdownPlacement Placement;
            public Rect PanelRect;
            public Rect HeaderRect;
            public Rect BadgeRect;
            public Rect GaugeRect;
            public Rect BreakdownRect;
        }
    }
}
