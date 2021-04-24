using System;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using RoR2.UI;
using BetterAPI;
using RoR2.CharacterAI;

namespace Evaisa.MoreShrines
{
	[RequireComponent(typeof(PurchaseInteraction))]
	public class ShrineWispBehaviour : NetworkBehaviour
	{
		public PurchaseInteraction purchaseInteraction;
		public CombatDirector combatDirector;
		public CombatSquad combatSquad;
		public Color shrineEffectColor;
		public Transform symbolTransform;
		public int baseWispCount = 4;
		public float baseCredit = 40;

		public CharacterBody shrineOwner;

		public bool acceptNext = false;

		[SyncVar]
		public int tier = 1;

		[SyncVar]
		public int wispsSpawned;

		public List<CharacterBody> ghosts = new List<CharacterBody>();

		public ItemDef wispItem;

		public void Awake()
        {
			purchaseInteraction = GetComponent<PurchaseInteraction>();
			combatDirector = GetComponent<CombatDirector>();
			combatSquad = GetComponent<CombatSquad>();

			purchaseInteraction.Networkcost = 1;
			purchaseInteraction.cost = 1;




			combatDirector.shouldSpawnOneWave = false;

			combatDirector.resetMonsterCardIfFailed = true;

			if (NetworkServer.active)
			{
				acceptNext = UnityEngine.Random.Range(1, 100) > 40;

				if(acceptNext == false)
                {
					combatDirector.teamIndex = TeamIndex.Monster;
                }

				var rand = UnityEngine.Random.Range(1, 100);

				purchaseInteraction.costType = CostTypes.getCostTypeIndex(MoreShrines.costTypeDefWispWhite);

				if (rand <= 40)
				{
					purchaseInteraction.costType = CostTypes.getCostTypeIndex(MoreShrines.costTypeDefWispGreen);
					tier = 2;
				}

				if (rand <= 5)
				{
					purchaseInteraction.costType = CostTypes.getCostTypeIndex(MoreShrines.costTypeDefWispRed);
					tier = 3;
					var particleSystem = symbolTransform.GetComponent<ParticleSystem>();
					//particleSystem.colorOverLifetime.color.colorMin.r = 1.0f;
					Gradient grad = new Gradient();
					grad.SetKeys(new GradientColorKey[] { new GradientColorKey(new Color(1.0f, 0.31f, 0f), 0.0f), new GradientColorKey(new Color(0.75f, 0f, 0.14f), 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) });
					var colorOverLifetime = particleSystem.colorOverLifetime;
					colorOverLifetime.color = grad;
					var emission = particleSystem.emission;
					emission.rateOverTime = 20f;
					var main = particleSystem.main;
					main.maxParticles = 80;
				}

				purchaseInteraction.onPurchase.AddListener((interactor) =>
				{
					AddShrineStack(interactor);
				});

				combatDirector.onSpawnedServer.AddListener((gameobject) =>
				{
					OnSpawnedServer(gameObject);
				});

				if (!acceptNext)
				{
					combatSquad.onMemberDefeatedServer += CombatSquad_onMemberDefeatedServer;
				}
			}

			//gameObject.GetComponent<CustomDirector>().countToSpawn = wispCount;
			// On.RoR2.CombatDirector.PrepareNewMonsterWave += CombatDirector_PrepareNewMonsterWave;
		}

        private void CombatSquad_onMemberDefeatedServer(CharacterMaster arg1, DamageReport damageReport)
        {
			if (!damageReport.victimMaster)
			{
				return;
			}
			if (damageReport.attackerTeamIndex == damageReport.victimTeamIndex && damageReport.victimMaster.minionOwnership.ownerMaster)
			{
				return;
			}

			if (UnityEngine.Random.Range(1, 100) < 15)
			{
				PickupIndex pickupIndex = PickupCatalog.itemIndexToPickupIndex[(int)wispItem.itemIndex];
				if (pickupIndex != PickupIndex.none)
				{
					PickupDropletController.CreatePickupDroplet(pickupIndex, damageReport.victimBody.corePosition, Vector3.up * 20f);
				}
			}
		}


