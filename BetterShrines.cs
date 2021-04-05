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

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace Evaisa.BetterShrines
{
	//[BepInDependency(MiniRpcPlugin.Dependency)]
	[BepInDependency("com.xoxfaby.BetterAPI")]
	[BepInPlugin(ModGuid, ModName, ModVer)]
	public class BetterShrines : BaseUnityPlugin
    {
		private const string ModVer = "1.0.0";
		private const string ModName = "BetterShrines";
		private const string ModGuid = "com.Evaisa.BetterShrines";

		public static BetterShrines instance;

		public static Xoroshiro128Plus EvaRng;

		public static CharacterSpawnCard impSpawnCard;

		public BetterShrines ()
        {
			instance = this;

			System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
			int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;

			EvaResources.Init();

			EvaRng = new Xoroshiro128Plus((ulong)cur_time);

			GenerateTinyImp();
			GenerateFallenShrine();

			RoR2.UI.ObjectivePanelController.collectObjectiveSources += (master, list) =>
			{
				if (ShrineImpBehaviour.instances.Count > 0)
				{
					ShrineImpBehaviour.instances.ForEach(instance =>
					{
						if (instance.active)
						{
							var sourceDescriptor = new ObjectivePanelController.ObjectiveSourceDescriptor
							{
								source = instance.shrineBehaviour,
								master = master,
								objectiveType = typeof(ShrineImpObjective)
							};
							instance.shrineBehaviour.sourceDescriptor = sourceDescriptor;
							instance.shrineBehaviour.sourceDescriptorList = list;
							list.Add(sourceDescriptor);
						}
					});
				}
			};

			GenerateImpShrine();

			On.RoR2.Artifacts.SwarmsArtifactManager.OnSpawnCardOnSpawnedServerGlobal += SwarmsArtifactManager_OnSpawnCardOnSpawnedServerGlobal;

			//On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;

			On.RoR2.SceneDirector.GenerateInteractableCardSelection += SceneDirector_GenerateInteractableCardSelection;
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
			impCard.loadout = impCardOriginal.loadout;

			var impPrefab = Instantiate(impCardOriginal.prefab);
			impPrefab.name = "TinyImpMaster";
			var impMaster = impPrefab.GetComponent<CharacterMaster>();
			var impBody = Instantiate(impMaster.bodyPrefab);
			impBody.name = "TinyImpBody";

			impMaster.bodyPrefab = impBody;

			var impCharBody = impBody.GetComponent<CharacterBody>();

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
				Print("Old skill destroyed");
			}

			var walkDriver = impPrefab.AddComponent<AISkillDriver>();
			walkDriver.minDistance = 0;
			walkDriver.maxDistance = 150;
			walkDriver.aimType = AISkillDriver.AimType.MoveDirection;
			walkDriver.ignoreNodeGraph = false;
			walkDriver.moveTargetType = AISkillDriver.TargetType.CurrentEnemy;
			walkDriver.shouldSprint = true;
			walkDriver.movementType = AISkillDriver.MovementType.FleeMoveTarget;
			walkDriver.moveInputScale = 1.0f;
			walkDriver.driverUpdateTimerOverride = -1;
			walkDriver.skillSlot = SkillSlot.None;

			impPrefab.AddComponent<TinyImp>();

			//impPrefab.GetComponent<BaseAI>().localNavigator.allowWalkOffCliff = false;
			Prefabs.Add(impPrefab);
			Prefabs.Add(impBody);
			Bodies.Add(impBody);

			On.RoR2.LocalNavigator.Update += LocalNavigator_Update;

			impCard.prefab = impPrefab;
			impSpawnCard = impCard; // set a public static
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
			selection.choices.ForEachTry(choice =>
			{
				//Print(choice.value.spawnCard.prefab.name);
				Print("Name: " + choice.value.spawnCard.prefab.name);
				Print("Weight: " + choice.weight);
			});
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



			var shrinePrefab = (GameObject)Evaisa.BetterShrines.EvaResources.ShrineFallenPrefab;
			var mdlBase = shrinePrefab.transform.Find("Base").Find("mdlShrineFallen");

			mdlBase.GetComponent<MeshRenderer>().material.shader = Shader.Find("Hopoo Games/Deferred/Standard");
			mdlBase.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.8549f, 0.7647f, 1.0f);

			var symbolTransform = shrinePrefab.transform.Find("Symbol");

			var purchaseInteraction = shrinePrefab.GetComponent<PurchaseInteraction>();
			purchaseInteraction.Networkcost = 300;
			purchaseInteraction.cost = 300;
			purchaseInteraction.setUnavailableOnTeleporterActivated = false;
			//purchaseInteraction.automaticallyScaleCostWithDifficulty = false;

			var symbolMesh = symbolTransform.gameObject.GetComponent<MeshRenderer>();

			var texture = symbolMesh.material.mainTexture;

			symbolMesh.material = new Material(Shader.Find("Hopoo Games/FX/Cloud Remap"));

			symbolMesh.material.CopyPropertiesFromMaterial(oldSymbolMaterial);
			symbolMesh.material.mainTexture = texture;

			var fallenBehaviour = shrinePrefab.AddComponent<ShrineFallenBehaviour>();
			fallenBehaviour.shrineEffectColor = new Color(0.384f, 0.874f, 0.435f);
			fallenBehaviour.symbolTransform = symbolTransform;
			fallenBehaviour.maxUses = 2;
			fallenBehaviour.scalePerUse = true;

			shrinePrefab.AddComponent<modifyAfterSpawn>();


			//BetterAPI.Prefabs.Add(newSpawnCard.prefab);




			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = false;
			interactable.selectionWeight = 3;
			interactable.interactableCategory = Interactables.Category.Shrines;


			Interactables.AddToStages(interactable, Interactables.Stages.Default);

		}

		public void GenerateImpShrine()
		{

			var ChanceCard = Resources.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscShrineHealing");

			var oldPrefab = ChanceCard.prefab;
			var oldSymbol = oldPrefab.transform.Find("Symbol");
			var oldSymbolRenderer = oldSymbol.GetComponent<MeshRenderer>();
			var oldSymbolMaterial = oldSymbolRenderer.material;



			var shrinePrefab = (GameObject)Evaisa.BetterShrines.EvaResources.ShrineImpPrefab;
			var mdlBase = shrinePrefab.transform.Find("Base").Find("mdlShrineImp");

			mdlBase.GetComponent<MeshRenderer>().material.shader = Shader.Find("Hopoo Games/Deferred/Standard");
			mdlBase.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.8549f, 0.7647f, 1.0f);

			var symbolTransform = shrinePrefab.transform.Find("Symbol");

			var symbolMesh = symbolTransform.gameObject.GetComponent<MeshRenderer>();

			var texture = symbolMesh.material.mainTexture;

			symbolMesh.material = new Material(Shader.Find("Hopoo Games/FX/Cloud Remap"));

			symbolMesh.material.CopyPropertiesFromMaterial(oldSymbolMaterial);
			symbolMesh.material.mainTexture = texture;



			//BetterAPI.Prefabs.Add(newSpawnCard.prefab);
			var directorCard = new DirectorCard();
			directorCard.spawnCard = impSpawnCard;
			directorCard.selectionWeight = 10;
			directorCard.spawnDistance = DirectorCore.MonsterSpawnDistance.Close;
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
			impBehaviour.chosenDirectorCard = directorCard;

			shrinePrefab.AddComponent<modifyAfterSpawn>();

			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = false;
			interactable.selectionWeight = 30000;
			interactable.interactableCategory = Interactables.Category.Shrines;


			Interactables.AddToStages(interactable, Interactables.Stages.Default);

		}
	}


	public static class EnumerableExtensions
	{

		/// <summary>
		/// ForEach but with a try catch in it.
		/// </summary>
		/// <param name="list">the enumerable object</param>
		/// <param name="action">the action to do on it</param>
		/// <param name="exceptions">the exception dictionary that will get filled, null by default if you simply want to silence the errors if any pop.</param>
		/// <typeparam name="T"></typeparam>
		public static void ForEachTry<T>(this IEnumerable<T>? list, Action<T>? action, IDictionary<T, Exception?>? exceptions = null)
		{
			list.ToList().ForEach(element => {
				try
				{
					action(element);
				}
				catch (Exception exception)
				{
					exceptions?.Add(element, exception);
				}
			});
		}
	}
}
