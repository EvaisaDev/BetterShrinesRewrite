using System;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using RoR2.UI;

namespace Evaisa.BetterShrines
{
	[RequireComponent(typeof(PurchaseInteraction))]
	public class ShrineImpBehaviour : NetworkBehaviour
	{
		public PurchaseInteraction purchaseInteraction;
		public Color shrineEffectColor;
		public Transform symbolTransform;
		public int timeLeft;

		public static List<ShrineImpInstance> instances = new List<ShrineImpInstance>();

		public ShrineImpInstance instance;

		public ObjectivePanelController.ObjectiveSourceDescriptor sourceDescriptor;
		public List<ObjectivePanelController.ObjectiveSourceDescriptor> sourceDescriptorList;

		public CombatDirector combatDirector;

		public bool waitingForRefresh = false;
		public int purchaseCount = 0;
		public float refreshTimer = 0f;
		public int maxPurchaseCount = 1;
		public int monsterCredit = 200;
		public DirectorCard chosenDirectorCard;

		public class ShrineImpInstance
		{
			public ShrineImpBehaviour shrineBehaviour;
			public bool active;
			public HashSet<CharacterMaster> impMasters;
			public int originalImpCount;
			public int killedImpCount;
			public string impColor;

			public ShrineImpInstance(ShrineImpBehaviour shrineBehaviour, bool active, HashSet<CharacterMaster> impMasters, int originalImpCount, int killedImpCount)
			{
				this.shrineBehaviour = shrineBehaviour;
				this.active = active;
				this.impMasters = impMasters;
				this.originalImpCount = originalImpCount;
				this.killedImpCount = killedImpCount;
			}
		}

		public void Awake()
        {
			purchaseInteraction = GetComponent<PurchaseInteraction>();
			if (NetworkServer.active)
			{
				purchaseInteraction = GetComponent<PurchaseInteraction>();
				combatDirector = GetComponent<CombatDirector>();
				combatDirector.combatSquad.onDefeatedServer += this.OnDefeatedServer;
			}

		}

		private void Start()
		{
			if (NetworkServer.active)
			{
				chosenDirectorCard = combatDirector.SelectMonsterCardForCombatShrine(monsterCredit);
				if (chosenDirectorCard == null)
				{
					Debug.Log("Could not find appropriate spawn card for Combat Shrine");
					purchaseInteraction.SetAvailable(false);
				}
			}
		}

		public void FixedUpdate()
		{
			if (waitingForRefresh)
			{
				refreshTimer -= Time.fixedDeltaTime;
				if (refreshTimer <= 0f && purchaseCount < maxPurchaseCount)
				{
					purchaseInteraction.SetAvailable(true);
					waitingForRefresh = false;
				}
			}
		}

		public void AddShrineStack(Interactor interactor)
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineCombatBehavior::AddShrineStack(RoR2.Interactor)' called on client");
				return;
			}
			waitingForRefresh = true;
			combatDirector.CombatShrineActivation(interactor, monsterCredit, chosenDirectorCard);
			EffectManager.SpawnEffect(Resources.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
			{
				origin = transform.position,
				rotation = Quaternion.identity,
				scale = 1f,
				color = shrineEffectColor
			}, true);
			purchaseCount++;
			refreshTimer = 2f;
			if (this.purchaseCount >= maxPurchaseCount)
			{
				symbolTransform.gameObject.SetActive(false);
			}
		}

		public void OnDefeatedServer()
        {

        }
	}
}
