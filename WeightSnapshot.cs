using EFT;

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
        public EFT.InventoryLogic.Inventory Inventory { get; set; }
        public SkillManager Skills { get; set; }
        public HudPlayerRole Role { get; set; }
        public HudContextType ContextType { get; set; }

        public bool IsValid => Profile != null && Inventory != null && Skills != null;
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
