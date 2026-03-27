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
                    return ResolveRaidContext(localGame);
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

        private static WeightRuntimeContext ResolveRaidContext(LocalGame localGame)
        {
            var activeProfile = TryGetRaidProfile(localGame);
            if (activeProfile == null)
            {
                return new WeightRuntimeContext();
            }

            var activePlayer = TryGetRaidPlayer(localGame, activeProfile.ProfileId);
            return BuildContext(activeProfile, activePlayer, HudContextType.Raid);
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

        private static Profile TryGetRaidProfile(LocalGame localGame)
        {
            try
            {
                if (localGame.Profile_0 != null)
                {
                    return localGame.Profile_0;
                }
            }
            catch
            {
            }

            try
            {
                if (localGame.PlayerOwner?.Player?.Profile != null)
                {
                    return localGame.PlayerOwner.Player.Profile;
                }
            }
            catch
            {
            }

            try
            {
                var profileId = localGame.ProfileId;
                return localGame.AllPlayers?
                    .FirstOrDefault(player => player != null && player.Profile != null && player.Profile.ProfileId == profileId)
                    ?.Profile;
            }
            catch
            {
                return null;
            }
        }

        private static Player TryGetRaidPlayer(LocalGame localGame, string profileId)
        {
            try
            {
                if (!string.IsNullOrEmpty(profileId))
                {
                    var matchedPlayer = localGame.AllPlayers?
                        .FirstOrDefault(player => player != null && player.Profile != null && player.Profile.ProfileId == profileId);
                    if (matchedPlayer != null)
                    {
                        return matchedPlayer;
                    }
                }
            }
            catch
            {
            }

            try
            {
                if (localGame.LocalPlayer_0?.Profile != null && ProfilesMatch(localGame.LocalPlayer_0.Profile, profileId))
                {
                    return localGame.LocalPlayer_0;
                }
            }
            catch
            {
            }

            try
            {
                var ownerPlayer = localGame.PlayerOwner?.Player;
                if (ownerPlayer?.Profile != null && ProfilesMatch(ownerPlayer.Profile, profileId))
                {
                    return ownerPlayer;
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

        private static bool ProfilesMatch(Profile profile, string profileId)
        {
            return profile != null && !string.IsNullOrEmpty(profileId) && profile.ProfileId == profileId;
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
    }
}