		/*
        private void CombatDirector_PrepareNewMonsterWave(On.RoR2.CombatDirector.orig_PrepareNewMonsterWave orig, CombatDirector self, DirectorCard monsterCard)
        {
			orig(self, monsterCard);
			if (self.gameObject.GetComponent<ShrineWispBehaviour>())
            {

				self.currentMonsterCard = monsterCard;

				if(self.currentMonsterCard.cost * wispCount < monsterCredit)
                {
					self.currentMonsterCard = self.monsterCards.categories[0].cards[0];
                }

				if(self.currentMonsterCard.cost > self.monsterCredit)
                {
					self.currentMonsterCard = self.monsterCards.categories[0].cards[0];
				}

				self.ResetEliteType();

				self.currentActiveEliteTier = CombatDirector.eliteTiers[0];

				if (!(self.currentMonsterCard.spawnCard as CharacterSpawnCard).noElites)
				{
					for (int i = 1; i < CombatDirector.eliteTiers.Length; i++)
					{
						CombatDirector.EliteTierDef eliteTierDef = CombatDirector.eliteTiers[i];
						if (eliteTierDef.isAvailable(self.currentMonsterCard.spawnCard.eliteRules))
						{

							float num = eliteTierDef.costMultiplier * self.eliteBias;
							float num1 = (float)(self.currentMonsterCard.cost * wispCount - 1) + (self.currentMonsterCard.cost * num);
							//MoreShrines.Print("WispCount: " + wispCount);
							//MoreShrines.Print("WispCheck: " + num1 + " < " + monsterCredit);
							if (num1 < monsterCredit)
							{
								if ((float)self.currentMonsterCard.cost * num < self.monsterCredit)
								{
									self.currentActiveEliteTier = eliteTierDef;
								}
							}
						}
					}
				}

				self.currentActiveEliteDef = self.rng.NextElementUniform<EliteDef>(self.currentActiveEliteTier.eliteTypes);
			
				self.spawnCountInCurrentWave = 0;
			}

        }
		*/

        void Update()
        {
			combatDirector.monsterSpawnTimer = 0f;
			combatDirector.currentMonsterCard = null;

			/*
            if (combatDirector.enabled)
            {
				if(combatDirector.monsterCredit < 40)
                {
					combatDirector.enabled = false;
                }
            }
			*/
			if (NetworkServer.active)
			{
				var newMembers = new List<CharacterMaster>();
				foreach (var member in combatSquad.membersList)
				{
					newMembers.Add(member);
				}


				foreach (var member in newMembers)
				{

					if (member.GetBody())
					{
						if (shrineOwner.master)
						{
							if (!ghosts.Contains(member.GetBody()))
							{
								MakeGhost(member.GetBody(), shrineOwner);
								ghosts.Add(member.GetBody());

   
								//MoreShrines.Print("Hella heck!");
							}
						}	

					}
				}
			}
		}

		public void MakeGhost(CharacterBody body, CharacterBody owner)
        {
			if (acceptNext)
			{
				body.teamComponent.teamIndex = TeamComponent.GetObjectTeam(owner.gameObject);

				if (!body.master.GetComponent<AIOwnership>())
				{
					body.master.gameObject.AddComponent<AIOwnership>();
				}

				if (!body.master.GetComponent<MinionOwnership>())
				{
					body.master.gameObject.AddComponent<MinionOwnership>();
				}

				AIOwnership aiOwnership = body.master.GetComponent<AIOwnership>();
				aiOwnership.ownerMaster = owner.master;
				

				BaseAI baseAI = body.master.GetComponent<BaseAI>();
				baseAI.leader.gameObject = owner.gameObject;
				baseAI.leader.characterBody = owner;


				/*
	
				var skillDrivers = body.master.GetComponents<AISkillDriver>();

				foreach (var oldDriver in skillDrivers)
				{
                    UnityEngine.Object.Destroy(oldDriver);
				}
				
				var droneCard = Resources.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscBackupDrone");

				var skillDriversDrone = droneCard.prefab.GetComponents<AISkillDriver>();

				foreach(var skillDriver in skillDriversDrone)
                {
					MoreShrines.Print("Cloning Skill: " + skillDriver.customName);
					CopyComponent(skillDriver, body.master.gameObject);
                }
			*/

				body.inventory.GiveItem(RoR2Content.Items.Ghost);

            }

			body.inventory.GiveItem(wispItem, wispCount);
		}
		Component CopyComponent(Component original, GameObject destination)
		{
			System.Type type = original.GetType();
			Component copy = destination.AddComponent(type);
			// Copied fields can be restricted with BindingFlags
			System.Reflection.FieldInfo[] fields = type.GetFields();
			foreach (System.Reflection.FieldInfo field in fields)
			{
				field.SetValue(copy, field.GetValue(original));
			}
			return copy;
		}

