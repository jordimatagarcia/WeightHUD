using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SPT.Reflection.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
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
        private static readonly PropertyInfo SessionProfileOfPetProperty =
            ProfileInterface == null ? null : AccessTools.Property(ProfileInterface, "ProfileOfPet");

        private static readonly string[] PlayerMemberCandidates =
        {
            "LocalPlayer_0",
            "LocalPlayer",
            "MainPlayer",
            "Player"
        };

        private static readonly string[] ProfileMemberCandidates =
        {
            "Profile_0",
            "Profile",
            "MainProfile"
        };

        public WeightRuntimeContext Resolve()
        {
            var game = TryGetAbstractGame();
            if (game != null)
            {
                return ResolveRaidContext(game);
            }

            var sessionProfile = TryGetSessionProfile();
            if (sessionProfile != null)
            {
                return BuildContext(sessionProfile, null, HudContextType.MainMenu, "MainMenu", null, sessionProfile.ProfileId, null, sessionProfile.ProfileId, null, "SessionPMC");
            }

            return new WeightRuntimeContext();
        }

        private static AbstractGame TryGetAbstractGame()
        {
            try { return Singleton<AbstractGame>.Instance; }
            catch { return null; }
        }

        private static WeightRuntimeContext ResolveRaidContext(AbstractGame game)
        {
            var sessionProfile = TryGetSessionProfile();
            var scavProfile = TryGetScavProfile();
            var gameTypeName = game.GetType().FullName ?? game.GetType().Name;
            var raidProfileId = ReadStringMemberValue(game, "ProfileId");

            var activePlayer = TryGetRaidPlayer(game, raidProfileId, out var playerSource);
            var activeProfile = activePlayer?.Profile;
            var resolutionSource = playerSource;

            if (activeProfile == null)
            {
                activeProfile = TryGetRaidProfile(game, raidProfileId, sessionProfile, scavProfile, out resolutionSource);
            }

            if (activeProfile == null)
            {
                return new WeightRuntimeContext
                {
                    ContextType = HudContextType.Raid,
                    GameTypeName = gameTypeName,
                    RaidProfileId = raidProfileId,
                    SessionPmcProfileId = sessionProfile?.ProfileId,
                    SessionScavProfileId = scavProfile?.ProfileId,
                    ResolutionSource = resolutionSource ?? "Unresolved"
                };
            }

            return BuildContext(
                activeProfile,
                activePlayer,
                HudContextType.Raid,
                gameTypeName,
                raidProfileId,
                activeProfile.ProfileId,
                activePlayer?.Profile?.ProfileId,
                sessionProfile?.ProfileId,
                scavProfile?.ProfileId,
                resolutionSource);
        }

        private static WeightRuntimeContext BuildContext(
            Profile profile,
            Player player,
            HudContextType contextType,
            string gameTypeName,
            string raidProfileId,
            string resolvedProfileId,
            string resolvedPlayerProfileId,
            string sessionPmcProfileId,
            string sessionScavProfileId,
            string resolutionSource)
        {
            if (profile == null)
            {
                return new WeightRuntimeContext();
            }

            return new WeightRuntimeContext
            {
                Profile = profile,
                Player = player,
                HealthController = player?.HealthController,
                Inventory = profile.Inventory,
                Skills = profile.Skills,
                Role = profile.Side == EPlayerSide.Savage ? HudPlayerRole.Scav : HudPlayerRole.Pmc,
                ContextType = contextType,
                GameTypeName = gameTypeName,
                RaidProfileId = raidProfileId,
                ResolvedProfileId = resolvedProfileId,
                ResolvedPlayerProfileId = resolvedPlayerProfileId,
                SessionPmcProfileId = sessionPmcProfileId,
                SessionScavProfileId = sessionScavProfileId,
                ResolutionSource = resolutionSource
            };
        }

        private static Profile TryGetRaidProfile(object game, string raidProfileId, Profile sessionProfile, Profile scavProfile, out string source)
        {
            source = null;

            if (!string.IsNullOrEmpty(raidProfileId))
            {
                if (ProfileMatches(scavProfile, raidProfileId))
                {
                    source = "SessionSCAV";
                    return scavProfile;
                }

                if (ProfileMatches(sessionProfile, raidProfileId))
                {
                    source = "SessionPMC";
                    return sessionProfile;
                }
            }

            var allPlayersProfile = EnumeratePlayers(game)
                .FirstOrDefault(player => ProfileMatches(player?.Profile, raidProfileId))
                ?.Profile;
            if (allPlayersProfile != null)
            {
                source = "AllPlayers";
                return allPlayersProfile;
            }

            var ownerPlayer = TryGetPlayerOwnerPlayer(game);
            if (ownerPlayer?.Profile != null && (string.IsNullOrEmpty(raidProfileId) || ProfileMatches(ownerPlayer.Profile, raidProfileId)))
            {
                source = string.IsNullOrEmpty(raidProfileId) ? "PlayerOwnerFallback" : "PlayerOwner";
                return ownerPlayer.Profile;
            }

            foreach (var memberName in ProfileMemberCandidates)
            {
                var profile = ReadReferenceMemberValue<Profile>(game, memberName);
                if (profile != null && (string.IsNullOrEmpty(raidProfileId) || ProfileMatches(profile, raidProfileId)))
                {
                    source = string.IsNullOrEmpty(raidProfileId) ? memberName + "Fallback" : memberName;
                    return profile;
                }
            }

            foreach (var memberName in PlayerMemberCandidates)
            {
                var player = ReadReferenceMemberValue<Player>(game, memberName);
                if (player?.Profile != null && (string.IsNullOrEmpty(raidProfileId) || ProfileMatches(player.Profile, raidProfileId)))
                {
                    source = string.IsNullOrEmpty(raidProfileId) ? memberName + "Fallback" : memberName;
                    return player.Profile;
                }
            }

            var yourPlayerProfile = EnumeratePlayers(game)
                .FirstOrDefault(player => player != null && player.IsYourPlayer)
                ?.Profile;
            if (yourPlayerProfile != null)
            {
                source = string.IsNullOrEmpty(raidProfileId) ? "AllPlayers.IsYourPlayerFallback" : "AllPlayers.IsYourPlayer";
                return yourPlayerProfile;
            }

            source = sessionProfile != null ? "SessionPMCFallback" : "Unresolved";
            return sessionProfile;
        }

        private static Player TryGetRaidPlayer(object game, string raidProfileId, out string source)
        {
            source = null;

            var matchedPlayer = EnumeratePlayers(game)
                .FirstOrDefault(player => !string.IsNullOrEmpty(raidProfileId) && PlayerMatches(player, raidProfileId));
            if (matchedPlayer != null)
            {
                source = "AllPlayers";
                return matchedPlayer;
            }

            foreach (var memberName in PlayerMemberCandidates)
            {
                var player = ReadReferenceMemberValue<Player>(game, memberName);
                if (player != null && (string.IsNullOrEmpty(raidProfileId) || PlayerMatches(player, raidProfileId)))
                {
                    source = string.IsNullOrEmpty(raidProfileId) ? memberName + "Fallback" : memberName;
                    return player;
                }
            }

            var ownerPlayer = TryGetPlayerOwnerPlayer(game);
            if (ownerPlayer != null && (string.IsNullOrEmpty(raidProfileId) || PlayerMatches(ownerPlayer, raidProfileId)))
            {
                source = string.IsNullOrEmpty(raidProfileId) ? "PlayerOwnerFallback" : "PlayerOwner";
                return ownerPlayer;
            }

            var yourPlayer = EnumeratePlayers(game).FirstOrDefault(player => player != null && player.IsYourPlayer);
            if (yourPlayer != null)
            {
                source = string.IsNullOrEmpty(raidProfileId) ? "AllPlayers.IsYourPlayerFallback" : "AllPlayers.IsYourPlayer";
                return yourPlayer;
            }

            var nonAiPlayer = EnumeratePlayers(game).FirstOrDefault(player => player != null && !player.IsAI && player.HasGamePlayerOwner);
            if (nonAiPlayer != null)
            {
                source = string.IsNullOrEmpty(raidProfileId) ? "AllPlayers.NonAiOwnerFallback" : "AllPlayers.NonAiOwner";
                return nonAiPlayer;
            }

            source = string.IsNullOrEmpty(raidProfileId) ? "UnresolvedFallback" : "RaidProfileIdNoPlayerMatch";
            return null;
        }

        private static IEnumerable<Player> EnumeratePlayers(object game)
        {
            var players = ReadEnumerableMemberValue(game, "AllPlayers");
            if (players == null)
            {
                yield break;
            }

            foreach (var item in players)
            {
                if (item is Player player)
                {
                    yield return player;
                }
            }
        }

        private static Player TryGetPlayerOwnerPlayer(object game)
        {
            var owner = ReadReferenceMemberValue<object>(game, "PlayerOwner");
            return ReadReferenceMemberValue<Player>(owner, "Player");
        }

        private static T ReadReferenceMemberValue<T>(object instance, string memberName) where T : class
        {
            if (instance == null || string.IsNullOrEmpty(memberName))
            {
                return null;
            }

            try
            {
                var type = instance.GetType();
                var property = AccessTools.Property(type, memberName);
                if (property != null)
                {
                    return property.GetValue(instance) as T;
                }

                var field = AccessTools.Field(type, memberName);
                if (field != null)
                {
                    return field.GetValue(instance) as T;
                }
            }
            catch
            {
            }

            return null;
        }

        private static IEnumerable ReadEnumerableMemberValue(object instance, string memberName)
        {
            if (instance == null || string.IsNullOrEmpty(memberName))
            {
                return null;
            }

            try
            {
                var type = instance.GetType();
                var property = AccessTools.Property(type, memberName);
                if (property != null)
                {
                    return property.GetValue(instance) as IEnumerable;
                }

                var field = AccessTools.Field(type, memberName);
                if (field != null)
                {
                    return field.GetValue(instance) as IEnumerable;
                }
            }
            catch
            {
            }

            return null;
        }

        private static string ReadStringMemberValue(object instance, string memberName)
        {
            if (instance == null || string.IsNullOrEmpty(memberName))
            {
                return null;
            }

            try
            {
                var type = instance.GetType();
                var property = AccessTools.Property(type, memberName);
                if (property != null)
                {
                    return property.GetValue(instance) as string;
                }

                var field = AccessTools.Field(type, memberName);
                if (field != null)
                {
                    return field.GetValue(instance) as string;
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool ProfileMatches(Profile profile, string profileId)
        {
            return profile != null && !string.IsNullOrEmpty(profileId) && profile.ProfileId == profileId;
        }

        private static bool PlayerMatches(Player player, string profileId)
        {
            return player?.Profile != null && !string.IsNullOrEmpty(profileId) && player.Profile.ProfileId == profileId;
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

        private static Profile TryGetScavProfile()
        {
            try
            {
                var app = ClientAppUtils.GetMainApp();
                var session = app?.GetClientBackEndSession();
                return SessionProfileOfPetProperty?.GetValue(session) as Profile;
            }
            catch
            {
                return null;
            }
        }
    }
}
