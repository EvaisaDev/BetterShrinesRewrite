using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using RoR2;
using UnityEngine;

namespace Evaisa.MoreShrines
{
    internal class DebugScript : NetworkBehaviour
    {
		public static Dictionary<NetworkInstanceId, DebugScript> instances = new Dictionary<NetworkInstanceId, DebugScript>();

		[SyncVar]
		public NetworkInstanceId owner;

        internal static void SpawnTheBehavioursOnClients()
        {

            foreach (var client in NetworkUser.instancesList)
            {
				if (!instances.ContainsKey(client.netId))
				{
					if (client.connectionToClient.isReady)
					{
						GameObject go = Instantiate(MoreShrines.debugPrefab);
						go.GetComponent<DebugScript>().owner = client.netId;
						NetworkServer.SpawnWithClientAuthority(go, client.connectionToClient);
						instances.Add(client.netId, go.GetComponent<DebugScript>());
					}
					//Debug.Log("Yo what the hell.");
				}
            }

        }

		void SpawnObjectAtAim(PlayerCharacterMasterController player, SpawnCard card, Vector3 aimOrigin, Vector3 aimDirection)
		{
			var playerMaster = player.GetComponent<CharacterMaster>();

			if (playerMaster.GetBody())
			{
				//Debug.Log("Rawr2");
				CharacterBody localBody = playerMaster.GetBody();

				Ray myRay = new Ray(aimOrigin, aimDirection);

				float maxDistance = 1000f;
				RaycastHit raycastHit;

				if (Util.CharacterRaycast(localBody.gameObject, myRay, out raycastHit, maxDistance, LayerIndex.world.mask | LayerIndex.defaultLayer.mask | LayerIndex.pickups.mask, QueryTriggerInteraction.Collide))
				{

					Vector3 hitPos = raycastHit.point;


					SpawnCard chestCard = card;
					DirectorPlacementRule placementRule = new DirectorPlacementRule();
					placementRule.placementMode = DirectorPlacementRule.PlacementMode.Direct;
					GameObject chest = chestCard.DoSpawn(hitPos, Quaternion.Euler(new Vector3(0f, 0f, 0f)), new DirectorSpawnRequest(chestCard, placementRule, Run.instance.runRNG)).spawnedInstance;
					chest.transform.eulerAngles = new Vector3(0, 0, 0);
				}
			}
		}


		[Command]
		void CmdSpawnImpShrine(NetworkInstanceId netID, Vector3 aimOrigin, Vector3 aimDirection)
		{

			if (NetworkServer.objects.ContainsKey(netID))
			{
				PlayerCharacterMasterController localPlayer = NetworkServer.objects[netID].gameObject.GetComponent<PlayerCharacterMasterController>();
				SpawnObjectAtAim(localPlayer, MoreShrines.impShrineInteractableInfo.directorCard.spawnCard, aimOrigin, aimDirection);
			}
		}
		[Command]
		void CmdSpawnFallenShrine(NetworkInstanceId netID, Vector3 aimOrigin, Vector3 aimDirection)
		{
			if (NetworkServer.objects.ContainsKey(netID))
			{
				PlayerCharacterMasterController localPlayer = NetworkServer.objects[netID].gameObject.GetComponent<PlayerCharacterMasterController>();
				SpawnObjectAtAim(localPlayer, MoreShrines.fallenShrineInteractableInfo.directorCard.spawnCard, aimOrigin, aimDirection);
			}
		}
		[Command]
		void CmdSpawnDisorderShrine(NetworkInstanceId netID, Vector3 aimOrigin, Vector3 aimDirection)
		{
			if (NetworkServer.objects.ContainsKey(netID))
			{
				PlayerCharacterMasterController localPlayer = NetworkServer.objects[netID].gameObject.GetComponent<PlayerCharacterMasterController>();
				SpawnObjectAtAim(localPlayer, MoreShrines.disorderShrineInteractableInfo.directorCard.spawnCard, aimOrigin, aimDirection);
			}
		}
		[Command]
		void CmdSpawnHeresyShrine(NetworkInstanceId netID, Vector3 aimOrigin, Vector3 aimDirection)
		{
			if (NetworkServer.objects.ContainsKey(netID))
			{
				PlayerCharacterMasterController localPlayer = NetworkServer.objects[netID].gameObject.GetComponent<PlayerCharacterMasterController>();
				SpawnObjectAtAim(localPlayer, MoreShrines.heresyShrineInteractableInfo.directorCard.spawnCard, aimOrigin, aimDirection);
			}
		}
		[Command]
		void CmdGiveMoney(NetworkInstanceId netID)
		{
			if (NetworkServer.objects.ContainsKey(netID))
			{
				PlayerCharacterMasterController localPlayer = NetworkServer.objects[netID].gameObject.GetComponent<PlayerCharacterMasterController>();

				localPlayer.GetComponent<CharacterMaster>().GiveMoney(1000);
			}
		}
		[Command]
		void CmdGiveLunar(NetworkInstanceId netID)
		{
			if (NetworkServer.objects.ContainsKey(netID))
			{
				PlayerCharacterMasterController localPlayer = NetworkServer.objects[netID].gameObject.GetComponent<PlayerCharacterMasterController>();

				localPlayer.networkUser.AwardLunarCoins(1);
			}
		}
		[Command]
		void CmdSpawnChest(NetworkInstanceId netID, Vector3 aimOrigin, Vector3 aimDirection)
		{
			if (NetworkServer.objects.ContainsKey(netID))
			{
				PlayerCharacterMasterController localPlayer = NetworkServer.objects[netID].gameObject.GetComponent<PlayerCharacterMasterController>();
				SpawnObjectAtAim(localPlayer, Resources.Load<SpawnCard>("spawncards/interactablespawncard/iscChest1"), aimOrigin, aimDirection);
			}
		}
		[Command]
		void CmdSpawnShrineCombat(NetworkInstanceId netID, Vector3 aimOrigin, Vector3 aimDirection)
		{
			if (NetworkServer.objects.ContainsKey(netID))
			{
				PlayerCharacterMasterController localPlayer = NetworkServer.objects[netID].gameObject.GetComponent<PlayerCharacterMasterController>();
				SpawnObjectAtAim(localPlayer, Resources.Load<SpawnCard>("spawncards/interactablespawncard/iscShrineCombat"), aimOrigin, aimDirection);
			}
		}
		[Command]
		void CmdSpawnWispShrine(NetworkInstanceId netID, Vector3 aimOrigin, Vector3 aimDirection)
		{
			if (NetworkServer.objects.ContainsKey(netID))
			{
				PlayerCharacterMasterController localPlayer = NetworkServer.objects[netID].gameObject.GetComponent<PlayerCharacterMasterController>();
				SpawnObjectAtAim(localPlayer, MoreShrines.wispShrineInteractableInfo.directorCard.spawnCard, aimOrigin, aimDirection);
			}
		}

