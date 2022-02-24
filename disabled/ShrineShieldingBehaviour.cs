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
    public class ShrineShieldingBehaviour : NetworkBehaviour
    {
        public Transform symbolTransform;
        public Color shrineEffectColor;

        public PurchaseInteraction purchaseInteraction;

        public static Dictionary<CharacterBody, float> shielding = new Dictionary<CharacterBody, float>();

        public static float currentShielding = 0f;

        public void Awake()
        {
            purchaseInteraction = GetComponent<PurchaseInteraction>();
            purchaseInteraction.onPurchase.AddListener((interactor) =>
            {
                AddShrineStack(interactor);
            });
        }

        [ClientRpc]
        public void RpcAddShrineStackClient(Interactor interactor)
        {
            var characterBody = interactor.GetComponent<CharacterBody>();
            if (characterBody && characterBody.inventory)
            {
                currentShielding = currentShielding + (characterBody.healthComponent.health * 2);
                symbolTransform.gameObject.SetActive(false);
            }
        }


        public void AddShrineStack(Interactor interactor)
        {
            RpcAddShrineStackClient(interactor);

            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void RoR2.TeleporterInteraction::AddShrineStack()' called on client");
                return;
            }

            var characterBody = interactor.GetComponent<CharacterBody>();
            if (characterBody && characterBody.inventory)
            {
                Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
                {
                    subjectAsCharacterBody = characterBody,
                    baseToken = "SHRINE_SHIELDING_USE_MESSAGE"
                });
                if (!shielding.ContainsKey(characterBody))
                {
                    shielding[characterBody] = (characterBody.healthComponent.health * 2);
                }
                else
                {
                    shielding[characterBody] = shielding[characterBody] + (characterBody.healthComponent.health * 2);
                }
                //characterBody.barrier
                //characterBody.healthComponent.AddBarrier(characterBody.healthComponent.barrier + (characterBody.healthComponent.health * 10));
            }

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