		private void OnSpawnedServer(GameObject gameObject)
		{
			wispsSpawned++;
			//MoreShrines.Print("Does this work: " + wispsSpawned);
			if (wispsSpawned >= wispCount)
			{
				//MoreShrines.Print("Does seem so.");
				combatDirector.enabled = false;
				RpcDisableDirector();
			}
			
		}

		[ClientRpc]
		public void RpcDisableDirector()
		{
			combatDirector.enabled = false;
		}


		private int wispCount 
		{
			get {
				var runDifficulty = DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty).scalingValue;
				var difficulty = Run.instance.difficultyCoefficient;

				var count = Math.Min((int)Math.Round((float)(baseWispCount) + (runDifficulty - 1)), 8);

				//MoreShrines.Print("Run Difficulty: " + runDifficulty);
				//MoreShrines.Print("Difficulty: " + difficulty);

				if (MoreShrines.wispShrineScaleDifficulty.Value)
				{
					count += (int) Math.Round((difficulty - 1) * 1.3f);
				}


				if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Swarms))
				{
					count *= 2;
				}

				return count;
			}	
		}

		private float monsterCredit
        {
			get {

				var difficulty = Run.instance.difficultyCoefficient;
				var difficultyMultiplier = DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty).scalingValue - 1;

				return ((baseCredit * (difficultyMultiplier + 1)) + ((baseCredit + 30) * (difficulty - 1)));
			}
        }




		[ClientRpc]
		public void RpcAddShrineStackClient(NetworkInstanceId interactorId)
		{
			symbolTransform.gameObject.SetActive(false);
			if (NetworkServer.objects.ContainsKey(interactorId))
			{
				Interactor interactor = NetworkServer.objects[interactorId].gameObject.GetComponent<Interactor>();
				shrineOwner = interactor.GetComponent<CharacterBody>();
			}
		}


		public void AddShrineStack(Interactor interactor)
		{
			RpcAddShrineStackClient(interactor.GetComponent<CharacterBody>().netId);
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineCombatBehavior::AddShrineStack(RoR2.Interactor)' called on client");
				return;
			}
			shrineOwner = interactor.GetComponent<CharacterBody>();
			wispShrineActivation(interactor, monsterCredit);
			EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
			{
				origin = transform.position,
				rotation = Quaternion.identity,
				scale = 1f,
				color = shrineEffectColor
			}, true);
		}


		public void wispShrineActivation(Interactor interactor, float monsterCredit)
		{
			combatDirector.enabled = true;
			combatDirector.monsterCredit += monsterCredit;
			combatDirector.maximumNumberToSpawnBeforeSkipping = wispCount;
			combatDirector.monsterSpawnTimer = 0f;
			

			if (wispItem)
			{
				var itemColor = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Tier1Item);
				if(tier == 2)
                {
					itemColor = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Tier2Item);
                }
				if (tier == 3)
				{
					itemColor = ColorCatalog.GetColorHexString(ColorCatalog.ColorIndex.Tier3Item);
				}
				if (acceptNext)
				{
					Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
					{
						subjectAsCharacterBody = interactor.GetComponent<CharacterBody>(),
						baseToken = "SHRINE_WISP_ACCEPT_MESSAGE",
						paramTokens = new string[]
						{
							itemColor,
							wispItem.nameToken,
						}
					});
                }
                else
                {
					Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
					{
						subjectAsCharacterBody = interactor.GetComponent<CharacterBody>(),
						baseToken = "SHRINE_WISP_DENY_MESSAGE",
						paramTokens = new string[]
						{
							itemColor,
							wispItem.nameToken,
						}
					});
				}
			}

		}



	}
}
