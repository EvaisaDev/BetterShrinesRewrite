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

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace Evaisa.MoreShrines
{
	//[BepInDependency(MiniRpcPlugin.Dependency)]
	[BepInDependency("com.xoxfaby.BetterAPI")]
	[BepInPlugin(ModGuid, ModName, ModVer)]
	public class MoreShrines : BaseUnityPlugin
    {
		private const string ModVer = "1.0.0";
		private const string ModName = "BetterShrines";
		private const string ModGuid = "com.Evaisa.MoreShrines";

		public static MoreShrines instance;

		public static Xoroshiro128Plus EvaRng;

		public static CharacterSpawnCard impSpawnCard;

		public static Interactables.InteractableInfo impShrineInteractableInfo;
		public static Interactables.InteractableInfo fallenShrineInteractableInfo;
		public static Interactables.InteractableInfo disorderShrineInteractableInfo;
		public static Interactables.InteractableInfo heresyShrineInteractableInfo;


		public static ConfigEntry<bool> impCountScale;
		public static ConfigEntry<int> impShrineTime;
		public static ConfigEntry<bool> itemRarityBasedOnSpeed;
		public static ConfigEntry<int> extraItemCount;

		public static CostTypeDef costTypeDefShrineFallen;
		public static CostTypeDef costTypeDefShrineDisorder;
		public static CostTypeDef costTypeDefShrineHeresy;

		public static bool debugMode = true;

		public MoreShrines ()
        {
			instance = this;

			System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
			int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;

			EvaResources.Init();

			EvaRng = new Xoroshiro128Plus((ulong)cur_time);

			InitBuffs.Add();

			CreateCostDefShrineFallen();
			CreateCostDefShrineDisorder();
			CreateCostDefShrineHeresy();

			RegisterConfig();
			RegisterLanguageTokens();

			GenerateTinyImp();

			GenerateFallenShrine();
			GenerateDisorderShrine();
			GenerateHeresyShrine();
			GenerateImpShrine();

			On.RoR2.Artifacts.SwarmsArtifactManager.OnSpawnCardOnSpawnedServerGlobal += SwarmsArtifactManager_OnSpawnCardOnSpawnedServerGlobal;

			if (debugMode)
			{
				On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };


				On.RoR2.CombatDirector.AttemptSpawnOnTarget += (orig, self, spawnTarget, placementMode) =>
				{
					if (self.transform.name == "Director")
					{
						return false;
					}
					else
					{
						return orig(self, spawnTarget, placementMode);
					}
				};
			
				On.RoR2.SceneDirector.GenerateInteractableCardSelection += SceneDirector_GenerateInteractableCardSelection;
			}
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
					}
				}
			};
			costTypeDefShrineFallen.colorIndex = ColorCatalog.ColorIndex.Blood;
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
			//Languages.AddTokenString("SHRINE_CHANCE_PUNISHED_MESSAGE", "<style=cShrine>{0} offered to the shrine and was punished!</color>");
			//Languages.AddTokenString("SHRINE_CHANCE_PUNISHED_MESSAGE_2P", "<style=cShrine>You offer to the shrine and are punished!</color>");
			Languages.AddTokenString("SHRINE_IMP_USE_MESSAGE", "<style=cShrine>{0} inspected the vase and tiny imps appeared!</color>");
			Languages.AddTokenString("SHRINE_IMP_USE_MESSAGE_2P", "<style=cShrine>You inspected the vase and tiny imps appeared!</color>");
			Languages.AddTokenString("SHRINE_IMP_COMPLETED", "<style=cIsHealing>You killed all the imps and found some items!</color>");
			Languages.AddTokenString("SHRINE_IMP_COMPLETED_2P", "<style=cIsHealing>{0} killed all the imps and found some items!</color>");
			Languages.AddTokenString("SHRINE_IMP_FAILED", "<style=cIsHealth>You failed to kill all the imps in time!</color>");
			Languages.AddTokenString("SHRINE_IMP_FAILED_2P", "<style=cIsHealth>{0} failed to kill all the imps in time!</color>");
			Languages.AddTokenString("SHRINE_IMP_NAME", "Shrine of Imps");
			Languages.AddTokenString("SHRINE_IMP_CONTEXT", "Inspect the vase.");
			Languages.AddTokenString("SHRINE_FALLEN_NAME", "Shrine of the Fallen");
			Languages.AddTokenString("SHRINE_FALLEN_CONTEXT", "Offer to Shrine of the Fallen");
			Languages.AddTokenString("SHRINE_FALLEN_USED", "<style=cIsHealing>{0} offered to the Shrine of the Fallen and revived {1}!</color>");
			Languages.AddTokenString("SHRINE_FALLEN_USED_2P", "<style=cIsHealing>You offer to the Shrine of the Fallen and revived {1}!</color>");
			Languages.AddTokenString("OBJECTIVE_KILL_TINY_IMPS", "Kill the <color={0}>tiny imps</color> ({1}/{2}) in {3} seconds!");
			Languages.AddTokenString("COST_PERCENTMAXHEALTH_FORMAT", "{0}% MAX HP");
			Languages.AddTokenString("COST_PERCENTMAXHEALTH_ROUND_FORMAT", "{0}% STAGE MAX HP");
			Languages.AddTokenString("SHRINE_DISORDER_NAME", "Shrine of Disorder");
			Languages.AddTokenString("SHRINE_DISORDER_CONTEXT", "Offer to Shrine of Disorder");
			Languages.AddTokenString("SHRINE_DISORDER_USE_MESSAGE_2P", "<style=cShrine>Your items have been disorganized.</color>");
			Languages.AddTokenString("SHRINE_DISORDER_USE_MESSAGE", "<style=cShrine>{0}'s items have been disorganized.</color>");
			Languages.AddTokenString("SHRINE_HERESY_NAME", "Shrine of Heresy");
			Languages.AddTokenString("SHRINE_HERESY_CONTEXT", "Offer to Shrine of Heresy");
			Languages.AddTokenString("SHRINE_HERESY_USE_MESSAGE_2P", "<style=cShrine>You have taken a step towards heresy.</color>");
			Languages.AddTokenString("SHRINE_HERESY_USE_MESSAGE", "<style=cShrine>{0} has taken a step towards heresy.</color>");
		}

		public void RegisterConfig()
        {
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
			extraItemCount = Config.Bind<int>(
				"Shrine of Imps",
				"Extra Item Count",
				0,
				"Drop X extra items along with the base amount when a Shrine of Imps is beaten."
			);
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
			impCard.noElites = false;

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

		public void Update()
		{
            if (debugMode)
            {

				if (PlayerCharacterMasterController.instances.Count > 0)
				{
					PlayerCharacterMasterController localPlayer = PlayerCharacterMasterController.instances[0];
					if (Input.GetKeyDown(KeyCode.F1))
					{


						if (localPlayer.hasEffectiveAuthority && localPlayer.bodyInputs && localPlayer.body)
						{
							CharacterBody localBody = localPlayer.body;
							InputBankTest localInputs = localPlayer.bodyInputs;

							Ray myRay = new Ray(localInputs.aimOrigin, localInputs.aimDirection);

							float maxDistance = 1000f;
							RaycastHit raycastHit;

							if (Util.CharacterRaycast(localBody.gameObject, myRay, out raycastHit, maxDistance, LayerIndex.world.mask | LayerIndex.defaultLayer.mask | LayerIndex.pickups.mask, QueryTriggerInteraction.Collide))
							{

								Vector3 hitPos = raycastHit.point;


								SpawnCard chestCard = impShrineInteractableInfo.directorCard.spawnCard;
								DirectorPlacementRule placementRule = new DirectorPlacementRule();
								placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
								GameObject chest = chestCard.DoSpawn(hitPos, Quaternion.Euler(new Vector3(0f, 0f, 0f)), new DirectorSpawnRequest(chestCard, placementRule, Run.instance.runRNG)).spawnedInstance;
								chest.transform.eulerAngles = new Vector3(0, 0, 0);
							}
						}
					}
					else if (Input.GetKeyDown(KeyCode.F2))
					{


						if (localPlayer.hasEffectiveAuthority && localPlayer.bodyInputs && localPlayer.body)
						{
							CharacterBody localBody = localPlayer.body;
							InputBankTest localInputs = localPlayer.bodyInputs;

							Ray myRay = new Ray(localInputs.aimOrigin, localInputs.aimDirection);

							float maxDistance = 1000f;
							RaycastHit raycastHit;

							if (Util.CharacterRaycast(localBody.gameObject, myRay, out raycastHit, maxDistance, LayerIndex.world.mask | LayerIndex.defaultLayer.mask | LayerIndex.pickups.mask, QueryTriggerInteraction.Collide))
							{

								Vector3 hitPos = raycastHit.point;


								SpawnCard chestCard = fallenShrineInteractableInfo.directorCard.spawnCard;
								DirectorPlacementRule placementRule = new DirectorPlacementRule();
								placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
								GameObject chest = chestCard.DoSpawn(hitPos, Quaternion.Euler(new Vector3(0f, 0f, 0f)), new DirectorSpawnRequest(chestCard, placementRule, Run.instance.runRNG)).spawnedInstance;
								chest.transform.eulerAngles = new Vector3(0, 0, 0);
							}
						}
					}
					else if (Input.GetKeyDown(KeyCode.F3))
					{


						if (localPlayer.hasEffectiveAuthority && localPlayer.bodyInputs && localPlayer.body)
						{
							CharacterBody localBody = localPlayer.body;
							InputBankTest localInputs = localPlayer.bodyInputs;

							Ray myRay = new Ray(localInputs.aimOrigin, localInputs.aimDirection);

							float maxDistance = 1000f;
							RaycastHit raycastHit;

							if (Util.CharacterRaycast(localBody.gameObject, myRay, out raycastHit, maxDistance, LayerIndex.world.mask | LayerIndex.defaultLayer.mask | LayerIndex.pickups.mask, QueryTriggerInteraction.Collide))
							{

								Vector3 hitPos = raycastHit.point;


								SpawnCard chestCard = Resources.Load<SpawnCard>("spawncards/interactablespawncard/iscScrapper");
								DirectorPlacementRule placementRule = new DirectorPlacementRule();
								placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
								GameObject chest = chestCard.DoSpawn(hitPos, Quaternion.Euler(new Vector3(0f, 0f, 0f)), new DirectorSpawnRequest(chestCard, placementRule, Run.instance.runRNG)).spawnedInstance;
								chest.transform.eulerAngles = new Vector3(0, 0, 0);
							}
						}
					}
					else if (Input.GetKeyDown(KeyCode.F4))
					{


						if (localPlayer.hasEffectiveAuthority && localPlayer.bodyInputs && localPlayer.body)
						{
							CharacterBody localBody = localPlayer.body;
							InputBankTest localInputs = localPlayer.bodyInputs;

							Ray myRay = new Ray(localInputs.aimOrigin, localInputs.aimDirection);

							float maxDistance = 1000f;
							RaycastHit raycastHit;

							if (Util.CharacterRaycast(localBody.gameObject, myRay, out raycastHit, maxDistance, LayerIndex.world.mask | LayerIndex.defaultLayer.mask | LayerIndex.pickups.mask, QueryTriggerInteraction.Collide))
							{

								Vector3 hitPos = raycastHit.point;


								SpawnCard chestCard = Resources.Load<SpawnCard>("spawncards/interactablespawncard/iscChest1");
								DirectorPlacementRule placementRule = new DirectorPlacementRule();
								placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
								GameObject chest = chestCard.DoSpawn(hitPos, Quaternion.Euler(new Vector3(0f, 0f, 0f)), new DirectorSpawnRequest(chestCard, placementRule, Run.instance.runRNG)).spawnedInstance;
								chest.transform.eulerAngles = new Vector3(0, 0, 0);
							}
						}
					}
					else if (Input.GetKeyDown(KeyCode.F5))
					{

						if (localPlayer.hasEffectiveAuthority && localPlayer.bodyInputs && localPlayer.body)
						{
							CharacterBody localBody = localPlayer.body;

							localPlayer.GetComponent<CharacterMaster>().GiveMoney(1000);
						}
					}
					else if (Input.GetKeyDown(KeyCode.F6))
					{
						if (localPlayer.hasEffectiveAuthority && localPlayer.bodyInputs && localPlayer.body)
						{
							CharacterBody localBody = localPlayer.body;
							localBody.master.playerCharacterMasterController.networkUser.AwardLunarCoins(1);

						}
					}
					else if (Input.GetKeyDown(KeyCode.F7))
					{
						if (localPlayer.hasEffectiveAuthority && localPlayer.bodyInputs && localPlayer.body)
						{
							CharacterBody localBody = localPlayer.body;
							InputBankTest localInputs = localPlayer.bodyInputs;

							Ray myRay = new Ray(localInputs.aimOrigin, localInputs.aimDirection);

							float maxDistance = 1000f;
							RaycastHit raycastHit;

							if (Util.CharacterRaycast(localBody.gameObject, myRay, out raycastHit, maxDistance, LayerIndex.world.mask | LayerIndex.defaultLayer.mask | LayerIndex.pickups.mask, QueryTriggerInteraction.Collide))
							{

								Vector3 hitPos = raycastHit.point;


								SpawnCard chestCard = Resources.Load<SpawnCard>("spawncards/interactablespawncard/iscShrineRestack");
								DirectorPlacementRule placementRule = new DirectorPlacementRule();
								placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
								GameObject chest = chestCard.DoSpawn(hitPos, Quaternion.Euler(new Vector3(0f, UnityEngine.Random.Range(0, 360), 0f)), new DirectorSpawnRequest(chestCard, placementRule, Run.instance.runRNG)).spawnedInstance;
								//chest.transform.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0, 360), 0);
							}
						}
					}
					else if (Input.GetKeyDown(KeyCode.F8))
					{
						if (localPlayer.hasEffectiveAuthority && localPlayer.bodyInputs && localPlayer.body)
						{
							CharacterBody localBody = localPlayer.body;
							InputBankTest localInputs = localPlayer.bodyInputs;

							Ray myRay = new Ray(localInputs.aimOrigin, localInputs.aimDirection);

							float maxDistance = 1000f;
							RaycastHit raycastHit;

							if (Util.CharacterRaycast(localBody.gameObject, myRay, out raycastHit, maxDistance, LayerIndex.world.mask | LayerIndex.defaultLayer.mask | LayerIndex.pickups.mask, QueryTriggerInteraction.Collide))
							{

								Vector3 hitPos = raycastHit.point;


								SpawnCard chestCard = disorderShrineInteractableInfo.directorCard.spawnCard;
								DirectorPlacementRule placementRule = new DirectorPlacementRule();
								placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
								GameObject chest = chestCard.DoSpawn(hitPos, Quaternion.Euler(new Vector3(0f, UnityEngine.Random.Range(0, 360), 0f)), new DirectorSpawnRequest(chestCard, placementRule, Run.instance.runRNG)).spawnedInstance;
								//chest.transform.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0, 360), 0);
							}
						}
					}
					else if (Input.GetKeyDown(KeyCode.F9))
					{
						if (localPlayer.hasEffectiveAuthority && localPlayer.bodyInputs && localPlayer.body)
						{
							CharacterBody localBody = localPlayer.body;
							InputBankTest localInputs = localPlayer.bodyInputs;

							Ray myRay = new Ray(localInputs.aimOrigin, localInputs.aimDirection);

							float maxDistance = 1000f;
							RaycastHit raycastHit;

							if (Util.CharacterRaycast(localBody.gameObject, myRay, out raycastHit, maxDistance, LayerIndex.world.mask | LayerIndex.defaultLayer.mask | LayerIndex.pickups.mask, QueryTriggerInteraction.Collide))
							{

								Vector3 hitPos = raycastHit.point;


								SpawnCard chestCard = heresyShrineInteractableInfo.directorCard.spawnCard;
								DirectorPlacementRule placementRule = new DirectorPlacementRule();
								placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
								GameObject chest = chestCard.DoSpawn(hitPos, Quaternion.Euler(new Vector3(0f, UnityEngine.Random.Range(0, 360), 0f)), new DirectorSpawnRequest(chestCard, placementRule, Run.instance.runRNG)).spawnedInstance;
								//chest.transform.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0, 360), 0);
							}
						}
					}
					else if (Input.GetKeyDown(KeyCode.F10))
					{
						if (localPlayer.hasEffectiveAuthority && localPlayer.bodyInputs && localPlayer.body)
						{
							CharacterBody localBody = localPlayer.body;
							localBody.master.TrueKill();

						}
					}
				}
			}
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
			if (result.spawnRequest.spawnCard as CharacterSpawnCard)
			{
				if (result.spawnedInstance.gameObject.GetComponent<CharacterMaster>())
				{
					if (!result.spawnedInstance.gameObject.GetComponent<TinyImp>())
					{
						orig(result);
					}
				}
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

        private bool PurchaseInteraction_CanBeAffordedByInteractor(On.RoR2.PurchaseInteraction.orig_CanBeAffordedByInteractor orig, PurchaseInteraction self, Interactor activator)
        {
            if (self.gameObject.GetComponent<ShrineFallenBehaviour>())
            {
				if(self.gameObject.GetComponent<ShrineFallenBehaviour>().isAvailable == false)
                {
					return false;
                }
            }
			return orig(self, activator);
        }

		public void GenerateFallenShrine()
		{

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineHealing");

			var oldPrefab = ChanceCard.prefab;
			var oldSymbol = oldPrefab.transform.Find("Symbol");
			var oldSymbolRenderer = oldSymbol.GetComponent<MeshRenderer>();
			var oldSymbolMaterial = oldSymbolRenderer.material;



			var shrinePrefab = (GameObject)Evaisa.MoreShrines.EvaResources.ShrineFallenPrefab;
			var mdlBase = shrinePrefab.transform.Find("Base").Find("mdlShrineFallen");

			mdlBase.GetComponent<MeshRenderer>().material.shader = Shader.Find("Hopoo Games/Deferred/Standard");
			mdlBase.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.8549f, 0.7647f, 1.0f);

			var symbolTransform = shrinePrefab.transform.Find("Symbol");

			var purchaseInteraction = shrinePrefab.GetComponent<PurchaseInteraction>();
			purchaseInteraction.Networkcost = 40;
			purchaseInteraction.cost = 40;
			purchaseInteraction.setUnavailableOnTeleporterActivated = false;
			
			purchaseInteraction.automaticallyScaleCostWithDifficulty = false;

			//purchaseInteraction.cost;
			//

			var symbolMesh = symbolTransform.gameObject.GetComponent<MeshRenderer>();

			var texture = symbolMesh.material.mainTexture;

			symbolMesh.material = new Material(Shader.Find("Hopoo Games/FX/Cloud Remap"));

			symbolMesh.material.CopyPropertiesFromMaterial(oldSymbolMaterial);
			symbolMesh.material.mainTexture = texture;

			var fallenBehaviour = shrinePrefab.AddComponent<ShrineFallenBehaviour>();
			fallenBehaviour.shrineEffectColor = new Color(0.384f, 0.874f, 0.435f);
			fallenBehaviour.symbolTransform = symbolTransform;
			fallenBehaviour.maxUses = 2;
			//fallenBehaviour.scalePerUse = true;

			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = false;
			interactable.selectionWeight = 30000;
			interactable.interactableCategory = Interactables.Category.Shrines;
			interactable.multiplayerOnly = true;


			fallenShrineInteractableInfo = Interactables.AddToStages(interactable, Interactables.Stages.Default);

		}

		public void GenerateHeresyShrine()
		{

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineRestack");

			var oldPrefab = ChanceCard.prefab;
			var oldSymbol = oldPrefab.transform.Find("Symbol");
			var oldSymbolRenderer = oldSymbol.GetComponent<MeshRenderer>();
			var oldSymbolMaterial = oldSymbolRenderer.material;



			var shrinePrefab = (GameObject)Evaisa.MoreShrines.EvaResources.ShrineHeresyPrefab;

			//Debug.Log(shrinePrefab);

			var mdlBase = shrinePrefab.transform.Find("Base").Find("mdlShrineHeresy");


			mdlBase.GetComponent<MeshRenderer>().material.shader = Shader.Find("Hopoo Games/Deferred/Standard");
			mdlBase.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.8549f, 0.7647f, 1.0f);

			var symbolTransform = shrinePrefab.transform.Find("Symbol");


			var symbolMesh = symbolTransform.gameObject.GetComponent<MeshRenderer>();

			var texture = symbolMesh.material.mainTexture;

			symbolMesh.material = new Material(Shader.Find("Hopoo Games/FX/Cloud Remap"));

			symbolMesh.material.CopyPropertiesFromMaterial(oldSymbolMaterial);
			symbolMesh.material.mainTexture = texture;

			var shrineBehaviour = shrinePrefab.AddComponent<ShrineHeresyBehaviour>();
			shrineBehaviour.shrineEffectColor = new Color(1f, 0.23f, 0.6337214f);
			shrineBehaviour.symbolTransform = symbolTransform;


			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = false;
			interactable.selectionWeight = 30000;
			interactable.interactableCategory = Interactables.Category.Shrines;


			heresyShrineInteractableInfo = Interactables.AddToStages(interactable, Interactables.Stages.Default);

		}

		public void GenerateDisorderShrine()
		{

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineRestack");

			var oldPrefab = ChanceCard.prefab;
			var oldSymbol = oldPrefab.transform.Find("Symbol");
			var oldSymbolRenderer = oldSymbol.GetComponent<MeshRenderer>();
			var oldSymbolMaterial = oldSymbolRenderer.material;



			var shrinePrefab = (GameObject)Evaisa.MoreShrines.EvaResources.ShrineDisorderPrefab;

			//Debug.Log(shrinePrefab);

			var mdlBase = shrinePrefab.transform.Find("Base").Find("mdlShrineDisorder");


			
			foreach (MeshRenderer renderer in mdlBase.transform.GetComponentsInChildren<MeshRenderer>()) {
				renderer.material.shader = Shader.Find("Hopoo Games/Deferred/Standard");
				renderer.material.color = new Color(1.0f, 0.8549f, 0.7647f, 1.0f);

			}

		

			var symbolTransform = shrinePrefab.transform.Find("Symbol");


			var symbolMesh = symbolTransform.gameObject.GetComponent<MeshRenderer>();

			var texture = symbolMesh.material.mainTexture;

			symbolMesh.material = new Material(Shader.Find("Hopoo Games/FX/Cloud Remap"));

			symbolMesh.material.CopyPropertiesFromMaterial(oldSymbolMaterial);
			symbolMesh.material.mainTexture = texture;

			var shrineBehaviour = shrinePrefab.AddComponent<ShrineDisorderBehaviour>();
			shrineBehaviour.shrineEffectColor = new Color(1f, 0.23f, 0.6337214f);
			shrineBehaviour.symbolTransform = symbolTransform;
			shrineBehaviour.modelBase = mdlBase.transform;
	
			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = false;
			interactable.selectionWeight = 30000;
			interactable.interactableCategory = Interactables.Category.Shrines;


			disorderShrineInteractableInfo = Interactables.AddToStages(interactable, Interactables.Stages.Default);

		}

		public void GenerateImpShrine()
		{

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineHealing");

			var oldPrefab = ChanceCard.prefab;
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
			symbolMesh.material.mainTexture = texture;


			var directorCard = new DirectorCard();
			directorCard.spawnCard = impSpawnCard;
			directorCard.selectionWeight = 10;
			directorCard.spawnDistance = DirectorCore.MonsterSpawnDistance.Close;
			directorCard.allowAmbushSpawn = true;
			directorCard.preventOverhead = false;
			directorCard.minimumStageCompletions = 0;

			/*var combatDirector = shrinePrefab.GetComponent<CombatDirector>();
			var cardSelection = ScriptableObject.CreateInstance<DirectorCardCategorySelection>();
			cardSelection.AddCategory("Imps", 10);
			cardSelection.AddCard(0, directorCard);

			combatDirector.monsterCards = cardSelection;
			combatDirector.monsterCredit = 300;*/


			var impBehaviour = shrinePrefab.AddComponent<ShrineImpBehaviour>();
			impBehaviour.shrineEffectColor = new Color(0.6661001f, 0.5333304f, 0.8018868f);
			impBehaviour.symbolTransform = symbolTransform;
			impBehaviour.directorCard = directorCard;


			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = false;
			interactable.selectionWeight = 3;
			interactable.interactableCategory = Interactables.Category.Shrines;


			impShrineInteractableInfo = Interactables.AddToStages(interactable, Interactables.Stages.Default);

		}


	}
}
