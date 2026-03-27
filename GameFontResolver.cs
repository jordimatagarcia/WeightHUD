using System;
using System.Linq;
using TMPro;
using UnityEngine;

namespace JordiXIII.WeightHUD
{
    internal sealed class GameFontResolver
    {
        private static readonly string[] FallbackNames =
        {
            "Bender",
            "BenderNormal",
            "Bender Normal",
            "Segoe",
            "LiberationSans"
        };

        private Font _cachedFont;
        private float _nextRetryAt;

        public Font Resolve(string preferredFontName)
        {
            if (_cachedFont != null)
            {
                return _cachedFont;
            }

            if (Time.unscaledTime < _nextRetryAt)
            {
                return GUI.skin?.font;
            }

            _nextRetryAt = Time.unscaledTime + 5f;
            _cachedFont = FindLoadedFont(preferredFontName) ?? FindFontFromTmp(preferredFontName) ?? GUI.skin?.font;
            return _cachedFont;
        }

        private static Font FindLoadedFont(string preferredFontName)
        {
            var fonts = Resources.FindObjectsOfTypeAll<Font>();
            return FindFontByName(fonts, preferredFontName);
        }

        private static Font FindFontFromTmp(string preferredFontName)
        {
            var assets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            var preferred = string.IsNullOrWhiteSpace(preferredFontName) ? null : preferredFontName.Trim();

            if (!string.IsNullOrEmpty(preferred))
            {
                var exact = assets.FirstOrDefault(asset => NameMatches(asset?.name, preferred));
                if (exact?.sourceFontFile != null)
                {
                    return exact.sourceFontFile;
                }
            }

            foreach (var fallback in FallbackNames)
            {
                var match = assets.FirstOrDefault(asset => NameMatches(asset?.name, fallback));
                if (match?.sourceFontFile != null)
                {
                    return match.sourceFontFile;
                }
            }

            return assets.FirstOrDefault(asset => asset?.sourceFontFile != null)?.sourceFontFile;
        }

        private static Font FindFontByName(Font[] fonts, string preferredFontName)
        {
            var preferred = string.IsNullOrWhiteSpace(preferredFontName) ? null : preferredFontName.Trim();

            if (!string.IsNullOrEmpty(preferred))
            {
                var exact = fonts.FirstOrDefault(font => NameMatches(font?.name, preferred));
                if (exact != null)
                {
                    return exact;
                }
            }

            foreach (var fallback in FallbackNames)
            {
                var match = fonts.FirstOrDefault(font => NameMatches(font?.name, fallback));
                if (match != null)
                {
                    return match;
                }
            }

            return fonts.FirstOrDefault();
        }

        private static bool NameMatches(string value, string expected)
        {
            return !string.IsNullOrEmpty(value) &&
                   value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