		/*
		void Update()
		{
			if (!NetworkServer.active) { 
				if (PlayerCharacterMasterController.instances.Count > 0)
				{
					PlayerCharacterMasterController localPlayer = PlayerCharacterMasterController.instances[0];

					if (localPlayer.netId == owner)
					{


						if (Input.GetKeyDown(KeyCode.F1))
						{
							CmdSpawnImpShrine(localPlayer.netId);
						}
						if (Input.GetKeyDown(KeyCode.F2))
						{
							CmdSpawnFallenShrine(localPlayer.netId);
						}
						if (Input.GetKeyDown(KeyCode.F3))
						{
							CmdSpawnDisorderShrine(localPlayer.netId);
						}
						if (Input.GetKeyDown(KeyCode.F4))
						{
							CmdSpawnHeresyShrine(localPlayer.netId);
						}
						if (Input.GetKeyDown(KeyCode.F5))
						{
							CmdGiveMoney(localPlayer.netId);
						}
						if (Input.GetKeyDown(KeyCode.F6))
						{
							CmdGiveLunar(localPlayer.netId);
						}
						if (Input.GetKeyDown(KeyCode.F7))
						{
							CmdSpawnChest(localPlayer.netId);
						}
					}
				}
			}
		}
		*/
		void Update()
		{
			if (PlayerCharacterMasterController.instances.Count > 0)
			{
				PlayerCharacterMasterController localPlayer = PlayerCharacterMasterController.instances[0];




				if (Input.GetKeyDown(KeyCode.F1))
				{
					CmdSpawnImpShrine(localPlayer.netId, localPlayer.bodyInputs.aimOrigin, localPlayer.bodyInputs.aimDirection);
				}
				if (Input.GetKeyDown(KeyCode.F2))
				{
					CmdSpawnFallenShrine(localPlayer.netId, localPlayer.bodyInputs.aimOrigin, localPlayer.bodyInputs.aimDirection);
				}
				if (Input.GetKeyDown(KeyCode.F3))
				{
					CmdSpawnDisorderShrine(localPlayer.netId, localPlayer.bodyInputs.aimOrigin, localPlayer.bodyInputs.aimDirection);
				}
				if (Input.GetKeyDown(KeyCode.F4))
				{
					CmdSpawnHeresyShrine(localPlayer.netId, localPlayer.bodyInputs.aimOrigin, localPlayer.bodyInputs.aimDirection);
				}
				if (Input.GetKeyDown(KeyCode.F5))
				{
					CmdGiveMoney(localPlayer.netId);
				}
				if (Input.GetKeyDown(KeyCode.F6))
				{
					CmdGiveLunar(localPlayer.netId);
				}
				if (Input.GetKeyDown(KeyCode.F7))
				{
					CmdSpawnChest(localPlayer.netId, localPlayer.bodyInputs.aimOrigin, localPlayer.bodyInputs.aimDirection);
				}
                if (Input.GetKeyDown(KeyCode.F8))
                {
					CmdSpawnWispShrine(localPlayer.netId, localPlayer.bodyInputs.aimOrigin, localPlayer.bodyInputs.aimDirection);

				}
				if (Input.GetKeyDown(KeyCode.F9))
				{
					CmdSpawnShrineCombat(localPlayer.netId, localPlayer.bodyInputs.aimOrigin, localPlayer.bodyInputs.aimDirection);

				}

			}
		}

		[Command]
		private void CmdTest(int arg1, int arg2)
		{
			Debug.LogWarning("firing on server : " + arg1 + " " + arg2);
		}

	}
}
