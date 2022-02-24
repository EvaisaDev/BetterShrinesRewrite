using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Evaisa.MoreShrines
{
	class CustomDirector : NetworkBehaviour
	{
		List<CardPool> finalSpawnOrder = new List<CardPool>();

		bool cardPoolInitialized = false;
		public int countToSpawn = 0;

		CombatDirector director;

		public void Awake()
        {
			On.RoR2.CombatDirector.AttemptSpawnOnTarget += AttemptSpawnOnTarget;
			director = gameObject.GetComponent<CombatDirector>();
		}
		/*
		public void Init()
		{
            On.RoR2.CombatDirector.AttemptSpawnOnTarget += CombatDirector_AttemptSpawnOnTarget; 
		}
		*/

		// private bool CombatDirector_AttemptSpawnOnTarget(On.RoR2.CombatDirector.orig_AttemptSpawnOnTarget orig, CombatDirector self, Transform spawnTarget, DirectorPlacementRule.PlacementMode placementMode)
		// {
		// throw new NotImplementedException();
		//return AttemptSpawnOnTarget<ShrineImpBehaviour>(orig, self, spawnTarget, placementMode);

		//}

		void OnDestroy()
		{
			On.RoR2.CombatDirector.AttemptSpawnOnTarget -= AttemptSpawnOnTarget;
		}


		public class CardPool
		{
			public int cost = 0;
			public List<DirectorCard> cards = new List<DirectorCard>();
		}

        public bool AttemptSpawnOnTarget(On.RoR2.CombatDirector.orig_AttemptSpawnOnTarget orig, CombatDirector self, Transform spawnTarget, DirectorPlacementRule.PlacementMode placementMode)
		{

			//this.GetComponent< behaviourName as MonoBehaviour>

			
			

			if (self.gameObject == this.gameObject)
			{
				
				var credit = self.monsterCredit;
				var monsterCards = self.monsterCards;


				//MoreShrines.Print("We good?");

		
				if (!this.cardPoolInitialized)
				{

					List<CardPool> cardPools = new List<CardPool>();

					foreach (var category in monsterCards.categories)
					{
						foreach (var card in category.cards)
						{
							if (cardPools.Any(pool => pool.cost == card.cost))
							{
								cardPools.Find(pool => pool.cost == card.cost).cards.Add(card);

							}
							else
							{
								var pool = new CardPool();
								pool.cost = card.cost;
								pool.cards.Add(card);
								cardPools.Add(pool);
							}
						}
					}
					cardPoolInitialized = true;
					cardPools.Sort((item1, item2) => { return item1.cost.CompareTo(item2.cost); });


					var poolIndex = 0;

					CardPool currentBasePool = cardPools[0];

					foreach(var pool in cardPools)
                    {
						if (pool.cost * countToSpawn < credit)
                        {
							currentBasePool = pool;
                        }
                        else
                        {
							break;
                        }
						poolIndex++;
                    }

					MoreShrines.Print("Preparing to spawn " + countToSpawn + " monsters.");

					CardPool buffPool = cardPools[0];

					var buffCount = 0;
					for(var i = 0; i < countToSpawn; i++)
					{
						var count = countToSpawn - i;

						if (cardPools.Count > poolIndex + 1)
						{
							if (((countToSpawn - count) * currentBasePool.cost) + (count * cardPools[poolIndex + 1].cost) < credit)
							{
								buffPool = cardPools[poolIndex + 1];
								buffCount++;
							}
						}
                    }

					for(var i = 0; i < countToSpawn - buffCount; i++)
                    {
						finalSpawnOrder.Add(currentBasePool);
                    }

					for (var i = 0; i < buffCount; i++)
					{
						finalSpawnOrder.Add(buffPool);
					}
				}

				//

				if(finalSpawnOrder.Count > 0)
                {
					self.currentMonsterCard = finalSpawnOrder[0].cards[Random.Range(0, finalSpawnOrder[0].cards.Count - 1)];
					SpawnCard spawnCard = self.currentMonsterCard.spawnCard;
					DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule
					{
						placementMode = placementMode,
						spawnOnTarget = spawnTarget,
						preventOverhead = self.currentMonsterCard.preventOverhead
					};
					DirectorCore.GetMonsterSpawnDistance(self.currentMonsterCard.spawnDistance, out directorPlacementRule.minDistance, out directorPlacementRule.maxDistance);
					directorPlacementRule.minDistance *= self.spawnDistanceMultiplier;
					DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, directorPlacementRule, self.rng);
					directorSpawnRequest.ignoreTeamMemberLimit = self.ignoreTeamSizeLimit;
					directorSpawnRequest.teamIndexOverride = new TeamIndex?(self.teamIndex);
					directorSpawnRequest.onSpawnedServer = new Action<SpawnCard.SpawnResult>(onCardSpawned);
					if (!DirectorCore.instance.TrySpawnObject(directorSpawnRequest))
					{
						/*
						Debug.LogFormat("Spawn card {0} failed to spawn. Aborting cost procedures.", new object[]
						{
					spawnCard
						});*/
						self.enabled = false;
						return false;
					}
				}
                else
                {
					self.enabled = false;
					return false;
                }


				self.spawnCountInCurrentWave += 1;
				
				return true;
			}
			else
			{

				return orig(self, spawnTarget, placementMode);
			}
        }


		internal void onCardSpawned(SpawnCard.SpawnResult result)
		{
			if (result.success)
			{
				CharacterMaster component = result.spawnedInstance.GetComponent<CharacterMaster>();
				GameObject bodyObject = component.GetBodyObject();
				if (director.combatSquad)
				{
					director.combatSquad.AddMember(component);
				}

				if (director.spawnEffectPrefab && NetworkServer.active)
				{
					Vector3 origin = result.position;
					CharacterBody component3 = bodyObject.GetComponent<CharacterBody>();
					if (component3)
					{
						origin = component3.corePosition;
					}
					EffectManager.SpawnEffect(director.spawnEffectPrefab, new EffectData
					{
						origin = origin
					}, true);
				}
				CombatDirector.OnSpawnedServer onSpawnedServer = director.onSpawnedServer;
				if (onSpawnedServer == null)
				{
					return;
				}
				onSpawnedServer.Invoke(result.spawnedInstance);
			}
		}

	}
}
