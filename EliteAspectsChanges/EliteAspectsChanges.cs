using System;
using System.Collections.Generic;
using BepInEx;
using RoR2;
using R2API;
using R2API.Utils;
using AspectsToItems;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using BepInEx.Configuration;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace EliteAspectsChanges
{
	[BepInDependency("com.bepis.r2api")]
	[R2APISubmoduleDependency("LanguageAPI", "BuffAPI")]
	[BepInDependency("com.Skell.AspectsToItems")]
	[BepInPlugin("com.Skell.EliteAspectsChanges", "Elite Aspects Changes", "2.0.0")]
	public class EliteAspectsChanges : BaseUnityPlugin
	{
		public void Awake()
		{
			ConfigInit();

			AspectsToItems.AspectsToItems.YellowAspectDef(Resources.Load<EquipmentDef>("equipmentdefs/AffixLunar"), AffixLunarTokens);
			AspectsToItems.AspectsToItems.YellowAspectDef(Resources.Load<EquipmentDef>("equipmentdefs/AffixPoison"), AffixPoisonTokens);
			AspectsToItems.AspectsToItems.YellowAspectDef(Resources.Load<EquipmentDef>("equipmentdefs/AffixHaunted"), AffixHauntedTokens);
			AspectsToItems.AspectsToItems.YellowAspectDef(Resources.Load<EquipmentDef>("equipmentdefs/AffixRed"), AffixRedTokens);
			AspectsToItems.AspectsToItems.YellowAspectDef(Resources.Load<EquipmentDef>("equipmentdefs/AffixBlue"), AffixBlueTokens);
			AspectsToItems.AspectsToItems.YellowAspectDef(Resources.Load<EquipmentDef>("equipmentdefs/AffixWhite"), AffixWhiteTokens);

			if (AffixWhiteToggle.Value)
			{
				FreezingBlood = ScriptableObject.CreateInstance<BuffDef>();
				FreezingBlood.name = "FreezingBlood";
				FreezingBlood.iconSprite = Resources.Load<BuffDef>("buffdefs/Bleeding").iconSprite;
				FreezingBlood.buffColor = new Color(0.64705884f, 0.87058824f, 0.92941177f);
				FreezingBlood.canStack = true;
				FreezingBlood.isDebuff = true;
				CustomBuff FreezingBloodC = new CustomBuff(FreezingBlood);
				BuffAPI.Add(FreezingBloodC);
			}

			Hook();
		}

		private void Hook()
        {
			IL.RoR2.GlobalEventManager.OnCharacterDeath += DropRateChange;
			if (AffixLunarToggle.Value)
            {
				IL.RoR2.CharacterBody.UpdateAffixLunar += LunarEliteChanges;
            }
			if (AffixPoisonToggle.Value)
			{
				On.RoR2.GlobalEventManager.OnHitEnemy += PoisonEliteChanges;
			}
			if (AffixHauntedToggle.Value)
			{
				On.RoR2.CharacterBody.RecalculateStats += AffixHauntedBuffChanges;
				On.RoR2.CharacterBody.OnInventoryChanged += ImmediateHauntedWardUpdate;
				IL.RoR2.CharacterBody.AffixHauntedBehavior.FixedUpdate += HauntedWardScaling;
			}
			if (AffixRedToggle.Value)
			{
				IL.RoR2.GlobalEventManager.OnHitEnemy += FireEliteChanges;
			}
			if (AffixBlueToggle.Value)
			{
				IL.RoR2.GlobalEventManager.OnHitAll += LightningEliteChanges;
			}
			if (AffixWhiteToggle.Value)
            {
				On.RoR2.HealthComponent.TakeDamage += IceEliteChanges;
			}
		}

        private void LunarEliteChanges(ILContext il)
        {
			ILCursor c = new ILCursor(il);
			c.GotoNext(
				x => x.MatchLdcI4(4),
				x => x.MatchStloc(0)
				);
			c.Index += 1;
			c.Emit(OpCodes.Pop);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<CharacterBody, int>>((self) =>
			{
				int itemCount = self.inventory.GetItemCount(AspectsToItems.AspectsToItems.NewDefsList.Find(x => x.name == "AffixLunar").itemIndex);
				if (itemCount > 0)
                {
					return 2 + 2 * itemCount;
                }
				return 4;
			});
		}

        private static void DropRateChange(ILContext il)
		{
			ILCursor c = new ILCursor(il);
			c.GotoNext(
				x => x.MatchLdcR4(0.025f),
				x => x.MatchLdloc(14)
				);
			c.Next.Operand = ConfigDropRate.Value;
		}

		private void HauntedWardScaling(ILContext il)
		{
			ILCursor c = new ILCursor(il);
			c.GotoNext(
				x => x.MatchLdarg(0),
				x => x.MatchLdfld<RoR2.CharacterBody.AffixHauntedBehavior>("affixHauntedWard"),
				x => x.MatchCallvirt<GameObject>("GetComponent"),
				x => x.MatchLdcR4(30)
				);
			c.Index += 4;
			c.Emit(OpCodes.Pop);
			c.Emit(OpCodes.Ldarg_0);
			c.EmitDelegate<Func<RoR2.CharacterBody.AffixHauntedBehavior, float>>((hauntedAffixBehavior) =>
			{
				if (hauntedAffixBehavior.body.inventory)
				{
					int itemCount = hauntedAffixBehavior.body.inventory.GetItemCount(AspectsToItems.AspectsToItems.NewDefsList.Find(x => x.name == "AffixHaunted").itemIndex);
					if (itemCount > 0)
					{
						return 30f * itemCount;
					}
				}
				return 30f;
			});
		}

		private void LightningEliteChanges(ILContext il)
		{
			ILCursor c = new ILCursor(il);
			c.GotoNext(
				x => x.MatchLdcR4(0.5f),
				x => x.MatchStloc(7)
				);
			c.Index += 1;
			c.Emit(OpCodes.Pop);
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldarg_2);
			c.EmitDelegate<Func<RoR2.DamageInfo, GameObject, float>>((damageInfo, hitObject) =>
			{
				if (damageInfo != null && hitObject != null)
				{
					RoR2.CharacterBody component = damageInfo.attacker.GetComponent<RoR2.CharacterBody>();
					int itemCount = component.inventory.GetItemCount(AspectsToItems.AspectsToItems.NewDefsList.Find(x => x.name == "AffixBlue").itemIndex);
					if (component.master.inventory)
					{
						if (itemCount > 0)
						{
							return 0.25f + 0.25f * itemCount;
						}
					}
				}
				return 0.5f;
			});
		}

		private void FireEliteChanges(ILContext il)
		{
			ILCursor c = new ILCursor(il);
			c.GotoNext(
				x => x.MatchLdarg(2),
				x => x.MatchLdarg(1),
				x => x.MatchLdfld<RoR2.DamageInfo>("attacker"),
				x => x.MatchLdloc(7),
				x => x.MatchBrtrue(out ILLabel IL_0270),
				x => x.MatchLdcI4(1),
				x => x.MatchBr(out ILLabel IL_0271),
				x => x.MatchLdcI4(3),
				x => x.MatchLdcR4(4)
				);
			c.Index += 9;
			c.Emit(OpCodes.Pop);
			c.Emit(OpCodes.Ldarg_1);
			c.Emit(OpCodes.Ldarg_2);
			c.EmitDelegate<Func<RoR2.DamageInfo, GameObject, float>>((damageInfo, victimGameObject) =>
			{
				if (damageInfo != null && victimGameObject != null)
				{
					RoR2.CharacterBody component = damageInfo.attacker.GetComponent<RoR2.CharacterBody>();
					int itemCount = component.inventory.GetItemCount(AspectsToItems.AspectsToItems.NewDefsList.Find(x => x.name == "AffixRed").itemIndex);
					if (component.master.inventory)
					{
						if (itemCount > 0)
						{
							return 2f + 2f * itemCount;
						}
					}
				}
				return 4f;
			});
		}

		private void ImmediateHauntedWardUpdate(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
		{
			orig(self);
			if (self.inventory.GetItemCount(AspectsToItems.AspectsToItems.NewDefsList.Find(x => x.name == "AffixHaunted").itemIndex) > 0)
			{
				self.AddItemBehavior<RoR2.CharacterBody.AffixHauntedBehavior>(self.inventory.GetItemCount(AspectsToItems.AspectsToItems.NewDefsList.Find(x => x.name == "AffixHaunted").itemIndex));
			}
		}

		private void AffixHauntedBuffChanges(On.RoR2.CharacterBody.orig_RecalculateStats orig, RoR2.CharacterBody self)
		{
			orig(self);
			if (self.HasBuff(Resources.Load<BuffDef>("AffixHauntedRecipient")))
			{
				self.armor += 50f;
				self.attackSpeed *= 1.25f;
			}
			if (self.HasBuff(Resources.Load<BuffDef>("AffixHaunted")))
			{
				self.moveSpeed *= 1.25f;
			}
		}

		private void PoisonEliteChanges(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
		{
			if (damageInfo.attacker)
			{
				RoR2.CharacterBody component = damageInfo.attacker.GetComponent<RoR2.CharacterBody>();
				RoR2.CharacterBody characterBody = victim ? victim.GetComponent<RoR2.CharacterBody>() : null;
				if (component)
				{
					RoR2.CharacterMaster master = component.master;
					if (master)
					{
						if ((component.HasBuff(Resources.Load<BuffDef>("buffdefs/AffixPoison")) ? 1 : 0) > 0 && characterBody)
						{
							int itemCount = master.inventory.GetItemCount(AspectsToItems.AspectsToItems.NewDefsList.Find(x => x.name == "AffixPoison").itemIndex);
							float amount = damageInfo.damage * (0.05f + 0.05f * itemCount);
							if (itemCount == 0)
							{
								amount = damageInfo.damage * .1f;
							}
							component.healthComponent.Heal(amount, damageInfo.procChainMask, true);
						}
					}
				}
			}
			orig(self, damageInfo, victim);
		}

		private void IceEliteChanges(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
		{
			RoR2.CharacterBody characterBody = null;
			if (damageInfo.attacker)
			{
				characterBody = damageInfo.attacker.GetComponent<RoR2.CharacterBody>();
			}
			if (damageInfo.damage > 0)
			{
				if (characterBody)
				{
					RoR2.CharacterMaster master = characterBody.master;
					if (master && master.inventory)
					{
						if (damageInfo.procCoefficient > 0f)
						{
							if (characterBody.GetBuffCount(Resources.Load<BuffDef>("buffdefs/AffixWhite")) > 0)
							{
								self.body.AddTimedBuff(FreezingBlood.buffIndex, 2f * damageInfo.procCoefficient);
								if (self.body.GetBuffCount(FreezingBlood.buffIndex) >= 10)
								{
									self.body.ClearTimedBuffs(FreezingBlood.buffIndex);
									RoR2.CharacterBody component = damageInfo.attacker.GetComponent<RoR2.CharacterBody>();
									RoR2.ProcChainMask procChainMask = damageInfo.procChainMask;
									int AffixWhiteStack = master.inventory.GetItemCount(AspectsToItems.AspectsToItems.NewDefsList.Find(x => x.name == "AffixWhite").itemIndex);
									Vector3 position = damageInfo.position;
									float damageCoefficient = 10f + 10f * AffixWhiteStack;
									if (AffixWhiteStack == 0)
									{
										damageCoefficient = 20f;
									}
									float damage2 = RoR2.Util.OnHitProcDamage(component.damage, component.damage, damageCoefficient);
									RoR2.DamageInfo damageInfo2 = new RoR2.DamageInfo
									{
										damage = damage2,
										damageColorIndex = DamageColorIndex.Item,
										damageType = DamageType.Generic,
										attacker = damageInfo.attacker,
										crit = damageInfo.crit,
										force = Vector3.zero,
										inflictor = null,
										position = position,
										procChainMask = procChainMask,
										procCoefficient = 1f
									};
									RoR2.EffectManager.SimpleImpactEffect(Resources.Load<GameObject>("Prefabs/Effects/ImpactEffects/IceCullExplosion"), position, Vector3.up, true);
									self.TakeDamage(damageInfo2);
								}
							}
						}
					}
				}
			}
			orig(self, damageInfo);
		}

		private void ConfigInit()
		{
			ConfigDropRate = Config.Bind<float>(
			"AspectsToItems",
			"Drop Rate",
			.2f,
			"The drop rate of the elite aspects, as a percent."
			);
			AffixLunarToggle = Config.Bind<bool>(
			"EliteAspectsChanges",
			"Lunar Changes Toggle",
			true,
			"Toggle the changes made to Perfected elites in this mod."
			);
			AffixPoisonToggle = Config.Bind<bool>(
			"EliteAspectsChanges",
			"Malachite Changes Toggle",
			true,
			"Toggle the changes made to Malachite elites in this mod."
			);
			AffixHauntedToggle = Config.Bind<bool>(
			"EliteAspectsChanges",
			"Celestine Changes Toggle",
			true,
			"Toggle the changes made to Celestine elites in this mod."
			);
			AffixRedToggle = Config.Bind<bool>(
			"EliteAspectsChanges",
			"Blazing Changes Toggle",
			true,
			"Toggle the changes made to Blazing elites in this mod."
			);
			AffixBlueToggle = Config.Bind<bool>(
			"EliteAspectsChanges",
			"Overloading Changes Toggle",
			true,
			"Toggle the changes made to Overloading elites in this mod."
			);
			AffixWhiteToggle = Config.Bind<bool>(
			"EliteAspectsChanges",
			"Glacial Changes Toggle",
			true,
			"Toggle the changes made to Glacial elites in this mod."
			);
		}
		//Initialization
		private static BuffDef FreezingBlood;

		public static ConfigEntry<float> ConfigDropRate { get; set; }
		public static ConfigEntry<bool> AffixLunarToggle { get; set; }
		public static ConfigEntry<bool> AffixPoisonToggle { get; set; }
		public static ConfigEntry<bool> AffixHauntedToggle { get; set; }
		public static ConfigEntry<bool> AffixRedToggle { get; set; }
		public static ConfigEntry<bool> AffixBlueToggle { get; set; }
		public static ConfigEntry<bool> AffixWhiteToggle { get; set; }

		private static string[] AffixLunarTokens =
		{
			"Shared Design",
			"Become an aspect of perfection.",
			"All of your health is converted to shields, and you gain <style=cIsUtility>30% movement speed</style> and <style=cIsUtility>50% extra health as shields</style>. On hit, the target is <style=cDeath>crippled</style>. Every so often, you fire <style=cDeath>4</style> <style=cStack>(+2 per stack)</style> <style=cDeath>tracking projectiles</style> that deal <style=cIsDamage>30% BASE damage</style> each.",
			""
		};
		private static string[] AffixPoisonTokens =
		{
			"N'kuhana's Retort",
			"Become an aspect of corruption.",
			"On hit, <style=cIsUtility>block</style> the target's healing for <style=cIsUtility>8</style> seconds, and <style=cIsHealing>heal</style> for <style=cIsHealing>10%</style> <style=cStack>(+5% per stack)</style> <style=cIsDamage>damage dealt</style>.",
			""
		};
		private static string[] AffixHauntedTokens =
		{
			"Spectral Circlet",
			"Become an aspect of incorporeality.",
			"You gain <style=cIsUtility>25% movement speed</style>, and a <style=cIsUtility>spherical aura</style> with range <style=cIsUtility>30 meters</style> <style=cStack>(+30 meters per stack)</style>. Allies within your spherical aura gain <style=cIsDamage>25% attack speed</style> and <style=cIsUtility>50 armor</style>.",
			""
		};
		private static string[] AffixRedTokens =
		{
			"Ifrit's Distinction",
			"Become an aspect of fire.",
			"On hit, <style=cDeath>burn</style> the target for <style=cIsDamage>10% BASE damage</style>, capped at <style=cIsDamage>1% of the target's health</style> for <style=cIsUtility>4 seconds, 5 times per second</style>.",
			""
		};
		private static string[] AffixBlueTokens =
		{
			"Silence Between Two Strikes",
			"Become an aspect of lightning",
			"On hit, stick a <style=cDeath>lightning bomb</style> to the target dealing <style=cIsDamage>50%</style> <style=cStack>(+25% per stack)</style> <style=cIsDamage>TOTAL damage</style>.",
			""
		};
		private static string[] AffixWhiteTokens =
		{
			"Her Biting Embrace",
			"Become an aspect of ice.",
			"On hit, apply a <style=cIsUtility>stacking debuff</style> for <style=cIsUtility>2 seconds</style>. At <style=cIsUtility>10 stacks</style>, the target's <style=cDeath>blood freezes</style>, and they take <style=cIsDamage>2000%</style> <style=cStack>(+1000% damage per stack)</style> <style=cIsDamage>BASE damage</style>.",
			""
		};
	}
}
