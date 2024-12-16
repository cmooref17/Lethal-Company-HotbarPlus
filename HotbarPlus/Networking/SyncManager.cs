using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using HotbarPlus.Patches;
using HotbarPlus.Config;
using HotbarPlus.Input;
using System.Collections.Generic;


namespace HotbarPlus.Networking
{
    [HarmonyPatch]
    public class SyncManager
    {
        private static PlayerControllerB localPlayerController { get { return StartOfRound.Instance?.localPlayerController; } }
        public static short hotbarSize;
        public static short purchasableHotbarSlots;
        public static short purchasableHotbarSlotsPrice;
        public static short purchasableHotbarSlotsPriceIncrease;
        public static short purchasedHotbarSlots;
        public static short currentHotbarSize { get { return (short)(hotbarSize + purchasedHotbarSlots); } }

        public static bool isSynced = false;


        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        private static void ResetValues()
        {
            isSynced = false;
            hotbarSize = (short)(int)ConfigSettings.hotbarSizeConfig.DefaultValue;
            purchasableHotbarSlots = (short)(int)ConfigSettings.purchasableHotbarSlotsConfig.DefaultValue;
            purchasableHotbarSlotsPrice = (short)(int)ConfigSettings.purchasableHotbarSlotsPriceConfig.DefaultValue;
            purchasableHotbarSlotsPriceIncrease = (short)(int)ConfigSettings.purchasableHotbarSlotsPriceIncreaseConfig.DefaultValue;
            purchasedHotbarSlots = 0;
        }


        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        private static void Init()
        {
            isSynced = false;
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("HotbarPlus.OnRequestSyncServerRpc", OnRequestSyncServerRpc);
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("HotbarPlus.OnPurchaseHotbarSlotServerRpc", OnPurchaseHotbarSlotServerRpc);
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("HotbarPlus.RequestSyncHeldObjectsServerRpc", RequestSyncHeldObjectsServerRpc);
                hotbarSize = (short)Mathf.Max(ConfigSettings.hotbarSizeConfig.Value, 0);
                purchasableHotbarSlots = (short)Mathf.Max(ConfigSettings.purchasableHotbarSlotsConfig.Value, 0);
                purchasableHotbarSlotsPrice = (short)Mathf.Max(ConfigSettings.purchasableHotbarSlotsPriceConfig.Value, 1);
                purchasableHotbarSlotsPriceIncrease = (short)Mathf.Max(ConfigSettings.purchasableHotbarSlotsPriceIncreaseConfig.Value, 0);

                SaveManager.LoadGameValues();
                OnSyncedWithServer();
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("HotbarPlus.OnRequestSyncClientRpc", OnRequestSyncClientRpc);
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("HotbarPlus.OnPurchaseHotbarSlotClientRpc", OnPurchaseHotbarSlotClientRpc);
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("HotbarPlus.RequestSyncHeldObjectsClientRpc", RequestSyncHeldObjectsClientRpc);
                RequestSyncWithServer();
            }
        }


        private static void RequestSyncWithServer()
        {
            Plugin.Log("Requesting sync with server.");
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("HotbarPlus.OnRequestSyncServerRpc", NetworkManager.ServerClientId, new FastBufferWriter(0, Allocator.Temp));
        }


        private static void OnRequestSyncServerRpc(ulong clientId, FastBufferReader reader)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            Plugin.Log("Received request for sync from Client: " + clientId);
            var writer = new FastBufferWriter(sizeof(short) * 5, Allocator.Temp);

