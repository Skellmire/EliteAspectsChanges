using System;
using BepInEx;
using RoR2;
using R2API;
using UnityEngine;
using Starstorm2;
using Starstorm2.Cores;
using System.Security;
using System.Security.Permissions;
using EliteAspectsChanges;
using Cloudburst;
using Cloudburst.Cores;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace EliteAspectsChanges
{
    public class SS2Compatibility
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.TeamMoonstorm.Starstorm2");
                }
                return (bool)_enabled;
            }
        }

        public static void AffixVoid()
        {
            if (Starstorm.EnableElites.Value)
            {
                if (EliteAspectsChanges.AffixVoidEquip == EquipmentIndex.None)
                {
                    EliteAspectsChanges.AffixVoidEquip = EquipmentCatalog.FindEquipmentIndex("AffixVoid");
                    if (EliteAspectsChanges.AffixVoidEquip != EquipmentIndex.None)
                    {
                        EliteAspectsChanges.AffixVoidIndex = EliteCatalog.GetEquipmentEliteIndex(EliteAspectsChanges.AffixVoidEquip);
                        EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(EliteAspectsChanges.AffixVoidEquip);
                        EliteAspectsChanges.AffixVoidBuff = equipmentDef.passiveBuff;
                    }
                }
            }
        }

        public static void ImmediateVoidWardUpdate(RoR2.CharacterBody self)
        {
            if (self.inventory.GetItemCount(Assets.AffixVoidItemIndex) > 0)
            {
                self.AddItemBehavior<Starstorm2.Cores.Elites.VoidElite.AffixVoidBehavior>(self.inventory.GetItemCount(Assets.AffixVoidItemIndex));
            }
        }
    }

    public class CloudburstCompatibility
    {
        private static bool? _enabled;

        public static bool enabled
        {
            get
            {
                if (_enabled == null)
                {
                    _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.TeamCloudburst.Cloudburst");
                }
                return (bool)_enabled;
            }
        }
        public static void AffixOrange()
        {
            if (CloudburstPlugin.EnableElites.Value)
            {
                if (EliteAspectsChanges.AffixOrangeEquip == EquipmentIndex.None)
                {
                    EliteAspectsChanges.AffixOrangeEquip = EquipmentCatalog.FindEquipmentIndex("AffixOrange");
                    if (EliteAspectsChanges.AffixOrangeEquip != EquipmentIndex.None)
                    {
                        EliteAspectsChanges.AffixOrangeIndex = EliteCatalog.GetEquipmentEliteIndex(EliteAspectsChanges.AffixOrangeEquip);
                        EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(EliteAspectsChanges.AffixOrangeEquip);
                        EliteAspectsChanges.AffixOrangeBuff = equipmentDef.passiveBuff;
                    }
                }
            }
        }
        public static void ImmediateWarWardUpdate(RoR2.CharacterBody self)
        {
            if (self.inventory.GetItemCount(Assets.AffixOrangeItemIndex) > 0)
            {
                self.AddItemBehavior<AffixWarBehavior>(self.inventory.GetItemCount(Assets.AffixOrangeItemIndex));
            }
        }

        public static void RubyMatUpdate(CharacterBody self)
        {
            if (self && self.inventory)
            {
                EliteIndex equipmentEliteIndex = RoR2.EliteCatalog.GetEquipmentEliteIndex(self.inventory.GetEquipmentIndex());
                if (self && equipmentEliteIndex == EliteIndex.None && self.isElite)
                {
                    int itemCount = self.inventory.GetItemCount(Assets.AffixOrangeItemIndex);
                    if (itemCount > 0)
                    {
                        equipmentEliteIndex = EliteAspectsChanges.AffixOrangeIndex;
                    }
                }
                RoR2.CharacterModel characterModelFromCharacterBody = CloudUtils.GetCharacterModelFromCharacterBody(self);
                if (equipmentEliteIndex == EliteAspectsChanges.AffixOrangeIndex && !self.gameObject.GetComponent<DestroyEffectOnBuffEnd>() && characterModelFromCharacterBody)
                {
                    DestroyEffectOnBuffEnd destroyEffectOnBuffEnd = self.gameObject.AddComponent<DestroyEffectOnBuffEnd>();
                    destroyEffectOnBuffEnd.body = self;
                    destroyEffectOnBuffEnd.buff = EliteAspectsChanges.AffixOrangeBuff;
                    RoR2.TemporaryOverlay temporaryOverlay = characterModelFromCharacterBody.gameObject.AddComponent<RoR2.TemporaryOverlay>();
                    temporaryOverlay.duration = float.PositiveInfinity;
                    temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    temporaryOverlay.animateShaderAlpha = true;
                    temporaryOverlay.destroyComponentOnEnd = true;
                    temporaryOverlay.originalMaterial = Cloudburst.Cores.AssetsCore.mainAssetBundle.LoadAsset<Material>("Assets/Cloudburst/753network/Crystallize/Ruby.mat");
                    temporaryOverlay.AddToCharacerModel(characterModelFromCharacterBody);
                    destroyEffectOnBuffEnd.effect = temporaryOverlay;
                }
            }
        }
    }
}
