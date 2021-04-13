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
    public class ShrineHeresyBehaviour : NetworkBehaviour
    {
        public Transform symbolTransform;
        public Color shrineEffectColor;

        public PurchaseInteraction purchaseInteraction;
        public PickupIndex dropPickup = PickupIndex.none;

        public List<ItemDef> possiblePicks = new List<ItemDef>();

        public void Awake()
        {
            purchaseInteraction = GetComponent<PurchaseInteraction>();
            purchaseInteraction.onPurchase.AddListener((interactor) =>
            {
                AddShrineStack(interactor);
            });
            purchaseInteraction.costType = CostTypes.getCostTypeIndex(MoreShrines.costTypeDefShrineHeresy);

            possiblePicks.Add(RoR2Content.Items.LunarPrimaryReplacement);
            possiblePicks.Add(RoR2Content.Items.LunarSecondaryReplacement);
            possiblePicks.Add(RoR2Content.Items.LunarSpecialReplacement);
            possiblePicks.Add(RoR2Content.Items.LunarUtilityReplacement);
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
                Debug.LogWarning("[Server] function 'System.Void RoR2.TeleporterInteraction::AddShrineStack()' called on client");
                return;
            }

            var characterBody = interactor.GetComponent<CharacterBody>();
            if (characterBody && characterBody.inventory)
            {
                if(characterBody.inventory.GetItemCount(RoR2Content.Items.LunarPrimaryReplacement.itemIndex) > 0)
                {
                    possiblePicks.Remove(RoR2Content.Items.LunarPrimaryReplacement);
                }
                else if(characterBody.inventory.GetItemCount(RoR2Content.Items.LunarSecondaryReplacement.itemIndex) > 0)
                {
                    possiblePicks.Remove(RoR2Content.Items.LunarSecondaryReplacement);
                }
                else if (characterBody.inventory.GetItemCount(RoR2Content.Items.LunarSpecialReplacement.itemIndex) > 0)
                {
                    possiblePicks.Remove(RoR2Content.Items.LunarSpecialReplacement);
                }
                else if (characterBody.inventory.GetItemCount(RoR2Content.Items.LunarUtilityReplacement.itemIndex) > 0)
                {
                    possiblePicks.Remove(RoR2Content.Items.LunarUtilityReplacement);
                }
                Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                {
                    subjectAsCharacterBody = characterBody,
                    baseToken = "SHRINE_HERESY_USE_MESSAGE"
                });
            }

            dropPickup = PickupCatalog.itemIndexToPickupIndex[(int)possiblePicks[UnityEngine.Random.Range(0, possiblePicks.Count)].itemIndex];

            if (this.dropPickup == PickupIndex.none)
            {
                return;
            }
            PickupDropletController.CreatePickupDroplet(this.dropPickup, this.symbolTransform.position + Vector3.up * 1.5f, Vector3.up * 20f + symbolTransform.forward * 2f);

            EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
            {
                origin = transform.position,
                rotation = Quaternion.identity,
                scale = 1f,
                color = shrineEffectColor
            }, true);
            symbolTransform.gameObject.SetActive(false);
        }

    }


}
