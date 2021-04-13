using System;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using RoR2.UI;
using BetterAPI;

namespace Evaisa.MoreShrines
{
	[RequireComponent(typeof(PurchaseInteraction))]
	public class ShrineImpBehaviour : NetworkBehaviour
	{
		public PurchaseInteraction purchaseInteraction;
		public CombatDirector combatDirector;
		public CombatSquad combatSquad;
		public Color shrineEffectColor;
		public Transform symbolTransform;
		public int baseImpCount = 8;
		public float baseCredit = 20;
		public DirectorCard directorCard;

		public static List<ShrineImpBehaviour> instances = new List<ShrineImpBehaviour>();

		public string objectiveBaseToken = "OBJECTIVE_KILL_TINY_IMPS";

		public string objectiveString = "Undefined";

		[SyncVar]
		public float monsterCredit;

		[SyncVar]
		public Color impColor;

		[SyncVar]
		public string impColorHex;

		[SyncVar]
		public int impsSpawned;

		[SyncVar]
		public bool active;

		[SyncVar]
		public float timeLeftUnrounded;

		[SyncVar]
		public int timeLeft;

		[SyncVar]
		public int startTime;

		public bool failedShrine = false;

		Objectives.ObjectiveInfo objectiveInfo;

		public void Awake()
        {
			purchaseInteraction = GetComponent<PurchaseInteraction>();
			combatDirector = GetComponent<CombatDirector>();
			combatSquad = GetComponent<CombatSquad>();
			instances.Add(this);

			if (NetworkServer.active)
			{
				purchaseInteraction.onPurchase.AddListener((interactor) =>
				{
					AddShrineStack(interactor);
				});
				startTime = MoreShrines.impShrineTime.Value;
				timeLeftUnrounded = (float)MoreShrines.impShrineTime.Value;

				var difficulty = Run.instance.difficultyCoefficient;
				var difficultyMultiplier = DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty).scalingValue - 1;
				monsterCredit = ((baseCredit * (difficultyMultiplier + 1)) + ((baseCredit + 30) * (difficulty - 1)));
				impColor = UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 0.7f, 1f, 1f);
				impColorHex = "#"+ColorUtility.ToHtmlStringRGB(impColor);
				combatDirector.combatSquad.onDefeatedServer += OnDefeatedServer;
				combatDirector.onSpawnedServer.AddListener((gameObject) =>
				{
					OnSpawnedServer(gameObject);
					//RpcOnSpawnedClient(gameObject.GetComponent<NetworkBehaviour>().netId);
				});
			}
			
			/*
			RoR2.UI.ObjectivePanelController.collectObjectiveSources += (master, list) =>
			{
				if (active)
				{
					sourceDescriptor = new ObjectivePanelController.ObjectiveSourceDescriptor
					{
						source = this,
						master = master,
						objectiveType = typeof(ShrineImpObjective)
					};
					sourceDescriptorList = list;
					list.Add(sourceDescriptor);
				}
			};
			*/

