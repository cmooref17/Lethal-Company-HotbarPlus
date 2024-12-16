using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using GameNetcodeStuff;
using HarmonyLib;
using HotbarPlus.Config;
using HotbarPlus.Patches;

namespace HotbarPlus.UI
{
    internal static class LightningIndicatorManager
    {
        private static Color warningIconColorHidden = new Color(1, 1, 1, 0);
        private static float iconScale = 0.85f;

        private static GameObject currentMetalObject;
        private static Image currentWarningIcon = null;
        private static float timeSetWarning = 0;
        private static float updateTime = 0;


        [HarmonyPatch(typeof(StormyWeather), "OnDisable")]
        [HarmonyPrefix]
        private static void OnStopStorm()
        {
            if (currentWarningIcon)
                ClearCurrentWarningIcon();
        }


        [HarmonyPatch(typeof(StormyWeather), "Update")]
        [HarmonyPostfix]
        private static void Update()
        {
            if (currentMetalObject && !ConfigSettings.disableItemStaticWarningsConfig.Value)
            {
                if (Time.time - updateTime > 0.1f)
                {
                    updateTime = Time.time;
                    Image warningIcon = null;
                    for (int i = 0; i < StartOfRound.Instance.localPlayerController.ItemSlots.Length; i++)
                    {
                        var itemObject = StartOfRound.Instance.localPlayerController.ItemSlots[i]?.gameObject;
                        if (currentMetalObject == itemObject)
                        {
                            Image itemSlotFrame = HUDManager.Instance.itemSlotIconFrames[i];
                            warningIcon = itemSlotFrame.transform.Find("LightningWarningIcon")?.GetComponent<Image>();
                            if (!warningIcon)
                            {
                                warningIcon = GameObject.Instantiate(Plugin.lightningIndicatorPrefab)?.GetComponent<Image>();

                                warningIcon.name = "LightningWarningIcon";
                                warningIcon.transform.SetParent(itemSlotFrame.transform);

                                warningIcon.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                                warningIcon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                                warningIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);

                                float warningIconScale = (itemSlotFrame.rectTransform.sizeDelta.x / HUDPatcher.defaultItemFrameSize.x) * iconScale;
                                warningIcon.rectTransform.localScale = (Vector3.one / 36) * warningIconScale;
                                warningIcon.rectTransform.anchoredPosition3D = new Vector3(0, 0, 0);
                                warningIcon.transform.localEulerAngles = new Vector3(0, 0, 90);
                            }
                            break;
                        }
                    }

                    if (warningIcon)
                    {
                        if (currentWarningIcon != warningIcon)
                        {
                            ClearCurrentWarningIcon();
                            SetCurrentWarningIcon(warningIcon);
                        }
                    }
                    else if (currentWarningIcon)
                        ClearCurrentWarningIcon();
                }
            }
            else if (currentWarningIcon)
                ClearCurrentWarningIcon();

            if (currentWarningIcon)
                currentWarningIcon.color = new Color(1, 1, 1, (Mathf.Sin(2 * Mathf.PI * (Time.time - timeSetWarning - 0.25f)) + 1) / 2); // Because I'm a math nerd, idk
        }


        [HarmonyPatch(typeof(StormyWeather), "SetStaticElectricityWarning")]
        [HarmonyPrefix]
        private static void OnSetStaticToObject(NetworkObject warningObject, float particleTime)
        {
            currentMetalObject = warningObject.gameObject;
        }


        [HarmonyPatch(typeof(StormyWeather), "LightningStrike")]
        [HarmonyPrefix]
        private static void OnLightningStrike(Vector3 strikePosition, bool useTargetedObject)
        {
            if (useTargetedObject)
                currentMetalObject = null;
        }


        [HarmonyPatch(typeof(PlayerControllerB), "SetObjectAsNoLongerHeld")]
        [HarmonyPrefix]
        private static void OnDiscardItem(bool droppedInElevator, bool droppedInShipRoom, Vector3 targetFloorPosition, GrabbableObject dropObject, PlayerControllerB __instance)
        {
            if (currentWarningIcon && dropObject == currentMetalObject && __instance == StartOfRound.Instance.localPlayerController)
                ClearCurrentWarningIcon();
        }


        private static void SetCurrentWarningIcon(Image warningIcon)
        {
            if (currentWarningIcon && currentWarningIcon != warningIcon)
                ClearCurrentWarningIcon();

            if (warningIcon)
            {
                currentWarningIcon = warningIcon;
                warningIcon.color = new Color(1, 1, 1, 0);
                timeSetWarning = Time.time;
                //Plugin.Log("Setting current lightning warning icon to slot: " + warningIcon.transform.parent.name);
            }
        }


        private static void ClearCurrentWarningIcon()
        {
            if (currentWarningIcon)
            {
                currentWarningIcon.color = warningIconColorHidden;
                currentWarningIcon = null;
                //Plugin.Log("Clearing current lightning warning icon.");
            }
        }
    }
}
