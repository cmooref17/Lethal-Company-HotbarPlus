using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotbarPlus.Config;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using HotbarPlus.Networking;
using HotbarPlus.Compatibility;


namespace HotbarPlus.Patches
{
    [HarmonyPatch]
    public class QuickDrop
    {
        public static PlayerControllerB localPlayerController { get { return StartOfRound.Instance?.localPlayerController; } }
        public static float timeDroppedItem;
        public static bool droppingItem = false;
        private static float timeLoggedPreventedItemSwap = 0;
        private static HashSet<int> checkedSlots = new HashSet<int>();

        [HarmonyPatch(typeof(PlayerControllerB), "DiscardHeldObject")]
        [HarmonyPostfix]
        private static void PerformQuickDiscard(PlayerControllerB __instance)
        {
            if (droppingItem || __instance != localPlayerController || !ConfigSettings.useItemQuickDropConfig.Value || !SyncManager.isSynced)
                return;

            if (ReservedItemSlots_Compat.Enabled && ReservedItemSlots_Compat.IsItemSlotReserved(localPlayerController.currentItemSlot))
                return;

            checkedSlots.Clear();
            int nextIndex = -1;
            float currentPriority = 0;
            for (int i = PlayerPatcher.CallGetNextItemSlot(__instance, true, __instance.currentItemSlot); i != __instance.currentItemSlot; i = PlayerPatcher.CallGetNextItemSlot(__instance, true, i))
            {
                if (checkedSlots.Contains(i))
                    break;

                checkedSlots.Add(i);
                GrabbableObject grabbable = __instance.ItemSlots[i];
                if (grabbable != null)
                {
                    float priority = 0;
                    if (grabbable.itemProperties.isScrap)
                    {
                        priority++;
                        if (!grabbable.itemProperties.isDefensiveWeapon)
                            priority++;
                    }
                    priority = (priority * 100000.0f) + grabbable.itemProperties.weight;
                    if (priority > currentPriority)
                    {
                        currentPriority = priority;
                        nextIndex = i;
                    }
                }
            }

            if (nextIndex >= 0 && nextIndex < __instance.ItemSlots.Length && nextIndex != __instance.currentItemSlot)
            {
                Plugin.Log("On discard item. Auto swapping to held item at slot: " + nextIndex + ". Prev slot: " + __instance.currentItemSlot);
                droppingItem = true;
                localPlayerController.playerBodyAnimator.SetBool("cancelGrab", false);
                PlayerPatcher.SetTimeSinceSwitchingSlots(__instance, 0);
                __instance.playerBodyAnimator.ResetTrigger("SwitchHoldAnimation");
                __instance.StartCoroutine(SwitchToItemSlotAfterDelay(__instance, nextIndex));
            }
        }


        private static IEnumerator SwitchToItemSlotAfterDelay(PlayerControllerB __instance, int slot)
        {
            int oldSlot = __instance.currentItemSlot;
            //float delay = !ConfigSettings.disableFasterHotbarSwapping.Value ? ConfigSettings.minSwapItemInterval : 0.3f;
            //timeDroppedItem = Time.time;
            yield return new WaitUntil(() => __instance.currentlyHeldObjectServer == null || __instance.currentItemSlot != oldSlot);
            //float dTime = Time.time - timeDroppedItem;
            if (__instance.currentItemSlot == oldSlot)
                SyncManager.SwapHotbarSlot(slot);
            else
                Plugin.LogWarning("Failed to perform item quick drop. Current selected item slot was updated before drop animation could complete. (this is okay)");
            droppingItem = false;
        }


        [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed")]
        [HarmonyPrefix]
        private static bool PreventItemSwappingDroppingItem(InputAction.CallbackContext context, PlayerControllerB __instance)
        {
            if (__instance == localPlayerController && droppingItem)
            {
                float time = Time.time;
                if (time - timeLoggedPreventedItemSwap > 1)
                {
                    timeLoggedPreventedItemSwap = time;
                    Plugin.LogWarning("[VERBOSE] Prevented item swap. Player is currently discarding an item? This should be fine, unless these logs are spamming.");
                }
                return false;
            }
            return true;
        }
    }
}