			//AddObjectiveSource(this, typeof(ShrineImpObjective), isActive);
		}


		/*
        private void CombatSquad_onMemberDiscovered(CharacterMaster obj)
        {
			Debug.Log("Imp discovered, owo.");
        }
		*/

        private int impCount 
		{
			get {
				var playerCount = Run.instance.participatingPlayerCount;
				var runDifficulty = DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty).scalingValue;
				var difficulty = Run.instance.difficultyCoefficient;

				var count = Math.Min((int)Math.Round((float)(baseImpCount - 1) + (playerCount * runDifficulty)), 17);

				if (MoreShrines.impCountScale.Value)
				{
					count += (int) Math.Round(difficulty* 1.3f);
				}


				if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Swarms))
				{
					count *= 2;
				}

				return count;
			}	
		}

		public int impsAlive
        {
			get {
				return combatSquad.memberCount;
			}
        }

		[ClientRpc]
		public void RpcHandleLoss()
		{
			
			failedShrine = true;
			Objectives.RemoveObjective(objectiveInfo);
			if (NetworkServer.active)
			{
				List<CharacterMaster> members = new List<CharacterMaster>();
				for (int i = combatSquad.membersList.Count - 1; i >= 0; i--)
				{
					combatSquad.membersList[i].TrueKill();
				}
			}
		}



		void Update()
		{
			if (active)
            {
                if (NetworkServer.active)
                {
					timeLeftUnrounded -= Time.deltaTime;
					timeLeft = (int)Math.Round(timeLeftUnrounded);
					if (timeLeft < 0)
					{
						active = false;
						timeLeft = 0;
						RpcHandleLoss();
					}
				}


				if (combatSquad.memberCount > 0)
                {
					var killedImpCount = impsSpawned - impsAlive;
					objectiveString = string.Format(Language.GetString(objectiveBaseToken), impColorHex, killedImpCount, impsSpawned, this.timeLeft);

					objectiveInfo.title = objectiveString;
					objectiveInfo.show = true;

					foreach (var imp in combatSquad.membersList)
                    {
						Debug.Log("Rawr 1");
                        if (imp.gameObject)
                        {
							Debug.Log("Rawr 2");
							if (imp.gameObject.GetComponent<TinyImp>())
							{
								Debug.Log("Rawr 3");
								if (!imp.gameObject.GetComponent<TinyImp>().hasMarker)
								{
									Debug.Log("A imp was marked!");
									markImp(imp);
									imp.gameObject.GetComponent<TinyImp>().hasMarker = true;
								}
							}
                        }
                    }
                }
            }
        }

		[ClientRpc]
		public void RpcAddShrineStackClient()
		{
			var killedImpCount = impsSpawned - impsAlive;
			objectiveString = string.Format(Language.GetString(objectiveBaseToken), impColorHex, killedImpCount, impsSpawned, this.timeLeft);
			objectiveInfo = Objectives.AddObjective(objectiveString, true);
			symbolTransform.gameObject.SetActive(false);
			
			combatDirector.monsterCredit += monsterCredit;
			combatDirector.maximumNumberToSpawnBeforeSkipping = impCount;
			combatDirector.OverrideCurrentMonsterCard(directorCard);
			combatDirector.monsterSpawnTimer = 0f;
			
		}


		public void AddShrineStack(Interactor interactor)
		{
			RpcAddShrineStackClient();
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineCombatBehavior::AddShrineStack(RoR2.Interactor)' called on client");
				return;
			}
			ImpShrineActivation(interactor, monsterCredit);
			EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
			{
				origin = transform.position,
				rotation = Quaternion.identity,
				scale = 1f,
				color = shrineEffectColor
			}, true);

			
			active = true;

		}



		private void markImp(CharacterMaster imp)
		{
			GameObject gameObject = Instantiate(Resources.Load<GameObject>("Prefabs/PositionIndicators/PoiPositionIndicator"), imp.GetBodyObject().transform.position, imp.GetBodyObject().transform.rotation);
			var positionIndicator = gameObject.GetComponent<PositionIndicator>();
			//positionIndicator.alwaysVisibleObject = true;

			positionIndicator.insideViewObject.GetComponent<SpriteRenderer>().color = impColor;
			Destroy(positionIndicator.insideViewObject.GetComponent<ObjectScaleCurve>());
			positionIndicator.insideViewObject.transform.localScale = positionIndicator.insideViewObject.transform.localScale / 2f;
			positionIndicator.insideViewObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("textures/miscicons/texAttackIcon");

			positionIndicator.outsideViewObject.transform.Find("Sprite").GetComponent<SpriteRenderer>().color = impColor;

			positionIndicator.targetTransform = imp.GetBodyObject().transform;
			gameObject.AddComponent<ImpMarkerKiller>();
		}

		public void ImpShrineActivation(Interactor interactor, float monsterCredit)
		{
			CharacterMaster component = directorCard.spawnCard.prefab.GetComponent<CharacterMaster>();
			combatDirector.enabled = true;
			if (component)
			{
				CharacterBody component2 = component.bodyPrefab.GetComponent<CharacterBody>();
				if (component2)
				{
					Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
					{
						subjectAsCharacterBody = interactor.GetComponent<CharacterBody>(),
						baseToken = "SHRINE_IMP_USE_MESSAGE",
						paramTokens = new string[]
						{
							component2.baseNameToken
						}
					});
				}
			}
		}

		private void OnSpawnedServer(GameObject gameObject)
		{
			impsSpawned++;
			if (impsSpawned >= impCount)
			{
				combatDirector.enabled = false;
			}
		}

		private List<PickupIndex> weightedTierPickSpeed()
		{
			var startTier3Weight = 30;
			var startTier2Weight = 55;
			var startTier1Weight = 15;

			var endTier3Weight = 0;
			var endTier2Weight = 30;
			var endTier1Weight = 70;

			var tier3Weight = (int)Math.Floor(Mathf.Lerp(endTier3Weight, startTier3Weight, timeLeftUnrounded / startTime));
			var tier2Weight = (int)Math.Floor(Mathf.Lerp(endTier2Weight, startTier2Weight, timeLeftUnrounded / startTime));
			var tier1Weight = (int)Math.Floor(Mathf.Lerp(endTier1Weight, startTier1Weight, timeLeftUnrounded / startTime));

			var weights = new Dictionary<List<PickupIndex>, int>();
			weights.Add(Run.instance.availableTier3DropList, tier3Weight);
			weights.Add(Run.instance.availableTier2DropList, tier2Weight);
			weights.Add(Run.instance.availableTier1DropList, tier1Weight);

			Evaisa.MoreShrines.MoreShrines.Print("Time percentage: " + timeLeftUnrounded / startTime);
			Evaisa.MoreShrines.MoreShrines.Print("Tier 3 weight: " + tier3Weight);
			Evaisa.MoreShrines.MoreShrines.Print("Tier 2 weight: " + tier2Weight);
			Evaisa.MoreShrines.MoreShrines.Print("Tier 1 weight: " + tier1Weight);

			var outcomePick = WeightedRandomizer.From(weights).TakeOne();

			return outcomePick;
		}
		private List<PickupIndex> weightedTierPick()
		{
			var tier3Weight = 5;
			var tier2Weight = 40;
			var tier1Weight = 55;

			var weights = new Dictionary<List<PickupIndex>, int>();
			weights.Add(Run.instance.availableTier3DropList, tier3Weight);
			weights.Add(Run.instance.availableTier2DropList, tier2Weight);
			weights.Add(Run.instance.availableTier1DropList, tier1Weight);

			Evaisa.MoreShrines.MoreShrines.Print("Time percentage: " + timeLeftUnrounded / startTime);
			Evaisa.MoreShrines.MoreShrines.Print("Tier 3 weight: " + tier3Weight);
			Evaisa.MoreShrines.MoreShrines.Print("Tier 2 weight: " + tier2Weight);
			Evaisa.MoreShrines.MoreShrines.Print("Tier 1 weight: " + tier1Weight);

			var outcomePick = WeightedRandomizer.From(weights).TakeOne();

			return outcomePick;
		}

		private void DropRewards()
		{
			int participatingPlayerCount = Run.instance.participatingPlayerCount;
			if (participatingPlayerCount != 0 && symbolTransform)
			{
				List<PickupIndex> list = new List<PickupIndex>();

				if (MoreShrines.itemRarityBasedOnSpeed.Value)
				{
					list = weightedTierPickSpeed();
				}
				else
				{
					list = weightedTierPick();
				}


				PickupIndex pickupIndex = MoreShrines.EvaRng.NextElementUniform<PickupIndex>(list);
				int num = 1;

				if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.funkfrog_sipondo.sharesuite"))
				{
					num *= participatingPlayerCount;
				}

				num += MoreShrines.extraItemCount.Value;


				float angle = 360f / (float)num;
				Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
				Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
				int i = 0;
				while (i < num)
				{
					PickupDropletController.CreatePickupDroplet(pickupIndex, symbolTransform.position, vector);
					i++;
					vector = rotation * vector;
				}
			}
		}

		[ClientRpc]
		public void RpcOnDefeatedClient()
		{
			objectiveString = string.Format(Language.GetString(objectiveBaseToken), impColorHex, impsSpawned, impsSpawned, this.timeLeft);
			objectiveInfo.title = objectiveString;
			Objectives.RemoveObjective(objectiveInfo);
			active = false;
		}

		public void OnDefeatedServer()
        {
			if (!failedShrine)
			{
				RpcOnDefeatedClient();
				DropRewards();
			}
        }
	}
}
