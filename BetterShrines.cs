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

		public BetterShrines ()
        {
			instance = this;

			System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
			int cur_time = (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;

			EvaResources.Init();

			EvaRng = new Xoroshiro128Plus((ulong)cur_time);

			GenerateFallenShrine();

           // On.RoR2.SceneDirector.Start += SceneDirector_Start;

            //IL.RoR2.SceneDirector.PopulateScene += SceneDirector_PopulateScene;
            On.RoR2.SceneDirector.GenerateInteractableCardSelection += SceneDirector_GenerateInteractableCardSelection;
		}

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
            if (self.gameObject.GetComponent<ShrineFallenBehavior>())
            {
				if(self.gameObject.GetComponent<ShrineFallenBehavior>().isAvailable == false)
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

			var fallenBehaviour = shrinePrefab.AddComponent<ShrineFallenBehavior>();
			fallenBehaviour.shrineEffectColor = new Color(0.384f, 0.874f, 0.435f);
			fallenBehaviour.symbolTransform = symbolTransform;
			fallenBehaviour.maxUses = 2;
			fallenBehaviour.scalePerUse = true;

			shrinePrefab.AddComponent<modifyAfterSpawn>();


			//BetterAPI.Prefabs.Add(newSpawnCard.prefab);




			var interactable = new BetterAPI.Interactables.InteractableTemplate();
			interactable.interactablePrefab = shrinePrefab;
			interactable.slightlyRandomizeOrientation = true;
			interactable.selectionWeight = 3000;
			interactable.interactableCategory = Interactables.Category.Shrines;


			Interactables.AddToStages(interactable, Interactables.Stages.AbandonedAqueduct | Interactables.Stages.AbyssalDepths | Interactables.Stages.Commencement | Interactables.Stages.DistantRoost | Interactables.Stages.RallypointDelta | Interactables.Stages.ScorchedAcres | Interactables.Stages.SirensCall | Interactables.Stages.SkyMeadow | Interactables.Stages.SunderedGrove | Interactables.Stages.TitanicPlains | Interactables.Stages.WetlandAspect );

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
