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
            var onPuchase = purchaseInteraction.onPurchase;

            // onPuchase.SetPersistentListenerState(1, UnityEngine.Events.UnityEventCallState.Off);
            var fallenBehaviour = GetComponent<ShrineFallenBehavior>();
            if(fallenBehaviour != null)
            {
                onPuchase.AddListener((interactor) =>
                {
                    fallenBehaviour.AddShrineStack(interactor);
                });
            }
        }
    }
}
