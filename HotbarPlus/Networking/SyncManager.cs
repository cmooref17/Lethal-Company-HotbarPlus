using GameNetcodeStuff;
using HarmonyLib;
using System.Reflection;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using HotbarPlus.Patches;
using HotbarPlus.Config;
using HotbarPlus.Input;


namespace HotbarPlus.Networking
{
    [HarmonyPatch]
    public class SyncManager
    {
        static PlayerControllerB localPlayerController { get { return StartOfRound.Instance?.localPlayerController; } }
        public static int hotbarSize { get; private set; }
        public static bool isSynced = false;


        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        public static void ResetValues()
        {
            isSynced = false;
            hotbarSize = 4;
        }


        [HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
        [HarmonyPostfix]
        public static void Init()
        {
            isSynced = false;
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("HotbarPlus.OnRequestSyncServerRpc", OnRequestSyncServerRpc);
                hotbarSize = ConfigSettings.hotbarSize.Value;
                OnSyncedWithServer();
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("HotbarPlus.OnRequestSyncClientRpc", OnRequestSyncClientRpc);
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
            var writer = new FastBufferWriter(sizeof(int), Allocator.Temp);
            writer.WriteValue(hotbarSize);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("HotbarPlus.OnRequestSyncClientRpc", clientId, writer);
        }


        private static void OnRequestSyncClientRpc(ulong clientId, FastBufferReader reader)
        {
            if (!NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
                return;

            reader.ReadValue(out int newHotbarSize);
            Plugin.Log("Received sync from Server. Hotbar size: " + newHotbarSize);
            hotbarSize = newHotbarSize;
            OnSyncedWithServer();
        }


        private static void OnSyncedWithServer()
        {
            isSynced = true;
            PlayerPatcher.ResizeInventory();
            HUDPatcher.ResizeHotbarSlotsHUD();
            Keybinds.OnSetHotbarSize();
        }




        private static void SendHotbarSlotChange(int hotbarSlot)
        {
            if (!NetworkManager.Singleton.IsClient || hotbarSlot == localPlayerController.currentItemSlot)
                return;

            Plugin.Log("Sending hotbar swap slot: " + hotbarSlot);

            int swapSlots = localPlayerController.currentItemSlot - hotbarSlot;
            bool forward = swapSlots > 0;

            for (int i = 0; i < Mathf.Abs(swapSlots); i++)
            {
                MethodInfo method = localPlayerController.GetType().GetMethod("SwitchItemSlotsServerRpc", BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(localPlayerController, new object[] { forward });
            }
        }



        
        public static void SwapHotbarSlot(int hotbarIndex)
        {
            if (hotbarIndex >= hotbarSize)
                return;

            SendHotbarSlotChange(hotbarIndex);
            CallSwitchToItemSlotMethod(localPlayerController, hotbarIndex);
        }


        public static void CallSwitchToItemSlotMethod(PlayerControllerB playerController, int hotbarIndex)
        {
            if (hotbarIndex < 0 || hotbarIndex >= hotbarSize || playerController.currentItemSlot == hotbarIndex)
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
    }
}
