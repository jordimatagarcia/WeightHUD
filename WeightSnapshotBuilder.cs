using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace JordiXIII.WeightHUD
{
    internal sealed class WeightSnapshotBuilder
    {
        private static readonly FieldInfo SkillManagerStrengthBuffEliteField = AccessTools.Field(typeof(SkillManager), "StrengthBuffElite");
        private static readonly FieldInfo InventoryTotalWeightEliteSkillField = AccessTools.Field(typeof(Inventory), "TotalWeightEliteSkill");
        private static readonly FieldInfo InventoryTotalWeightField = AccessTools.Field(typeof(Inventory), "TotalWeight");
        private static readonly PropertyInfo FloatWrapperValueProperty = AccessTools.Property(InventoryTotalWeightField.FieldType, "Value");
        private static readonly FieldInfo BoolWrapperValueField = AccessTools.Field(SkillManagerStrengthBuffEliteField.FieldType, "Value");

        private static readonly FieldInfo PlayerPhysicalField = AccessTools.Field(typeof(Player), "Physical");
        private static readonly PropertyInfo PhysicalOverweightProperty = AccessTools.Property(PlayerPhysicalField.FieldType, "Overweight");
        private static readonly PropertyInfo PhysicalWalkOverweightProperty = AccessTools.Property(PlayerPhysicalField.FieldType, "WalkOverweight");
        private static readonly PropertyInfo PhysicalWalkOverweightLimitsProperty = AccessTools.Property(PlayerPhysicalField.FieldType, "WalkOverweightLimits");

        private static readonly EquipmentSlot[] WeaponSlots =
        {
            EquipmentSlot.FirstPrimaryWeapon,
            EquipmentSlot.SecondPrimaryWeapon,
            EquipmentSlot.Holster,
            EquipmentSlot.Scabbard
        };

        private readonly WeightThresholdGlobals _globals;

        public WeightSnapshotBuilder(WeightThresholdGlobals globals)
        {
            _globals = globals ?? WeightThresholdGlobals.Defaults;
        }

        public WeightSnapshot Build(WeightRuntimeContext context)
        {
            if (context == null || !context.IsValid)
            {
                return WeightSnapshot.Empty;
            }

            var hasEliteStrength = ReadEliteStrength(context.Skills);
            var totalWeight = ReadTotalWeight(context.Inventory, hasEliteStrength);
            var breakdown = BuildBreakdown(context.Inventory, hasEliteStrength);
            var thresholds = ResolveThresholds(context);

            return new WeightSnapshot
            {
                IsValid = true,
                Role = context.Role,
                ContextType = context.ContextType,
                HasEliteStrength = hasEliteStrength,
                CurrentWeight = totalWeight,
                EquipmentWeight = breakdown.EquipmentWeight,
                WeaponWeight = breakdown.WeaponWeight,
                BackpackWeight = breakdown.BackpackWeight,
                OverweightThreshold = thresholds.Overweight,
                SlowWalkThreshold = thresholds.SlowWalk,
                MaxCarryThreshold = thresholds.MaxCarry,
                State = ResolveState(totalWeight, thresholds.Overweight, thresholds.SlowWalk, thresholds.MaxCarry),
                RoleLabel = context.Role == HudPlayerRole.Scav ? "SCAV" : "PMC",
                ContextLabel = string.Empty
            };
        }

        private static bool ReadEliteStrength(SkillManager skills)
        {
            var eliteWrapper = SkillManagerStrengthBuffEliteField?.GetValue(skills);
            return eliteWrapper != null && (bool)(BoolWrapperValueField?.GetValue(eliteWrapper) ?? false);
        }

        private static float ReadTotalWeight(Inventory inventory, bool hasEliteStrength)
        {
            var field = hasEliteStrength ? InventoryTotalWeightEliteSkillField : InventoryTotalWeightField;
            var wrapper = field?.GetValue(inventory);
            return wrapper == null ? 0f : (float)(FloatWrapperValueProperty?.GetValue(wrapper) ?? 0f);
        }

        private static WeightBreakdown BuildBreakdown(Inventory inventory, bool hasEliteStrength)
        {
            var equipment = inventory?.Equipment;
            if (equipment == null)
            {
                return default;
            }

            var backpackWeight = GetSlotWeight(equipment, EquipmentSlot.Backpack);
            var weaponWeight = 0f;

            foreach (var slot in WeaponSlots)
            {
                weaponWeight += GetSlotWeight(equipment, slot);
            }

            var equipmentWeight = 0f;
            foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slot == EquipmentSlot.Backpack || Array.IndexOf(WeaponSlots, slot) >= 0)
                {
                    continue;
                }

                equipmentWeight += GetSlotWeight(equipment, slot);
            }

            return new WeightBreakdown
            {
                EquipmentWeight = equipmentWeight,
                WeaponWeight = hasEliteStrength ? 0f : weaponWeight,
                BackpackWeight = backpackWeight
            };
        }

        private static float GetSlotWeight(InventoryEquipment equipment, EquipmentSlot slot)
        {
            var slotObject = equipment.GetSlot(slot);
            var item = slotObject?.ContainedItem;
            return item?.TotalWeight ?? 0f;
        }

        private ThresholdSet ResolveThresholds(WeightRuntimeContext context)
        {
            if (context.Player != null && TryReadLiveThresholds(context.Player, out var liveThresholds))
            {
                return liveThresholds;
            }

            var modifier = Mathf.Max(-0.95f, context.Skills.CarryingWeightRelativeModifier);
            return new ThresholdSet
            {
                Overweight = _globals.BaseOverweightThreshold * (1f + modifier),
                SlowWalk = _globals.SlowWalkThreshold * (1f + modifier),
                MaxCarry = _globals.MaxCarryThreshold * (1f + modifier)
            };
        }

        private static bool TryReadLiveThresholds(Player player, out ThresholdSet thresholds)
        {
            thresholds = default;

            var physical = PlayerPhysicalField?.GetValue(player);
            if (physical == null)
            {
                return false;
            }

            try
            {
                var overweight = (float)(PhysicalOverweightProperty?.GetValue(physical) ?? 0f);
                var slowWalk = (float)(PhysicalWalkOverweightProperty?.GetValue(physical) ?? 0f);
                var walkLimits = (Vector2)(PhysicalWalkOverweightLimitsProperty?.GetValue(physical) ?? Vector2.zero);
                var maxCarry = walkLimits.y > 0f ? walkLimits.y : slowWalk;

                if (overweight <= 0f || slowWalk <= 0f || maxCarry <= 0f)
                {
                    return false;
                }

                thresholds = new ThresholdSet
                {
                    Overweight = overweight,
                    SlowWalk = slowWalk,
                    MaxCarry = maxCarry
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static WeightState ResolveState(float currentWeight, float overweight, float slowWalk, float maxCarry)
        {
            if (currentWeight >= maxCarry)
            {
                return WeightState.MaxCarry;
            }

            if (currentWeight >= slowWalk)
            {
                return WeightState.SlowWalk;
            }

            if (currentWeight >= overweight)
            {
                return WeightState.Overweight;
            }

            return WeightState.Normal;
        }

        private struct WeightBreakdown
        {
            public float EquipmentWeight;
            public float WeaponWeight;
            public float BackpackWeight;
        }

        private struct ThresholdSet
        {
            public float Overweight;
            public float SlowWalk;
            public float MaxCarry;
        }
    }
}