            writer.WriteValue(hotbarSize);
            writer.WriteValue(purchasableHotbarSlots);
            writer.WriteValue(purchasableHotbarSlotsPrice);
            writer.WriteValue(purchasableHotbarSlotsPriceIncrease);
            writer.WriteValue(purchasedHotbarSlots);

            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("HotbarPlus.OnRequestSyncClientRpc", clientId, writer);
        }


        private static void OnRequestSyncClientRpc(ulong clientId, FastBufferReader reader)
        {
            if (!NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
                return;

            reader.ReadValue(out hotbarSize);
            reader.ReadValue(out purchasableHotbarSlots);
            reader.ReadValue(out purchasableHotbarSlotsPrice);
            reader.ReadValue(out purchasableHotbarSlotsPriceIncrease);
            reader.ReadValue(out purchasedHotbarSlots);

            Plugin.Log("Received sync from Server. Hotbar size: " + hotbarSize + " PurchasableHotbarSlots: " + purchasableHotbarSlots + " HotbarSlotsPrice: " + purchasableHotbarSlotsPrice + " HotbarSlotsPriceIncrease: " + purchasableHotbarSlotsPriceIncrease + " CurrentPurchasedHotbarSlots: " + purchasedHotbarSlots);
            OnSyncedWithServer();
        }


        private static void OnSyncedWithServer()
        {
            isSynced = true;
            PlayerPatcher.ResizeInventory();
            HUDPatcher.ResizeHotbarSlotsHUD();
            Keybinds.OnSetHotbarSize();
            RequestSyncHeldObjects();
        }


        internal static void OnUpdateHotbarSize()
        {
            Plugin.Log("Finished receiving OnUpdatePurchasedHotbarSlots update. New purchased hotbar slots: " + purchasedHotbarSlots);
            PlayerPatcher.ResizeInventory();
            HUDPatcher.ResizeHotbarSlotsHUD();
            Keybinds.OnSetHotbarSize();
        }





        private static void RequestSyncHeldObjects()
        {
            if (!NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer/* || requestedSyncHeldObjects*/)
                return;

            Plugin.Log("Requesting sync held objects from server.");
            //requestedSyncHeldObjects = true;
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("HotbarPlus.RequestSyncHeldObjectsServerRpc", NetworkManager.ServerClientId, new FastBufferWriter(0, Allocator.Temp));
        }


        // ServerRpc
        private static void RequestSyncHeldObjectsServerRpc(ulong clientId, FastBufferReader reader)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            var clientIds = new List<ushort>();
            var selectedItemSlots = new List<short>();
            var inventoryIndexes = new Dictionary<ushort, List<short>>();
            var heldItemNetworkIds = new Dictionary<ushort, List<ulong>>();

            int syncBufferSize = sizeof(short);

            foreach (var playerController in StartOfRound.Instance.allPlayerScripts)
            {
                ushort syncClientId = (ushort)playerController.actualClientId;
                ushort syncPlayerId = (ushort)playerController.playerClientId;
                if (syncClientId == clientId)
                    continue;

                if (syncClientId == 0 && playerController != localPlayerController)
                    continue;

                for (int inventoryIndex = 4; inventoryIndex < currentHotbarSize; inventoryIndex++)
                {
                    var item = playerController.ItemSlots[inventoryIndex];
                    if (item != null || inventoryIndex == playerController.currentItemSlot)
                    {
                        if (!inventoryIndexes.ContainsKey(syncPlayerId))
                        {
                            inventoryIndexes.Add(syncPlayerId, new List<short>());
                            heldItemNetworkIds.Add(syncPlayerId, new List<ulong>());
                        }
                        if (item != null)
                        {
                            inventoryIndexes[syncPlayerId].Add((short)inventoryIndex);
                            heldItemNetworkIds[syncPlayerId].Add(item.NetworkObjectId);
                        }
                    }
                }

                if (inventoryIndexes.ContainsKey(syncPlayerId) && (inventoryIndexes[syncPlayerId].Count > 0 || (playerController.currentItemSlot >= 4 && playerController.currentItemSlot < currentHotbarSize)))
                {
                    syncBufferSize += sizeof(ushort); // syncClientId
                    syncBufferSize += sizeof(short); // selected item slot
                    syncBufferSize += sizeof(short); // num held items in extra slots
                    syncBufferSize += (sizeof(short) + sizeof(ulong)) * inventoryIndexes[syncPlayerId].Count;
                    clientIds.Add(syncPlayerId);
                    selectedItemSlots.Add((short)playerController.currentItemSlot);
                }
            }

            Plugin.Log("Receiving sync held objects request from client with id: " + clientId + ". " + clientIds.Count + " players are currently holding items in extra item slots.");

            var writer = new FastBufferWriter(syncBufferSize, Allocator.Temp);
            writer.WriteValue((short)clientIds.Count);

            for (int i = 0; i < clientIds.Count; i++)
            {
                ushort syncClientId = clientIds[i];
                short selectedItemSlot = selectedItemSlots[i];
                short numElements = (short)inventoryIndexes[syncClientId].Count;

                writer.WriteValue(syncClientId);
                writer.WriteValue(selectedItemSlot);
                writer.WriteValue(numElements);

                for (int j = 0; j < numElements; j++)
                {
                    short inventoryIndex = inventoryIndexes[syncClientId][j];
                    ulong networkObjectId = heldItemNetworkIds[syncClientId][j];

                    writer.WriteValue(inventoryIndex);
                    writer.WriteValue(networkObjectId);
                }
            }
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("HotbarPlus.RequestSyncHeldObjectsClientRpc", clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
        }


        // ClientRpc
        private static void RequestSyncHeldObjectsClientRpc(ulong clientId, FastBufferReader reader)
        {
            if (!NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
                return;

            reader.ReadValue(out short numPlayersToSyncItems);
            Plugin.Log("Receiving sync held objects from server. Number of players already holding items in extra hotbar slots: " + numPlayersToSyncItems);

            for (int i = 0; i < numPlayersToSyncItems; i++)
            {
                reader.ReadValue(out ushort syncPlayerId);
                reader.ReadValue(out short currentlySelectedItemSlot);
                reader.ReadValue(out short numHeldExtraItems);

                var playerController = GetPlayerControllerByPlayerId(syncPlayerId);

                if (playerController.currentItemSlot >= 0 && playerController.currentItemSlot < playerController.ItemSlots.Length && playerController.currentItemSlot != currentlySelectedItemSlot && currentlySelectedItemSlot >= 4)
                {
                    var currentlyHeldObject = playerController.ItemSlots[playerController.currentItemSlot];
                    if (currentlyHeldObject != null)
                        currentlyHeldObject.PocketItem();
                    playerController.currentItemSlot = currentlySelectedItemSlot;
                }

                for (int j = 0; j < numHeldExtraItems; j++)
                {
                    reader.ReadValue(out short inventoryIndex);
                    reader.ReadValue(out ulong networkObjectId);
                    var grabbableObject = GetGrabbableObjectByNetworkId(networkObjectId);

                    if (grabbableObject != null && playerController && inventoryIndex >= 0)
                    {
                        grabbableObject.isHeld = true;
                        playerController.ItemSlots[inventoryIndex] = grabbableObject;
                        grabbableObject.parentObject = playerController.serverItemHolder;
                        grabbableObject.playerHeldBy = playerController;
                        bool currentlySelected = currentlySelectedItemSlot == inventoryIndex;

                        grabbableObject.EnablePhysics(false);

                        if (currentlySelected)
                        {
                            grabbableObject.EquipItem();
                            playerController.currentlyHeldObjectServer = grabbableObject;
                            playerController.isHoldingObject = true;
                            playerController.twoHanded = grabbableObject.itemProperties.twoHanded;
                            playerController.twoHandedAnimation = grabbableObject.itemProperties.twoHandedAnimation;
                            playerController.currentItemSlot = inventoryIndex;
                        }
                        else
                            grabbableObject.PocketItem();
                    }
                }
            }
        }



        public static void SendPurchaseHotbarSlotToServer(int newAdditionalSlots)
        {
            var writer = new FastBufferWriter(sizeof(short), Allocator.Temp);
            Plugin.Log("Sending purchase hotbar slot update to server. New additional hotbar slots: " + newAdditionalSlots);
            writer.WriteValue((short)newAdditionalSlots);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("HotbarPlus.OnPurchaseHotbarSlotServerRpc", NetworkManager.ServerClientId, writer);
        }


        // ServerRpc
        private static void OnPurchaseHotbarSlotServerRpc(ulong clientId, FastBufferReader reader)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            reader.ReadValue(out purchasedHotbarSlots);

            if (NetworkManager.Singleton.IsClient)
            {
                if (clientId != localPlayerController.actualClientId)
                    Plugin.Log("Receiving on purchase additional hotbar slot update from client: " + clientId + ". New hotbar slots purchased: " + purchasedHotbarSlots);
                OnUpdateHotbarSize();
            }
            SendNewAdditionalHotbarSlotsToClients(purchasedHotbarSlots);
        }


        public static void SendNewAdditionalHotbarSlotsToClients(int newAdditionalSlots)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            var writer = new FastBufferWriter(sizeof(short), Allocator.Temp);
            writer.WriteValue((short)newAdditionalSlots);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("HotbarPlus.OnPurchaseHotbarSlotClientRpc", writer);
        }


        // ClientRpc
        private static void OnPurchaseHotbarSlotClientRpc(ulong clientId, FastBufferReader reader)
        {
            if (!NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
                return;

            reader.ReadValue(out purchasedHotbarSlots);

            Plugin.Log("Receiving on purchase additional hotbar slot update from server. New hotbar slots purchased: " + purchasedHotbarSlots);
            OnUpdateHotbarSize();
        }

        



        private static void SendHotbarSlotChange(int hotbarSlot)
        {
            if (!NetworkManager.Singleton.IsClient || hotbarSlot == localPlayerController.currentItemSlot)
                return;

            Plugin.Log("Sending hotbar swap slot: " + hotbarSlot);

            int swapSlots = hotbarSlot - localPlayerController.currentItemSlot;
            bool forward = swapSlots > 0;

            for (int i = 0; i < Mathf.Abs(swapSlots); i++)
            {
                MethodInfo method = localPlayerController.GetType().GetMethod("SwitchItemSlotsServerRpc", BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(localPlayerController, new object[] { forward });
            }
        }

        
        public static void SwapHotbarSlot(int hotbarIndex)
        {
            if (hotbarIndex >= currentHotbarSize)
                return;

            SendHotbarSlotChange(hotbarIndex);
            CallSwitchToItemSlotMethod(localPlayerController, hotbarIndex);
        }


        public static void CallSwitchToItemSlotMethod(PlayerControllerB playerController, int hotbarIndex)
        {
            if (hotbarIndex < 0 || hotbarIndex >= currentHotbarSize || playerController.currentItemSlot == hotbarIndex)
                return;

            if (playerController == localPlayerController)
            {
                ShipBuildModeManager.Instance.CancelBuildMode(true);
                playerController.playerBodyAnimator.SetBool("GrabValidated", value: false);
            }
            MethodInfo method = playerController.GetType().GetMethod("SwitchToItemSlot", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(playerController, new object[] { hotbarIndex, null });
            PlayerPatcher.SetTimeSinceSwitchingSlots(playerController, 0);
            if (playerController.currentlyHeldObjectServer != null)
                playerController.currentlyHeldObjectServer.gameObject.GetComponent<AudioSource>().PlayOneShot(playerController.currentlyHeldObjectServer.itemProperties.grabSFX, 0.6f);
        }


        internal static PlayerControllerB GetPlayerControllerByClientId(ulong clientId)
        {
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                var playerController = StartOfRound.Instance.allPlayerScripts[i];
                if (playerController.actualClientId == clientId)
                    return playerController;
            }
            return null;
        }


        internal static PlayerControllerB GetPlayerControllerByPlayerId(ulong playerId)
        {
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
            {
                var playerController = StartOfRound.Instance.allPlayerScripts[i];
                if (playerController.playerClientId == playerId)
                    return playerController;
            }
            return null;
        }


        internal static GrabbableObject GetGrabbableObjectByNetworkId(ulong networkObjectId)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
                return networkObject.GetComponentInChildren<GrabbableObject>();
            return null;
        }
    }
}
