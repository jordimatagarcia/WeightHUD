using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Utils;
using System;
using System.Linq;
using System.Reflection;

namespace JordiXIII.WeightHUD
{
    internal sealed class WeightContextResolver
    {
        private static readonly Type ProfileInterface = typeof(ISession).GetInterfaces().FirstOrDefault(i =>
        {
            var properties = i.GetProperties();
            return properties.Length == 2 && properties.Any(p => p.Name == "Profile");
        });

        private static readonly PropertyInfo SessionProfileProperty =
            ProfileInterface == null ? null : AccessTools.Property(ProfileInterface, "Profile");

        public WeightRuntimeContext Resolve()
        {
            try
            {
                if (Singleton<AbstractGame>.Instance is LocalGame localGame)
                {
                    var localPlayer = TryGetLocalPlayer(localGame);
                    if (localPlayer != null)
                    {
                        return BuildContext(localPlayer.Profile, localPlayer, HudContextType.Raid);
                    }

                    // Never fall back to the menu PMC profile while a raid is active.
                    return new WeightRuntimeContext();
                }
            }
            catch
            {
                return new WeightRuntimeContext();
            }

            var sessionProfile = TryGetSessionProfile();
            if (sessionProfile != null)
            {
                return BuildContext(sessionProfile, null, HudContextType.MainMenu);
            }

            return new WeightRuntimeContext();
        }

        private static WeightRuntimeContext BuildContext(Profile profile, Player player, HudContextType contextType)
        {
            if (profile == null)
            {
                return new WeightRuntimeContext();
            }

            return new WeightRuntimeContext
            {
                Profile = profile,
                Player = player,
                Inventory = profile.Inventory,
                Skills = profile.Skills,
                Role = profile.Side == EPlayerSide.Savage ? HudPlayerRole.Scav : HudPlayerRole.Pmc,
                ContextType = contextType
            };
        }

        private static Profile TryGetSessionProfile()
        {
            try
            {
                var app = ClientAppUtils.GetMainApp();
                var session = app?.GetClientBackEndSession();
                return SessionProfileProperty?.GetValue(session) as Profile;
            }
            catch
            {
                return null;
            }
        }

        private static Player TryGetLocalPlayer(LocalGame localGame)
        {
            if (localGame == null)
            {
                return null;
            }

            try
            {
                var directLocalPlayer = localGame.LocalPlayer_0;
                if (directLocalPlayer != null)
                {
                    return directLocalPlayer;
                }
            }
            catch
            {
            }

            try
            {
                var playerFromOwner = localGame.PlayerOwner?.Player;
                if (playerFromOwner != null)
                {
                    return playerFromOwner;
                }
            }
            catch
            {
            }

            try
            {
                return localGame.AllPlayers?.FirstOrDefault(player => player != null && player.IsYourPlayer) ??
                       localGame.AllPlayers?.FirstOrDefault(player => player != null && !player.IsAI && player.HasGamePlayerOwner);
            }
            catch
            {
                return null;
            }
        }
    }
}
