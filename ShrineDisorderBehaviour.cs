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
	public class ShrineDisorderBehaviour : NetworkBehaviour
	{
		public Transform symbolTransform;
		public Transform modelBase;
		public Color shrineEffectColor;

		public PurchaseInteraction purchaseInteraction;

        private ItemTier[] tiersToCheck = new[] {
            ItemTier.Tier1,
            ItemTier.Tier2,
            ItemTier.Tier3,
            ItemTier.Lunar,
            ItemTier.Boss,
        };

        public void Awake()
        {
			purchaseInteraction = GetComponent<PurchaseInteraction>();
            purchaseInteraction.costType = CostTypes.getCostTypeIndex(MoreShrines.costTypeDefShrineDisorder);
            purchaseInteraction.cost = 1;
            purchaseInteraction.Networkcost = 1;
			purchaseInteraction.onPurchase.AddListener((interactor) =>
			{
				AddShrineStack(interactor);
			});

		}

		[ClientRpc]
		public void RpcAddShrineStackClient()
		{
			symbolTransform.gameObject.SetActive(false);
			//modelBase.GetComponent<Animator>().Play("stonesfalling");
			foreach (Rigidbody rigidbody in modelBase.transform.GetComponentsInChildren<Rigidbody>())
			{
				rigidbody.isKinematic = false;
			}
		}


		public void AddShrineStack(Interactor interactor)
		{
			RpcAddShrineStackClient();

            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void RoR2.TeleporterInteraction::AddShrineStack()' called on client");
                return;
            }

            var characterBody = interactor.GetComponent<CharacterBody>();
            if (characterBody && characterBody.inventory)
            {
                foreach (ItemTier tier in tiersToCheck)
                {
                    FlattenInventory(characterBody.inventory, tier);
                }
                Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                {
                    subjectAsCharacterBody = characterBody,
                    baseToken = "SHRINE_DISORDER_USE_MESSAGE"
                });
            }
            EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
            {
                origin = transform.position,
                rotation = Quaternion.identity,
                scale = 1f,
                color = shrineEffectColor
            }, true);
        }

        public void FlattenInventory(Inventory inventory, ItemTier itemTier)
        {
            var itemDefs = Utils.ItemDefsFromTier(itemTier, true);
            int[] itemCounts = new int[itemDefs.Length];
            int lowestCount = int.MaxValue;
            foreach (var itemDef in itemDefs)
            {
                lowestCount = Math.Min(inventory.GetItemCount(itemDef), lowestCount);
                if (lowestCount == 0) break;
            }
            var newCount = lowestCount + 1;
            var itemBudget = 0;
            var itemChoices = new WeightedSelection<ItemDef>();
            foreach (var itemDef in itemDefs)
            {
                var count = inventory.GetItemCount(itemDef);
                if (count > newCount)
                {
                    var toRemove = count - newCount;
                    itemBudget += toRemove;
                    inventory.RemoveItem(itemDef, toRemove);
                }
                else
                {
                    itemChoices.AddChoice(itemDef, 1);
                }
            }

            int budgetForAll = 0;

            while (itemBudget > 0)
            {
                if (itemChoices.Count > 0)
                {
                    itemBudget--;
                    var randomIndex = itemChoices.EvaluteToChoiceIndex(UnityEngine.Random.value);

                    itemCounts[Array.IndexOf(itemDefs, itemChoices.GetChoice(randomIndex).value)]++;
                    itemChoices.RemoveChoice(randomIndex);
                }
                else
                {

                    budgetForAll = itemBudget / itemDefs.Length;
                    itemBudget -= budgetForAll * itemDefs.Length;
                    foreach (var itemDef in itemDefs)
                    {
                        itemChoices.AddChoice(itemDef, 1);
                    }

                }
            }
            for (int i = 0; i < itemDefs.Length; i++)
            {
                var count = budgetForAll + itemCounts[i];
                if (count > 0)
                {
                    inventory.GiveItem(itemDefs[i], count);
                }
            }
        }

    }


}
