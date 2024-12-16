using HarmonyLib;
using HotbarPlus.Networking;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HotbarPlus
{
    [HarmonyPatch]
    internal static class SaveManager
	{

        [HarmonyPatch(typeof(StartOfRound), "ResetShip")]
        [HarmonyPostfix]
        private static void OnResetShip()
        {
            if (SyncManager.purchasedHotbarSlots > 0)
            {
                SyncManager.purchasedHotbarSlots = 0;
                SyncManager.OnUpdateHotbarSize();
            }

            if (NetworkManager.Singleton.IsServer && SyncManager.purchasableHotbarSlots > 0)
                ResetGameValues();
        }


        [HarmonyPatch(typeof(GameNetworkManager), "SaveGameValues")]
        [HarmonyPostfix]
        private static void OnSaveGameValues()
        {
            if (NetworkManager.Singleton.IsHost && StartOfRound.Instance.inShipPhase)
                SaveGameValues();
        }


        /*[HarmonyPatch(typeof(StartOfRound), "LoadUnlockables")]
        [HarmonyPostfix]
        private static void OnLoadGameValues()
        {
            if (NetworkManager.Singleton.IsServer && SyncManager.isSynced)
                LoadGameValues();
        }*/


        internal static void SaveGameValues()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (SyncManager.purchasableHotbarSlots > 0)
                    Plugin.LogWarning("Saving " + SyncManager.purchasedHotbarSlots + " purchased hotbar slots.");

                ES3.Save<short>("HotbarPlus.PurchasedHotbarSlots", SyncManager.purchasedHotbarSlots, GameNetworkManager.Instance.currentSaveFileName);
            }
        }


        internal static void LoadGameValues()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                short purchasedHotbarSlots = ES3.Load("HotbarPlus.PurchasedHotbarSlots", GameNetworkManager.Instance.currentSaveFileName, (short)0);
                SyncManager.purchasedHotbarSlots = (short)Mathf.Clamp(purchasedHotbarSlots, (short)0, (short)Mathf.Max(SyncManager.purchasableHotbarSlots, 0));

                if (SyncManager.purchasableHotbarSlots > 0)
                    Plugin.LogWarning("Loaded " + SyncManager.purchasedHotbarSlots + " purchased hotbar slots.");
            }
        }


        internal static void ResetGameValues()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                if (SyncManager.purchasableHotbarSlots > 0)
                    Plugin.LogWarning("Resetting game values.");
                ES3.DeleteKey("HotbarPlus.PurchasedHotbarSlots", GameNetworkManager.Instance.currentSaveFileName);
            }
        }
    }
}