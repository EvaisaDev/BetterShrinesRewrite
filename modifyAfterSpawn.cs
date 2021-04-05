using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;

namespace Evaisa.BetterShrines
{
    class modifyAfterSpawn : MonoBehaviour
    {
        public void Start()
        {
            var purchaseInteraction = GetComponent<PurchaseInteraction>();
            var onPurchase = purchaseInteraction.onPurchase;

            // onPuchase.SetPersistentListenerState(1, UnityEngine.Events.UnityEventCallState.Off);
            var impBehaviour = GetComponent<ShrineImpBehaviour>();
            var fallenBehaviour = GetComponent<ShrineFallenBehaviour>();
            var combatSquad = GetComponent<CombatSquad>();
            if (impBehaviour != null)
            {
                onPurchase.AddListener((interactor) =>
                {
                    impBehaviour.AddShrineStack(interactor);
                });
                /*combatSquad.onDefeatedServerLogicEvent.AddListener(() =>
                {
                    impBehaviour.OnDefeated();
                });*/
            }
            if (fallenBehaviour != null)
            {
                onPurchase.AddListener((interactor) =>
                {
                    fallenBehaviour.AddShrineStack(interactor);
                });
            }
        }
    }
}
