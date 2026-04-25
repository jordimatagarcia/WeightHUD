using EFT;
using EFT.HealthSystem;

namespace JordiXIII.WeightHUD
{
    internal enum HudPlayerRole
    {
        Unknown,
        Pmc,
        Scav
    }

    internal enum HudContextType
    {
        None,
        MainMenu,
        Raid
    }

    internal enum WeightState
    {
        Safe,
        Overweight,
        CriticallyOverweight,
        MaxWeight
    }

    internal sealed class WeightRuntimeContext
    {
        public Profile Profile { get; set; }
        public Player Player { get; set; }
        public IHealthController HealthController { get; set; }
        public EFT.InventoryLogic.Inventory Inventory { get; set; }
        public SkillManager Skills { get; set; }
        public HudPlayerRole Role { get; set; }
        public HudContextType ContextType { get; set; }
        public string GameTypeName { get; set; }
        public string RaidProfileId { get; set; }
        public string ResolvedProfileId { get; set; }
        public string ResolvedPlayerProfileId { get; set; }
        public string SessionPmcProfileId { get; set; }
        public string SessionScavProfileId { get; set; }
        public string ResolutionSource { get; set; }

        public bool IsValid => Profile != null && Inventory != null && Skills != null;

        public string DiagnosticSignature => $"{GameTypeName}|{ContextType}|{Role}|{RaidProfileId}|{ResolvedProfileId}|{ResolvedPlayerProfileId}|{ResolutionSource}";

        public string DiagnosticSummary =>
            $"GameType={GameTypeName ?? "<null>"}, Context={ContextType}, Role={Role}, RaidProfileId={RaidProfileId ?? "<null>"}, ResolvedProfileId={ResolvedProfileId ?? "<null>"}, ResolvedPlayerProfileId={ResolvedPlayerProfileId ?? "<null>"}, SessionPMC={SessionPmcProfileId ?? "<null>"}, SessionSCAV={SessionScavProfileId ?? "<null>"}, Source={ResolutionSource ?? "<unknown>"}";
    }

    internal sealed class WeightSnapshot
    {
        public static readonly WeightSnapshot Empty = new WeightSnapshot
        {
            IsValid = false,
            RoleLabel = "NO DATA",
            ContextLabel = string.Empty
        };

        public bool IsValid { get; set; }
        public HudPlayerRole Role { get; set; }
        public HudContextType ContextType { get; set; }
        public bool HasEliteStrength { get; set; }

        public float CurrentWeight { get; set; }
        public float EquipmentWeight { get; set; }
        public float WeaponWeight { get; set; }
        public float BackpackWeight { get; set; }

        public float OverweightThreshold { get; set; }
        public float CriticalOverweightThreshold { get; set; }
        public float MaxWeightThreshold { get; set; }

        public WeightState State { get; set; }
        public string RoleLabel { get; set; }
        public string ContextLabel { get; set; }
    }
}
