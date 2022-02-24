using BepInEx;
using RoR2;
using UnityEngine;
using System.Collections.Generic;
using System;
using BepInEx.Configuration;
using System.Reflection;
using MonoMod.Cil;
using KinematicCharacterController;
using UnityEngine.Networking;
using Object = UnityEngine.Object;
using System.Linq;
using System.Collections;
using Mono.Cecil.Cil;
using System.Security;
using System.Security.Permissions;
using RoR2.CharacterAI;
using RoR2.UI;
using RoR2.Navigation;
using BetterAPI;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace Evaisa.MoreShrines
{
	//[BepInDependency(MiniRpcPlugin.Dependency)]
	[BepInDependency("com.xoxfaby.BetterAPI")]
	[BepInPlugin(ModGuid, ModName, ModVer)]
	public class MoreShrines : BaseUnityPlugin
    {
		private const string ModVer = "1.1.7.1";
		private const string ModName = "More Shrines";
		private const string ModGuid = "com.evaisa.moreshrines";

		public static MoreShrines instance;

		public static Xoroshiro128Plus EvaRng;

		public static CharacterSpawnCard impSpawnCard;

		public static Interactables.InteractableInfo impShrineInteractableInfo;
		public static Interactables.InteractableInfo fallenShrineInteractableInfo;
		public static Interactables.InteractableInfo disorderShrineInteractableInfo;
		public static Interactables.InteractableInfo heresyShrineInteractableInfo;
		public static Interactables.InteractableInfo wispShrineInteractableInfo;
		public static Interactables.InteractableInfo shieldShrineInteractableInfo;


		public static ConfigEntry<bool> impShrineEnabled;
		public static ConfigEntry<int> impShrineWeight;
		public static ConfigEntry<bool> impCountScale;
		public static ConfigEntry<int> impShrineTime;
		public static ConfigEntry<bool> itemRarityBasedOnSpeed;
		public static ConfigEntry<bool> dropItemForEveryPlayer;
		//public static ConfigEntry<bool> allowElites;
		public static ConfigEntry<int> extraItemCount;

		public static ConfigEntry<bool> fallenShrineEnabled;
		public static ConfigEntry<int> fallenShrineWeight;
		public static ConfigEntry<int> fallenShrineHPPenalty;
		public static ConfigEntry<bool> fallenShrineMoney;
		public static ConfigEntry<int> fallenShrineMoneyCost;
		//public static ConfigEntry<bool> fallenShrineSpawnAtleastOne;

		public static ConfigEntry<bool> disorderShrineEnabled;
		public static ConfigEntry<int> disorderShrineWeight;

		public static ConfigEntry<bool> heresyShrineEnabled;
		public static ConfigEntry<int> heresyShrineWeight;

		public static ConfigEntry<bool> wispShrineEnabled;
		public static ConfigEntry<bool> wispShrineScaleDifficulty;
		public static ConfigEntry<int> wispShrineWeight;

		//public static ConfigEntry<bool> shieldShrineEnabled;
		//public static ConfigEntry<int> shieldShrineWeight;


		public static CostTypeDef costTypeDefShrineFallen;
		public static CostTypeDef costTypeDefShrineDisorder;
		public static CostTypeDef costTypeDefShrineHeresy;
		public static CostTypeDef costTypeDefWispWhite;
		public static CostTypeDef costTypeDefWispGreen;
		public static CostTypeDef costTypeDefWispRed;

		public static GameObject debugPrefab;

		public static bool debugMode = false;

		public static List<Objectives.ObjectiveInfo> objectives = new List<Objectives.ObjectiveInfo>();

		public MoreShrines ()
        {
			instance = this;

			System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
			int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;

			EvaResources.Init();

			EvaRng = new Xoroshiro128Plus((ulong)cur_time);

			RegisterConfig();
			RegisterLanguageTokens();

			InitBuffs.Add();

			if (fallenShrineMoney.Value)
			{
				CreateCostDefShrineFallenAlt();
			}
			else {
				CreateCostDefShrineFallen();
			}


			CreateCostDefShrineDisorder();
			CreateCostDefShrineHeresy();
			CreateCostDefWispWhite();
			CreateCostDefWispGreen();
			CreateCostDefWispRed();

			GenerateTinyImp();

			if (fallenShrineEnabled.Value)
			{
				GenerateFallenShrine();
			}
			if (disorderShrineEnabled.Value)
			{
				GenerateDisorderShrine();
			}
			if (heresyShrineEnabled.Value)
			{
				GenerateHeresyShrine();
			}
			if (impShrineEnabled.Value)
			{
				GenerateImpShrine();
			}
			if (wispShrineEnabled.Value)
			{
				GenerateWispShrine();
			}
			/*
			if (shieldShrineEnabled.Value)
			{
				GenerateShieldShrine();
			}
			*/

			On.RoR2.Artifacts.SwarmsArtifactManager.OnSpawnCardOnSpawnedServerGlobal += SwarmsArtifactManager_OnSpawnCardOnSpawnedServerGlobal;

			//HealthBarAPI.Init();
			//setupHealthBar();
			//On.RoR2.UI.HealthBar.UpdateBarInfos += HealthBar_UpdateBarInfos;
			//IL.RoR2.UI.HealthBar.UpdateBarInfos += HealthBar_UpdateBarInfos;
		}

		/*
		public static HealthBar.BarInfo customShieldBarInfo;
		public static HealthBarStyle.BarStyle customShieldBarStyle;

		private void setupHealthBar()
        {
			customShieldBarInfo = new HealthBar.BarInfo()
			{
				normalizedXMin = 0,
				normalizedXMax = 0,
			};
			customShieldBarStyle = new HealthBarStyle.BarStyle()
			{
				enabled = true,
				baseColor = Color.yellow,
				sprite = EvaResources.ShieldBar,
				imageType = UnityEngine.UI.Image.Type.Tiled,
				sizeDelta = 0,
			};
		}
		*/
		/*
		private void HealthBar_UpdateBarInfos(ILContext il)
		{
			var c = new ILCursor(il);

			var found = c.TryGotoNext(
				x => x.MatchLdarg(0),
				x => x.MatchLdfld("RoR2.HealthComponent/HealthBarValues", "shieldFraction"),
				x => x.MatchLdloca(0)
			);

			c.Index += 3;

			if (found)
			{
				c.Emit(OpCodes.Ldarg_0);
				c.Emit(OpCodes.Ldloc, 2);
				c.EmitDelegate<Action<HealthBar, float>>((healthBar, currentBarEnd) => {

					ref HealthBar.BarInfo local3 = ref customShieldBarInfo;
					local3.enabled = true;
					ApplyStyle(ref local3, ref customShieldBarStyle);
					AddBar(ref local3, 10);

					void AddBar(ref HealthBar.BarInfo barInfo, float fraction)
					{
						if (!barInfo.enabled)
							return;
						barInfo.normalizedXMin = currentBarEnd;
						currentBarEnd = barInfo.normalizedXMax = barInfo.normalizedXMin + fraction;
					}

					void ApplyStyle(ref HealthBar.BarInfo barInfo, ref HealthBarStyle.BarStyle barStyle)
					{
						barInfo.enabled &= barStyle.enabled;
						barInfo.color = barStyle.baseColor;
						barInfo.sprite = barStyle.sprite;
						barInfo.imageType = barStyle.imageType;
						barInfo.sizeDelta = barStyle.sizeDelta;
					}
				});
			}
		}*/

		private void Stage_Start(On.RoR2.Stage.orig_Start orig, Stage self)
        {
			orig(self);
			foreach(var objective in objectives)
            {
				Objectives.RemoveObjective(objective);
            }
			objectives.Clear();
		}

        void Update()
        {
			//HealthBarAPI.Update();
		}

		public void CreateCostDefWispWhite()
		{
			costTypeDefWispWhite = new CostTypeDef();
			costTypeDefWispWhite.costStringFormatToken = "COST_ITEM_FORMAT";
			//costTypeDefShrineHeresy.saturateWorldStyledCostString = false;
			//costTypeDefShrineHeresy.darkenWorldStyledCostString = true;
			costTypeDefWispWhite.isAffordable = delegate (CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody characterBody = context.activator.GetComponent<CharacterBody>();
				if (characterBody)
				{
					var inventory = characterBody.inventory;
					if (inventory)
					{
						return inventory.GetTotalItemCountOfTier(ItemTier.Tier1) > 0;
					}
				}
				return false;
			};
			costTypeDefWispWhite.payCost = delegate (CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				CharacterBody characterBody = context.activator.GetComponent<CharacterBody>();
				if (characterBody)
				{
					var inventory = characterBody.inventory;

					var items = new List<ItemDef>();

					foreach(var index in inventory.itemAcquisitionOrder)
                    {
						if(ItemCatalog.GetItemDef(index).tier == ItemTier.Tier1)
                        {
							items.Add(ItemCatalog.GetItemDef(index));
                        }
                    }

					var item = items[Random.Range(0, items.Count)];

					context.purchasedObject.GetComponent<ShrineWispBehaviour>().wispItem = item;

					inventory.RemoveItem(item, 1);

					
				}
			};
			costTypeDefWispWhite.colorIndex = ColorCatalog.ColorIndex.Tier1Item;
			CostTypes.Add(costTypeDefWispWhite);
		}

		public void CreateCostDefWispGreen()
		{
			costTypeDefWispGreen = new CostTypeDef();
			costTypeDefWispGreen.costStringFormatToken = "COST_ITEM_FORMAT";
			costTypeDefWispGreen.saturateWorldStyledCostString = true;
			//costTypeDefShrineHeresy.darkenWorldStyledCostString = true;
			costTypeDefWispGreen.isAffordable = delegate (CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody characterBody = context.activator.GetComponent<CharacterBody>();
				if (characterBody)
				{
					var inventory = characterBody.inventory;
					if (inventory)
					{
						return inventory.GetTotalItemCountOfTier(ItemTier.Tier2) > 0;
					}
				}
				return false;
			};
			costTypeDefWispGreen.payCost = delegate (CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				CharacterBody characterBody = context.activator.GetComponent<CharacterBody>();
				if (characterBody)
				{
					var inventory = characterBody.inventory;

					var items = new List<ItemDef>();

					foreach (var index in inventory.itemAcquisitionOrder)
					{
						if (ItemCatalog.GetItemDef(index).tier == ItemTier.Tier2)
						{
							items.Add(ItemCatalog.GetItemDef(index));
						}
					}

					var item = items[Random.Range(0, items.Count)];

					context.purchasedObject.GetComponent<ShrineWispBehaviour>().wispItem = item;

					inventory.RemoveItem(item, 1);


				}
			};
			costTypeDefWispGreen.colorIndex = ColorCatalog.ColorIndex.Tier2Item;
			CostTypes.Add(costTypeDefWispGreen);
		}

		public void CreateCostDefWispRed()
		{
			costTypeDefWispRed = new CostTypeDef();
			costTypeDefWispRed.costStringFormatToken = "COST_ITEM_FORMAT";
			costTypeDefWispRed.saturateWorldStyledCostString = true;
			//costTypeDefShrineHeresy.darkenWorldStyledCostString = true;
			costTypeDefWispRed.isAffordable = delegate (CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody characterBody = context.activator.GetComponent<CharacterBody>();
				if (characterBody)
				{
					var inventory = characterBody.inventory;

					if (inventory)
					{
						return inventory.GetTotalItemCountOfTier(ItemTier.Tier3) > 0;
					}
				}
				return false;
			};
			costTypeDefWispRed.payCost = delegate (CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				CharacterBody characterBody = context.activator.GetComponent<CharacterBody>();
				if (characterBody)
				{
					var inventory = characterBody.inventory;

					var items = new List<ItemDef>();

					foreach (var index in inventory.itemAcquisitionOrder)
					{
						if (ItemCatalog.GetItemDef(index).tier == ItemTier.Tier3)
						{
							items.Add(ItemCatalog.GetItemDef(index));
						}
					}

					var item = items[Random.Range(0, items.Count)];

					context.purchasedObject.GetComponent<ShrineWispBehaviour>().wispItem = item;

					inventory.RemoveItem(item, 1);


				}
			};
			costTypeDefWispRed.colorIndex = ColorCatalog.ColorIndex.Tier3Item;
			CostTypes.Add(costTypeDefWispRed);
		}

		public void CreateCostDefShrineFallen()
        {
			costTypeDefShrineFallen = new CostTypeDef();
			costTypeDefShrineFallen.costStringFormatToken = "COST_PERCENTMAXHEALTH_ROUND_FORMAT";
			costTypeDefShrineFallen.saturateWorldStyledCostString = false;
			costTypeDefShrineFallen.darkenWorldStyledCostString = true;
			costTypeDefShrineFallen.isAffordable = delegate (CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody characterBody = context.activator.GetComponent<CharacterBody>();
				if (characterBody)
				{
					var count = characterBody.GetBuffCount(InitBuffs.maxHPDownStage);
					var added_count = (int)Mathf.Ceil(((100f - (float)characterBody.GetBuffCount(InitBuffs.maxHPDownStage)) / 100f) * (float)context.cost);
					//Debug.Log(count + added_count);
					return count + added_count < 100 && ShrineFallenBehaviour.IsAnyoneDead();
				}
				return false;
			};
			costTypeDefShrineFallen.payCost = delegate (CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				CharacterBody component = context.activator.GetComponent<CharacterBody>();
				if (component)
				{
					var count = component.GetBuffCount(InitBuffs.maxHPDownStage);
					var added_count = (int)Mathf.Ceil(((100f - (float)component.GetBuffCount(InitBuffs.maxHPDownStage)) / 100f) * (float)context.cost);
					//var added_count_revived = (int)Mathf.Ceil(((100f - (float)component.GetBuffCount(InitBuffs.maxHPDownStage)) / 100f) * (100 - (float)context.cost));

					for (var i = 0; i < added_count; i++)
                    {
						component.AddBuff(InitBuffs.maxHPDownStage);
						//component.AddTimedBuff(RoR2Content.Buffs.AffixWhite, 0.1f);
					}
				}
			};
			costTypeDefShrineFallen.colorIndex = ColorCatalog.ColorIndex.Blood;
			CostTypes.Add(costTypeDefShrineFallen);
		}


		public void CreateCostDefShrineFallenAlt()
		{
			costTypeDefShrineFallen = new CostTypeDef();
			costTypeDefShrineFallen.costStringFormatToken = "COST_MONEY_FORMAT";
			costTypeDefShrineFallen.saturateWorldStyledCostString = true;
			costTypeDefShrineFallen.darkenWorldStyledCostString = false;
			costTypeDefShrineFallen.isAffordable = delegate (CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody characterBody = context.activator.GetComponent<CharacterBody>();
				if (characterBody)
				{
					CharacterMaster master = characterBody.master;
					if (master)
					{
						return (ulong)master.money >= (ulong)((long)context.cost) && ShrineFallenBehaviour.IsAnyoneDead();
					}
				}
				return false;
			};
			costTypeDefShrineFallen.payCost = delegate (CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{

				if (context.activatorMaster)
				{
					context.activatorMaster.money -= (uint)context.cost;
				}

			};
			costTypeDefShrineFallen.colorIndex = ColorCatalog.ColorIndex.Money;
			CostTypes.Add(costTypeDefShrineFallen);
		}

		public void CreateCostDefShrineDisorder()
		{
			costTypeDefShrineDisorder = new CostTypeDef();
			costTypeDefShrineDisorder.costStringFormatToken = "COST_LUNARCOIN_FORMAT";
			costTypeDefShrineDisorder.saturateWorldStyledCostString = false;
			costTypeDefShrineDisorder.darkenWorldStyledCostString = true;
			costTypeDefShrineDisorder.isAffordable = delegate (CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody characterBody = context.activator.GetComponent<CharacterBody>();
				if (characterBody)
				{
					var allowBuy = false;
					var inventory = characterBody.inventory;


					
					foreach (ItemTier tier in Enum.GetValues(typeof(ItemTier)))
					{
						var minStack = int.MaxValue;
						var itemDefs = Utils.ItemDefsFromTier(tier);
						foreach (var itemDef in itemDefs)
						{
							var count = inventory.GetItemCount(itemDef);
							minStack = Math.Min(minStack, count);
							if (count - minStack > 1)
							{
								allowBuy = true;
							}
						}
					}


					NetworkUser networkUser = Util.LookUpBodyNetworkUser(context.activator.gameObject);
					return networkUser && (ulong)networkUser.lunarCoins >= (ulong)((long)context.cost) && allowBuy;
				}
				return false;
			};
			costTypeDefShrineDisorder.payCost = delegate (CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				NetworkUser networkUser = Util.LookUpBodyNetworkUser(context.activator.gameObject);
				if (networkUser)
				{
					networkUser.DeductLunarCoins((uint)context.cost);
				}
			};
			costTypeDefShrineDisorder.colorIndex = ColorCatalog.ColorIndex.LunarCoin;
			CostTypes.Add(costTypeDefShrineDisorder);
		}

		public void CreateCostDefShrineHeresy()
		{
			costTypeDefShrineHeresy = new CostTypeDef();
			costTypeDefShrineHeresy.costStringFormatToken = "COST_LUNARCOIN_FORMAT";
			costTypeDefShrineHeresy.saturateWorldStyledCostString = false;
			costTypeDefShrineHeresy.darkenWorldStyledCostString = true;
			costTypeDefShrineHeresy.isAffordable = delegate (CostTypeDef costTypeDef, CostTypeDef.IsAffordableContext context)
			{
				CharacterBody characterBody = context.activator.GetComponent<CharacterBody>();
				if (characterBody)
				{
					var allowBuy = false;
					var inventory = characterBody.inventory;

					if (!(characterBody.inventory.GetItemCount(RoR2Content.Items.LunarPrimaryReplacement.itemIndex) > 0 && characterBody.inventory.GetItemCount(RoR2Content.Items.LunarSecondaryReplacement.itemIndex) > 0 && characterBody.inventory.GetItemCount(RoR2Content.Items.LunarUtilityReplacement.itemIndex) > 0 && characterBody.inventory.GetItemCount(RoR2Content.Items.LunarSpecialReplacement.itemIndex) > 0))
                    {
						allowBuy = true;
                    }


					NetworkUser networkUser = Util.LookUpBodyNetworkUser(context.activator.gameObject);
					return networkUser && (ulong)networkUser.lunarCoins >= (ulong)((long)context.cost) && allowBuy;
				}
				return false;
			};
			costTypeDefShrineHeresy.payCost = delegate (CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
			{
				NetworkUser networkUser = Util.LookUpBodyNetworkUser(context.activator.gameObject);
				if (networkUser)
				{
					networkUser.DeductLunarCoins((uint)context.cost);
				}
			};
			costTypeDefShrineHeresy.colorIndex = ColorCatalog.ColorIndex.LunarCoin;
			CostTypes.Add(costTypeDefShrineHeresy);
		}

		public void RegisterLanguageTokens()
        {
			//Languages.AddTokenString("SHRINE_CHANCE_PUNISHED_MESSAGE", "<style=cShrine>{0} offered to the shrine and was punished!</style>");
			//Languages.AddTokenString("SHRINE_CHANCE_PUNISHED_MESSAGE_2P", "<style=cShrine>You offer to the shrine and are punished!</style>");
			Languages.AddTokenString("SHRINE_IMP_USE_MESSAGE", "<style=cShrine>{0} inspected the vase and tiny imps appeared!</style>");
			Languages.AddTokenString("SHRINE_IMP_USE_MESSAGE_2P", "<style=cShrine>You inspected the vase and tiny imps appeared!</style>");
			Languages.AddTokenString("SHRINE_IMP_COMPLETED", "<style=cIsHealing>You killed all the imps and found some items!</style>");
			Languages.AddTokenString("SHRINE_IMP_COMPLETED_2P", "<style=cIsHealing>{0} killed all the imps and found some items!</style>");
			Languages.AddTokenString("SHRINE_IMP_FAILED", "<style=cIsHealth>You failed to kill all the imps in time!</style>");
			Languages.AddTokenString("SHRINE_IMP_FAILED_2P", "<style=cIsHealth>{0} failed to kill all the imps in time!</style>");
			Languages.AddTokenString("SHRINE_IMP_NAME", "Shrine of Imps");
			Languages.AddTokenString("SHRINE_IMP_CONTEXT", "Inspect the vase.");
			Languages.AddTokenString("SHRINE_FALLEN_NAME", "Shrine of the Fallen");
			Languages.AddTokenString("SHRINE_FALLEN_CONTEXT", "Offer to Shrine of the Fallen");
			Languages.AddTokenString("SHRINE_FALLEN_USED", "<style=cIsHealing>{0} offered to the Shrine of the Fallen and revived {1}!</style>");
			Languages.AddTokenString("SHRINE_FALLEN_USED_2P", "<style=cIsHealing>You offer to the Shrine of the Fallen and revived {1}!</style>");
			Languages.AddTokenString("OBJECTIVE_KILL_TINY_IMPS", "Kill the <color={0}>tiny imps</color> ({1}/{2}) in {3} seconds!");
			Languages.AddTokenString("COST_PERCENTMAXHEALTH_FORMAT", "{0}% MAX HP");
			Languages.AddTokenString("COST_PERCENTMAXHEALTH_ROUND_FORMAT", "{0}% STAGE MAX HP");
			Languages.AddTokenString("SHRINE_DISORDER_NAME", "Shrine of Disorder");
			Languages.AddTokenString("SHRINE_DISORDER_CONTEXT", "Offer to Shrine of Disorder");
			Languages.AddTokenString("SHRINE_DISORDER_USE_MESSAGE_2P", "<style=cShrine>Your order has been disturbed.</style>");
			Languages.AddTokenString("SHRINE_DISORDER_USE_MESSAGE", "<style=cShrine>{0}'s order has been disturbed.</style>");
			Languages.AddTokenString("SHRINE_HERESY_NAME", "Shrine of Heresy");
			Languages.AddTokenString("SHRINE_HERESY_CONTEXT", "Offer to Shrine of Heresy");
			Languages.AddTokenString("SHRINE_HERESY_USE_MESSAGE_2P", "<style=cShrine>You have taken a step towards heresy.</style>");
			Languages.AddTokenString("SHRINE_HERESY_USE_MESSAGE", "<style=cShrine>{0} has taken a step towards heresy.</style>");
			Languages.AddTokenString("SHRINE_WISP_NAME", "Shrine of Wisps");
			Languages.AddTokenString("SHRINE_WISP_CONTEXT", "Offer to the tree");
			Languages.AddTokenString("SHRINE_WISP_ACCEPT_MESSAGE_2P", "<style=cShrine>The tree accepted your <color=#{1}>{2}</color> and ghostly Wisps appeared.</style>");
			Languages.AddTokenString("SHRINE_WISP_ACCEPT_MESSAGE", "<style=cShrine>The tree accepted {0}'s <color=#{1}>{2}</color> and ghostly Wisps appeared.</style>");
			Languages.AddTokenString("SHRINE_WISP_DENY_MESSAGE_2P", "<style=cIsDamage>The tree rejected your <color=#{1}>{2}</color> and angry Wisps appeared..</style>");
			Languages.AddTokenString("SHRINE_WISP_DENY_MESSAGE", "<style=cIsDamage>The tree rejected {0}'s <color=#{1}>{2}</color> and angry Wisps appeared..</style>");
			Languages.AddTokenString("SHRINE_SHIELDING_NAME", "Shrine of Hardening");
			Languages.AddTokenString("SHRINE_SHIELDING_CONTEXT", "Touch the shield.");
			Languages.AddTokenString("SHRINE_SHIELDING_USE_MESSAGE_2P", "<style=cShrine>You feel protected.</style>");
			Languages.AddTokenString("SHRINE_SHIELDING_USE_MESSAGE", "<style=cShrine>{0} feels protected.</style>");
		}

		public void RegisterConfig()
        {
			// Shrine of Imps
			impShrineEnabled = Config.Bind<bool>(
				"Shrine of Imps",
				"Enable",
				true,
				"Enable the Shrine of Imps."
			);
			impShrineWeight = Config.Bind<int>(
				"Shrine of Imps",
				"Weight",
				2,
				"The spawn weight of this shrine, lower is more rare."
			);
			impCountScale = Config.Bind<bool>(
				"Shrine of Imps",
				"Count Scale",
				true,
				"Scale the maximum amount of imps with stage difficulty."
			);
			impShrineTime = Config.Bind<int>(
				"Shrine of Imps",
				"Time",
				30,
				"The amount of time you get to finish a Shrine of Imps."
			);
			itemRarityBasedOnSpeed = Config.Bind<bool>(
				"Shrine of Imps",
				"Item Rarity Based On Speed",
				true,
				"Increase item rarity based on how fast you killed all the imps."
			);
			dropItemForEveryPlayer = Config.Bind<bool>(
				"Shrine of Imps",
				"Drop for every player",
				true,
				"Drop a item for every player in the session."
			);
			/*
			allowElites = Config.Bind<bool>(
				"Shrine of Imps",
				"Allow elite imps",
				true,
				"Allow the shrine to spawn imps as elites."
			);
			*/
			extraItemCount = Config.Bind<int>(
				"Shrine of Imps",
				"Extra Item Count",
				0,
				"Drop X extra items along with the base amount when a Shrine of Imps is beaten."
			);

			// Shrine of the Fallen
			fallenShrineEnabled = Config.Bind<bool>(
				"Shrine of the Fallen",
				"Enable",
				true,
				"Enable the Shrine of the Fallen."
			);
			fallenShrineWeight = Config.Bind<int>(
				"Shrine of the Fallen",
				"Weight",
				2,
				"The spawn weight of this shrine, lower is more rare."
			);
			/*
			fallenShrineSpawnAtleastOne = Config.Bind<bool>(
				"Shrine of the Fallen",
				"Always spawn",
				false,
				"Always spawn atleast one Shrine of the Fallen per stage."
			);
			*/
			fallenShrineHPPenalty = Config.Bind<int>(
				"Shrine of the Fallen",
				"HP Penalty",
				40,
				"The max HP penalty the user takes for the rest of the stage when this shrine is used."
			);
			fallenShrineMoney = Config.Bind<bool>(
				"Shrine of the Fallen",
				"Use Money",
				false,
				"Shrine of the Fallen costs money rather than a HP penalty."
			);
			fallenShrineMoneyCost = Config.Bind<int>(
				"Shrine of the Fallen",
				"Money Base Cost",
				300,
				"The base cost for the shrine. (only applicable if 'Use Money' is enabled)"
			);


			// Shrine of Disorder
			disorderShrineEnabled = Config.Bind<bool>(
				"Shrine of Disorder",
				"Enable",
				true,
				"Enable the Shrine of Disorder."
			);
			disorderShrineWeight = Config.Bind<int>(
				"Shrine of Disorder",
				"Weight",
				1,
				"The spawn weight of this shrine, lower is more rare."
			);

			// Shrine of Heresy
			heresyShrineEnabled = Config.Bind<bool>(
				"Shrine of Heresy",
				"Enable",
				true,
				"Enable the Shrine of Heresy."
			);
			heresyShrineWeight = Config.Bind<int>(
				"Shrine of Heresy",
				"Weight",
				1,
				"The spawn weight of this shrine, lower is more rare."
			);

			// Shrine of Wisps
			wispShrineEnabled = Config.Bind<bool>(
				"Shrine of Wisps",
				"Enable",
				true,
				"Enable the Shrine of Wisps."
			);
			wispShrineScaleDifficulty = Config.Bind<bool>(
				"Shrine of Wisps",
				"Scale Count With Difficulty",
				true,
				"Scale the number of wisps spawned with difficulty."
			);
			wispShrineWeight = Config.Bind<int>(
				"Shrine of Wisps",
				"Weight",
				2,
				"The spawn weight of this shrine, lower is more rare."
			);

			// Shrine of Shielding
			/*
			shieldShrineEnabled = Config.Bind<bool>(
				"Shrine of Shielding",
				"Enable",
				true,
				"Enable the Shrine of Shielding."
			);
			shieldShrineWeight = Config.Bind<int>(
				"Shrine of Shielding",
				"Weight",
				1,
				"The spawn weight of this shrine, lower is more rare."
			);
			*/
		}

        public void GenerateTinyImp()
		{

			var impCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
			var impCardOriginal = Resources.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscImp");
			impCard.directorCreditCost = 10;
			impCard.forbiddenFlags = NodeFlags.None;
			impCard.hullSize = HullClassification.Human;
			impCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
			impCard.occupyPosition = false;
			impCard.requiredFlags = NodeFlags.None;
			impCard.sendOverNetwork = true;
			impCard.forbiddenAsBoss = true;
			//impCard.noElites = !allowElites.Value;

			var impPrefab = Utils.PrefabFromGameObject(impCardOriginal.prefab);
			impPrefab.name = "TinyImpMaster";

			var impMaster = impPrefab.GetComponent<CharacterMaster>();
			var impBody = Utils.PrefabFromGameObject(impMaster.bodyPrefab);

			impBody.name = "TinyImpBody";

			impMaster.bodyPrefab = impBody;

			var impCharBody = impBody.GetComponent<CharacterBody>();

			impCharBody.baseNameToken = "IMP_TINY_BODY_NAME";

			var impModelTransform = impBody.GetComponent<ModelLocator>().modelTransform;

			impModelTransform.localScale = impModelTransform.localScale / 2f;

			var skillDrivers = impPrefab.GetComponents<AISkillDriver>();

			impCharBody.baseMaxHealth = impCharBody.baseMaxHealth / 2;

			impCharBody.levelMaxHealth = impCharBody.levelMaxHealth / 2;

			impCharBody.baseJumpPower = impCharBody.baseJumpPower / 5;

			impCharBody.levelJumpPower = 0;

			impCharBody.baseMoveSpeed = impCharBody.baseMoveSpeed * 1.5f;

			foreach (var oldDriver in skillDrivers)
			{
				Object.Destroy(oldDriver);
			}

			var walkDriver = impPrefab.AddComponent<AISkillDriver>();
			walkDriver.minDistance = 0;
			walkDriver.maxDistance = 500;
			walkDriver.aimType = AISkillDriver.AimType.MoveDirection;
			walkDriver.ignoreNodeGraph = false;
			walkDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
			walkDriver.shouldSprint = true;
			walkDriver.movementType = AISkillDriver.MovementType.FleeMoveTarget;
			walkDriver.moveInputScale = 1.0f;
			walkDriver.driverUpdateTimerOverride = -1;
			walkDriver.skillSlot = SkillSlot.None;

			impPrefab.AddComponent<TinyImp>();

			impPrefab.GetComponent<BaseAI>().localNavigator.allowWalkOffCliff = false;
			MasterPrefabs.Add(impPrefab);
			BodyPrefabs.Add(impBody);

			On.RoR2.LocalNavigator.Update += LocalNavigator_Update;

			impCard.prefab = impPrefab;
			impSpawnCard = impCard;
		}

		private void LocalNavigator_Update(On.RoR2.LocalNavigator.orig_Update orig, LocalNavigator self, float deltaTime)
		{

			if (self.bodyComponents.body)
			{
				if (self.bodyComponents.body.master)
				{
					if (self.bodyComponents.body.master.gameObject)
					{
						if (self.bodyComponents.body.master.gameObject.GetComponent<TinyImp>())
						{
							self.allowWalkOffCliff = false;
						}

						//print("lookAheadDistance: " + self.lookAheadDistance);
					}
				}
			}
			orig(self, deltaTime);
		}


		private void SwarmsArtifactManager_OnSpawnCardOnSpawnedServerGlobal(On.RoR2.Artifacts.SwarmsArtifactManager.orig_OnSpawnCardOnSpawnedServerGlobal orig, SpawnCard.SpawnResult result)
		{
			var allow_handle = true;

			if (result.spawnedInstance)
			{
				if (result.spawnedInstance.GetComponent<TinyImp>())
				{
					allow_handle = false;
				}
			}

			if (allow_handle)
            {
				orig(result);
			}
		}
		/*
		private void CharacterMaster_OnBodyStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
		{
			orig(self, body);
			if (body.master)
			{
				if (BuffCatalog.FindBuffIndex("Immune") != BuffIndex.None){
					var masterObject = body.masterObject;
					if (masterObject.GetComponent<TinyImp>())
					{
						body.AddTimedBuff(BuffCatalog.FindBuffIndex("Immune"), 2);
					}
				}
			}
		}
		*/

		private WeightedSelection<DirectorCard> SceneDirector_GenerateInteractableCardSelection(On.RoR2.SceneDirector.orig_GenerateInteractableCardSelection orig, SceneDirector self)
		{
			WeightedSelection<DirectorCard> selection = orig(self);
			foreach (var choice in selection.choices)
			{
				Print("Card Name: " + choice.value?.spawnCard.name);
				//Print(choice.value.spawnCard.prefab.name);
				Print("Name: " + choice.value?.spawnCard?.prefab?.name);
				Print("Weight: " + choice.weight);
			};
			return selection;
		}

		public static void Print(string printString)
        {
			Debug.Log("[Better Shrines] "+printString);
        }


		public void GenerateFallenShrine()
		{

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineHealing");

			/*
			var oldPrefab = ChanceCard.prefab;
			var oldSymbol = oldPrefab.transform.Find("Symbol");
			var oldSymbolRenderer = oldSymbol.GetComponent<MeshRenderer>();
			var oldSymbolMaterial = oldSymbolRenderer.material;



			var shrinePrefab = (GameObject)Evaisa.MoreShrines.EvaResources.ShrineFallenPrefab;
			var mdlBase = shrinePrefab.transform.Find("Base").Find("mdlShrineFallen");

			mdlBase.GetComponent<MeshRenderer>().material.shader = Shader.Find("Hopoo Games/Deferred/Standard");
			mdlBase.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.8549f, 0.7647f, 1.0f);

			var symbolTransform = shrinePrefab.transform.Find("Symbol");
			*/
			var info = new ShrineAPI.ShrineInfo(ShrineAPI.ShrineBaseType.Order, new Color(1.0f, 0.8549f, 0.7647f, 1.0f), (GameObject)Evaisa.MoreShrines.EvaResources.ShrineFallenPrefab);

			var symbolTransform = info.shrinePrefab.transform.Find("Symbol");

			var mdlBase = info.modelTransform;

			var shrinePrefab = info.shrinePrefab;

			var purchaseInteraction = shrinePrefab.GetComponent<PurchaseInteraction>();

			var cost = fallenShrineHPPenalty.Value;

			if(cost > 99)
            {
				cost = 99;
            }else if(cost < 0)
            {
				cost = 0; 
            }

            if (fallenShrineMoney.Value)
            {
				purchaseInteraction.Networkcost = fallenShrineMoneyCost.Value;
				purchaseInteraction.cost = fallenShrineMoneyCost.Value;
			}
            else
            {
				purchaseInteraction.Networkcost = cost;
				purchaseInteraction.cost = cost;
			}



			purchaseInteraction.setUnavailableOnTeleporterActivated = false;

			if (fallenShrineMoney.Value)
			{
				purchaseInteraction.automaticallyScaleCostWithDifficulty = true;
			}
            else
            {
				purchaseInteraction.automaticallyScaleCostWithDifficulty = false;
			}
			

			//purchaseInteraction.cost;
			//

			/*
			var symbolMesh = symbolTransform.gameObject.GetComponent<MeshRenderer>();

			var texture = symbolMesh.material.mainTexture;

			symbolMesh.material = new Material(Shader.Find("Hopoo Games/FX/Cloud Remap"));

			symbolMesh.material.CopyPropertiesFromMaterial(oldSymbolMaterial);
			symbolMesh.material.mainTexture = texture;
			*/
			var fallenBehaviour = shrinePrefab.AddComponent<ShrineFallenBehaviour>();
			fallenBehaviour.shrineEffectColor = new Color(0.384f, 0.874f, 0.435f);
			fallenBehaviour.symbolTransform = symbolTransform;
			fallenBehaviour.maxUses = 1;
			//fallenBehaviour.scalePerUse = true;

			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = false;
			interactable.selectionWeight = fallenShrineWeight.Value;
			interactable.interactableCategory = Interactables.Category.Shrines;
			interactable.multiplayerOnly = true;
			/*
			if (fallenShrineSpawnAtleastOne.Value)
			{
				interactable.minimumCount = 1;
            }
            else
            {
				interactable.minimumCount = 0;
			}
			*/

			fallenShrineInteractableInfo = Interactables.AddToStages(interactable, Interactables.Stages.Default);

		}

		public void GenerateHeresyShrine()
		{

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineRestack");

			/*
			var oldPrefab = ChanceCard.prefab;
			var oldSymbol = oldPrefab.transform.Find("Symbol");
			var oldSymbolRenderer = oldSymbol.GetComponent<MeshRenderer>();
			var oldSymbolMaterial = oldSymbolRenderer.material;



			var shrinePrefab = (GameObject)Evaisa.MoreShrines.EvaResources.ShrineHeresyPrefab;

			//Debug.Log(shrinePrefab);

			var mdlBase = shrinePrefab.transform.Find("Base").Find("mdlShrineHeresy");
			//mdlBase.GetComponent<MeshRenderer>().material.mainTexture = Resources.Load<Texture2D>("Assets/Texture2D/texShrineBloodDiffuse.png");

			mdlBase.GetComponent<MeshRenderer>().material.shader = Shader.Find("Hopoo Games/Deferred/Standard");
			mdlBase.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.8549f, 0.7647f, 1.0f);

			var symbolTransform = shrinePrefab.transform.Find("Symbol");


			var symbolMesh = symbolTransform.gameObject.GetComponent<MeshRenderer>();

			var texture = symbolMesh.material.mainTexture;

			symbolMesh.material = new Material(Shader.Find("Hopoo Games/FX/Cloud Remap"));

			symbolMesh.material.CopyPropertiesFromMaterial(oldSymbolMaterial);
			symbolMesh.material.mainTexture = texture;*/

			var info = new ShrineAPI.ShrineInfo(ShrineAPI.ShrineBaseType.Order, new Color(1.0f, 0.8549f, 0.7647f, 1.0f), (GameObject)Evaisa.MoreShrines.EvaResources.ShrineHeresyPrefab);

			var symbolTransform = info.shrinePrefab.transform.Find("Symbol");

			var mdlBase = info.modelTransform;

			var shrinePrefab = info.shrinePrefab;

			var shrineBehaviour = shrinePrefab.AddComponent<ShrineHeresyBehaviour>();
			shrineBehaviour.shrineEffectColor = new Color(1f, 0.23f, 0.6337214f);
			shrineBehaviour.symbolTransform = symbolTransform;


			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = false;
			interactable.selectionWeight = heresyShrineWeight.Value;
			interactable.interactableCategory = Interactables.Category.Shrines;


			heresyShrineInteractableInfo = Interactables.AddToStages(interactable, Interactables.Stages.Default);

		}

		public void GenerateDisorderShrine()
		{

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineRestack");

			var info = new ShrineAPI.ShrineInfo(ShrineAPI.ShrineBaseType.Order, new Color(1.0f, 0.8549f, 0.7647f, 1.0f), (GameObject)Evaisa.MoreShrines.EvaResources.ShrineDisorderPrefab);

			var symbolTransform = info.shrinePrefab.transform.Find("Symbol");

			var mdlBase = info.modelTransform;

			var shrinePrefab = info.shrinePrefab;

			var shrineBehaviour = shrinePrefab.AddComponent<ShrineDisorderBehaviour>();
			shrineBehaviour.shrineEffectColor = new Color(1f, 0.23f, 0.6337214f);
			shrineBehaviour.symbolTransform = symbolTransform;
			shrineBehaviour.modelBase = mdlBase.transform;
			

			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = false;
			interactable.selectionWeight = disorderShrineWeight.Value;
			interactable.interactableCategory = Interactables.Category.Shrines;


			disorderShrineInteractableInfo = Interactables.AddToStages(interactable, Interactables.Stages.Default);

		}

		/*
		public void GenerateShieldShrine()
		{

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineChance");

			var info = new ShrineAPI.ShrineInfo(ShrineAPI.ShrineBaseType.Order, new Color(1.0f, 0.8549f, 0.7647f, 1.0f), (GameObject)Evaisa.MoreShrines.EvaResources.ShrineShieldPrefab);

			var symbolTransform = info.shrinePrefab.transform.Find("Symbol");

			var mdlBase = info.modelTransform;

			var shrinePrefab = info.shrinePrefab;

			var shrineBehaviour = shrinePrefab.AddComponent<ShrineShieldingBehaviour>();
			shrineBehaviour.shrineEffectColor = new Color(1f, 0.23f, 0.6337214f);
			shrineBehaviour.symbolTransform = symbolTransform;


			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = false;
			interactable.selectionWeight = shieldShrineWeight.Value;
			interactable.interactableCategory = Interactables.Category.Shrines;


			shieldShrineInteractableInfo = Interactables.AddToStages(interactable, Interactables.Stages.Default);

		}*/

		public void GenerateImpShrine()
		{

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineHealing");

			//var oldPrefab = ChanceCard.prefab;

			var info = new ShrineAPI.ShrineInfo(ShrineAPI.ShrineBaseType.Healing, new Color(1.0f, 0.8549f, 0.7647f, 1.0f), (GameObject)Evaisa.MoreShrines.EvaResources.ShrineImpPrefab);

			var symbolTransform = info.shrinePrefab.transform.Find("Symbol");

			var shrinePrefab = info.shrinePrefab;

			/*
			var oldSymbol = oldPrefab.transform.Find("Symbol");
			var oldSymbolRenderer = oldSymbol.GetComponent<MeshRenderer>();
			var oldSymbolMaterial = oldSymbolRenderer.material;



			var shrinePrefab = (GameObject)Evaisa.MoreShrines.EvaResources.ShrineImpPrefab;
			var mdlBase = shrinePrefab.transform.Find("Base").Find("mdlShrineImp");

			mdlBase.GetComponent<MeshRenderer>().material.shader = Shader.Find("Hopoo Games/Deferred/Standard");
			mdlBase.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.8549f, 0.7647f, 1.0f);

			var symbolTransform = shrinePrefab.transform.Find("Symbol");

			var symbolMesh = symbolTransform.gameObject.GetComponent<MeshRenderer>();

			var texture = symbolMesh.material.mainTexture;

			symbolMesh.material = new Material(Shader.Find("Hopoo Games/FX/Cloud Remap"));

			symbolMesh.material.CopyPropertiesFromMaterial(oldSymbolMaterial);
			symbolMesh.material.mainTexture = texture;*/


			var directorCard = new DirectorCard();
			directorCard.spawnCard = impSpawnCard;
			directorCard.selectionWeight = 10;
			directorCard.spawnDistance = DirectorCore.MonsterSpawnDistance.Standard;
			directorCard.allowAmbushSpawn = true;
			directorCard.preventOverhead = false;
			directorCard.minimumStageCompletions = 0;

			var combatDirector = shrinePrefab.GetComponent<CombatDirector>();
			var cardSelection = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
			cardSelection.AddCategory("Imps", 10);
			cardSelection.AddCard(0, directorCard);

			combatDirector.monsterCards = cardSelection;


			var impBehaviour = shrinePrefab.AddComponent<ShrineImpBehaviour>();
			impBehaviour.shrineEffectColor = new Color(0.6661001f, 0.5333304f, 0.8018868f);
			impBehaviour.symbolTransform = symbolTransform;
			impBehaviour.directorCard = directorCard;

			var customDirector = shrinePrefab.AddComponent<CustomDirector>();

			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = false;
			interactable.selectionWeight = impShrineWeight.Value;
			interactable.interactableCategory = Interactables.Category.Shrines;


			impShrineInteractableInfo = Interactables.AddToStages(interactable, Interactables.Stages.Default);

		}

		public void GenerateWispShrine()
		{

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineBlood");

			var oldPrefab = ChanceCard.prefab;

			var info = new ShrineAPI.ShrineInfo(ShrineAPI.ShrineBaseType.Blood, new Color(1.0f, 0.8549f, 0.7647f, 1.0f), (GameObject)Evaisa.MoreShrines.EvaResources.ShrineWispPrefab);

			var symbolTransform = info.shrinePrefab.transform.Find("Symbol");

			var mdlBase = info.modelTransform;

			var shrinePrefab = info.shrinePrefab;

			var directorCard1 = new DirectorCard();
			directorCard1.spawnCard = Resources.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscLesserWisp");
			directorCard1.selectionWeight = 10;
			directorCard1.spawnDistance = DirectorCore.MonsterSpawnDistance.Standard;
			directorCard1.allowAmbushSpawn = true;
			directorCard1.preventOverhead = false;
			directorCard1.minimumStageCompletions = 0;

			var directorCard2 = new DirectorCard();
			directorCard2.spawnCard = Resources.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscGreaterWisp");
			directorCard2.selectionWeight = 3;
			directorCard2.spawnDistance = DirectorCore.MonsterSpawnDistance.Standard;
			directorCard2.allowAmbushSpawn = true;
			directorCard2.preventOverhead = false;
			directorCard2.minimumStageCompletions = 0;


			var combatDirector = shrinePrefab.GetComponent<CombatDirector>();
			var cardSelection = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
			cardSelection.AddCategory("Imps", 13);
			cardSelection.AddCard(0, directorCard1);
			cardSelection.AddCard(0, directorCard2);

			combatDirector.monsterCards = cardSelection;

			var wispShrineBehaviour = shrinePrefab.AddComponent<ShrineWispBehaviour>();
			wispShrineBehaviour.shrineEffectColor = new Color(0.6661001f, 0.5333304f, 0.8018868f);
			wispShrineBehaviour.symbolTransform = symbolTransform;

			/*
			var impBehaviour = shrinePrefab.AddComponent<ShrineImpBehaviour>();
			impBehaviour.shrineEffectColor = new Color(0.6661001f, 0.5333304f, 0.8018868f);
			impBehaviour.symbolTransform = symbolTransform;
			impBehaviour.directorCard = directorCard;
			*/

			//var customDirector = shrinePrefab.AddComponent<CustomDirector>();


			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = false;
			interactable.selectionWeight = wispShrineWeight.Value;
			interactable.interactableCategory = Interactables.Category.Shrines;


			wispShrineInteractableInfo = Interactables.AddToStages(interactable, Interactables.Stages.ScorchedAcres | Interactables.Stages.SirensCall);

		}
	}
}
