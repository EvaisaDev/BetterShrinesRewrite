using System;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using BetterAPI;

namespace Evaisa.MoreShrines
{
	[RequireComponent(typeof(PurchaseInteraction))]
	public class ShrineFallenBehaviour : NetworkBehaviour
	{
		public Interactor whoInteracted;
		public Transform symbolTransform;
		public Color shrineEffectColor;
		public int maxUses = 1;

		public List<CharacterMaster> playersToRespawn = new List<CharacterMaster>();

		[SyncVar]
		public int timesUsed = 0;

		//[SyncVar]
		//public bool scalePerUse;
		//public float scaleMultiplier = 1.5f;
		public float wait = 2f;

		[SyncVar]
		public float stopwatch;
		public PurchaseInteraction purchaseInteraction;

		[SyncVar]
		public bool inUse = false;

		[SyncVar]
		public bool isAvailable = true;


		public void Awake()
        {
			purchaseInteraction = GetComponent<PurchaseInteraction>();

			purchaseInteraction.costType = (CostTypeIndex)CostTypes.getCostTypeIndex(MoreShrines.costTypeDefShrineFallen);

			purchaseInteraction.onPurchase.AddListener((interactor) =>
			{
				AddShrineStack(interactor);
			});

            On.RoR2.CharacterMaster.RespawnExtraLife += CharacterMaster_RespawnExtraLife;
		}

        private void CharacterMaster_RespawnExtraLife(On.RoR2.CharacterMaster.orig_RespawnExtraLife orig, CharacterMaster self)
        {
			orig(self);

			if (playersToRespawn.Contains(self))
			{
				playersToRespawn.Remove(self);
				var characterBody = self.GetBody();

				CharacterBody component = characterBody;
				if (component)
				{
					var count = component.GetBuffCount(InitBuffs.maxHPDownStage);
					var added_count = (int)Mathf.Ceil(((100f - (float)component.GetBuffCount(InitBuffs.maxHPDownStage)) / 100f) * (float)purchaseInteraction.cost);
					//Debug.Log(count + added_count);
					if (count + added_count < 100)
					{



						for (var i = 0; i < added_count; i++)
						{
							component.AddBuff(InitBuffs.maxHPDownStage);
						}
					}
				}
			}
		}

        public void Update()
        {
			//BetterShrines.Print(Run.instance.difficultyCoefficient);
			
			if (timesUsed < maxUses)
			{
				if (inUse)
				{
					stopwatch -= Time.deltaTime;
					if (stopwatch < 0)
					{
						inUse = false;
					}
				}
				else
				{
					if (IsAnyoneDead())
					{
						
						symbolTransform.gameObject.SetActive(true);
						isAvailable = true;
						//if (NetworkServer.active)
						//{
							//purchaseInteraction.SetAvailable(true);
						//	Debug.Log("Someone is dead.");
						//}
					}
					else
					{
						symbolTransform.gameObject.SetActive(false);
						isAvailable = false;
						//if (NetworkServer.active)
						//{
							//purchaseInteraction.SetAvailable(false);
						//	Debug.Log("No one is dead.");
						//}
					}
				}
            }
            else
            {
				symbolTransform.gameObject.SetActive(false);
				isAvailable = false;
				if (NetworkServer.active)
				{
					purchaseInteraction.SetAvailable(false);
				}
			}
			
		}

		[ClientRpc]
		public void RpcAddShrineStackClient()
		{
			symbolTransform.gameObject.SetActive(false);
		}


		public void AddShrineStack(Interactor interactor)
		{
			RpcAddShrineStackClient();
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineCombatBehavior::AddShrineStack(RoR2.Interactor)' called on client");
				return;
			}

			MoreShrines.Print(interactor.name + " has used a Shrine of the Fallen");
			if (IsAnyoneDead())
			{
				timesUsed += 1;
				stopwatch = wait;
				inUse = true;
				whoInteracted = interactor;
				
				EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
				{
					origin = base.transform.position,
					rotation = Quaternion.identity,
					scale = 1f,
					color = shrineEffectColor
				}, true);

				
				/*
				if (scalePerUse)
                {
					purchaseInteraction.Networkcost = (int)Math.Round(purchaseInteraction.cost * scaleMultiplier);
					purchaseInteraction.cost = (int)Math.Round(purchaseInteraction.cost * scaleMultiplier);
                }
				*/


				if (NetworkServer.active)
				{
					MoreShrines.Print("attempting to revive user.");
					var player = getRandomDeadPlayer();
					if (player != null)
					{

						//player.body.AddBuff
						
						playersToRespawn.Add(player.master);
						player.GetComponent<CharacterMaster>().RespawnExtraLife();

						Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
						{
							subjectAsCharacterBody = interactor.GetComponent<CharacterBody>(),
							baseToken = "SHRINE_FALLEN_USED",
							paramTokens = new string[]
							{
								player.networkUser.userName
							}
						});
					}
				}
			}
		}

		public static bool IsAnyoneDead()
        {
			var isAnyoneDead = false;
			foreach(var instance in PlayerCharacterMasterController.instances)
			{
                if (instance.master)
                {
					CharacterBody body = instance.master.GetBody();
					if ((!body || !body.healthComponent.alive) && instance.master.inventory.GetItemCount(ItemCatalog.FindItemIndex("ExtraLife")) <= 0)
                    {
						isAnyoneDead = true;
                    }
                }
			};
			return isAnyoneDead;
        }

		public PlayerCharacterMasterController getRandomDeadPlayer()
        {
			var deadPlayers = new List<PlayerCharacterMasterController>();
			foreach(var instance in PlayerCharacterMasterController.instances)
			{
				if (instance.master)
				{
					CharacterBody body = instance.master.GetBody();
					if ((!body || !body.healthComponent.alive) && instance.master.inventory.GetItemCount(ItemCatalog.FindItemIndex("ExtraLife")) <= 0)
					{
						deadPlayers.Add(instance);
					}
				}
			};
			if(deadPlayers.Count == 0)
            {
				return null;
            }
			var player = MoreShrines.EvaRng.NextElementUniform<PlayerCharacterMasterController>(deadPlayers);
			return player;
		}

	}


}
