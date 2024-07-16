using GameNetcodeStuff;
using HarmonyLib;
using HotbarPlus.Compatibility;
using HotbarPlus.Config;
using HotbarPlus.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace HotbarPlus.UI
{
    [HarmonyPatch]
    internal static class EnergyBarManager
    {
        private static PlayerControllerB localPlayerController { get { return StartOfRound.Instance?.localPlayerController; } }
        internal static Dictionary<Image, EnergyBarData> energyBarSlidersDict = new Dictionary<Image, EnergyBarData>();
        public static Color energyBarColor = new Color(200, 200, 0, 0.75f);


        [HarmonyPatch(typeof(HUDManager), "Awake")]
        [HarmonyPrefix]
        private static void Init(HUDManager __instance)
        {
            energyBarSlidersDict?.Clear();
        }


        [HarmonyPatch(typeof(StartOfRound), "ResetShip")]
        [HarmonyPrefix]
        private static void OnResetShip()
        {
            ResetEnergyBars();
        }


        [HarmonyPatch(typeof(PlayerControllerB), "SpawnPlayerAnimation")]
        [HarmonyPrefix]
        private static void OnPlayerRespawn(PlayerControllerB __instance)
        {
            if (__instance != localPlayerController)
                return;

            ResetEnergyBars();
        }


        private static void ResetEnergyBars()
        {
            Plugin.Log("Resetting energy bars.");
            foreach (var energyBarData in energyBarSlidersDict.Values)
                GameObject.DestroyImmediate(energyBarData.gameObject);

            energyBarSlidersDict?.Clear();
        }


        [HarmonyPatch(typeof(HUDManager), "Update")]
        [HarmonyPostfix]
        private static void UpdateEnergyBars(HUDManager __instance)
        {
            if (localPlayerController == null || __instance?.itemSlotIconFrames == null || localPlayerController.isPlayerDead)
                return;

            for (int i = 0; i < __instance.itemSlotIconFrames.Length; i++)
            {
                var itemSlotFrame = __instance.itemSlotIconFrames[i];
                if (!itemSlotFrame)
                    continue;

                GrabbableObject item = null;
                if (i >= 0 && i < localPlayerController.ItemSlots.Length)
                    item = localPlayerController.ItemSlots[i];

                if (!energyBarSlidersDict.TryGetValue(itemSlotFrame, out var energyBarData))
                {
                    GameObject energyBar = itemSlotFrame.transform.Find("EnergyBar")?.gameObject;
                    if (!energyBar)
                        energyBar = GameObject.Instantiate(Plugin.energyBarPrefab);

                    energyBar.name = "EnergyBar";
                    energyBar.transform.SetParent(itemSlotFrame.transform);
                    energyBarData = new EnergyBarData(energyBar);
                    //energyBarData.rectTransform.anchoredPosition3D = new Vector3(14, 0, 0);
                    //energyBarData.rectTransform.localPosition = new Vector2(itemSlotFrame.rectTransform.rect.center.x, itemSlotFrame.rectTransform.rect.min.y + 4 * HUDPatcher.currentOverrideHotbarHudScale);
                    energyBarData.rectTransform.anchorMin = new Vector2(1, 0.5f);
                    energyBarData.rectTransform.anchorMax = new Vector2(1, 0.5f);
                    energyBarData.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    
                    energyBarData.SetEnergyBarColor(energyBarColor);
                    energyBarSlidersDict.Add(itemSlotFrame, energyBarData);
                }

                if (!energyBarData.gameObject)
                {
                    energyBarSlidersDict.Remove(itemSlotFrame);
                    continue;
                }

                float energyBarScale = itemSlotFrame.rectTransform.sizeDelta.x / HUDPatcher.defaultItemFrameSize.x;
                energyBarData.rectTransform.localScale = (Vector3.one / 36) * energyBarScale;
                energyBarData.rectTransform.anchoredPosition3D = new Vector3(-4f * energyBarScale, 0, 0);
                energyBarData.transform.localEulerAngles = new Vector3(0, 0, 90);

                if (ConfigSettings.disableEnergyBars.Value || !item || !item.itemProperties.requiresBattery || item.insertedBattery == null || (ReservedItemSlots_Compat.Enabled && itemSlotFrame.name.ToLower().Contains("reserved") && ReservedItemSlots_Compat.ShouldDisableEnergyBarsReservedItemSlots()))
                {
                    if (energyBarData.gameObject.activeSelf)
                        energyBarData.gameObject.SetActive(false);
                }
                else
                {
                    energyBarData.gameObject.SetActive(true);
                    energyBarData.slider.value = Mathf.Clamp(item.insertedBattery.charge, 0, 1);
                }
            }
        }
    }
}
