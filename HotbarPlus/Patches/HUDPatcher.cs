﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using GameNetcodeStuff;
using System.Reflection;
using Unity.Netcode;
using HotbarPlus.Config;
using System.Collections;
using HotbarPlus.Networking;
using HotbarPlus.UI;
using HotbarPlus.Compatibility;


namespace HotbarPlus.Patches
{
	[HarmonyPatch]
	public class HUDPatcher
	{
		internal static List<Image> mainItemSlotFrames = new List<Image>();
		internal static List<Image> mainItemSlotIcons = new List<Image>();
		private static int mainHotbarSize = 4; // Main as in vanilla plus extra hotbar slots added by this mod

		internal static float hotbarSlotSize = 40;
        internal static Vector2 defaultItemFrameSize;
        internal static Vector2 defaultItemIconSize;
        internal static float defaultItemSlotPosY;

        internal static float currentOverrideHotbarSpacing;
		internal static float currentOverrideHotbarHudScale;


		[HarmonyPatch(typeof(HUDManager), "Awake")]
		[HarmonyPostfix]
		private static void Init(HUDManager __instance)
		{
			mainItemSlotFrames.Clear();
			mainItemSlotIcons.Clear();
			mainHotbarSize = __instance.itemSlotIconFrames.Length;

			for (int i = 0; i < mainHotbarSize; i++)
            {
				mainItemSlotFrames.Add(__instance.itemSlotIconFrames[i]);
				mainItemSlotIcons.Add(__instance.itemSlotIcons[i]);
            }

			defaultItemFrameSize = __instance.itemSlotIconFrames[0].rectTransform.sizeDelta;
			defaultItemIconSize = __instance.itemSlotIcons[0].rectTransform.sizeDelta;
            defaultItemSlotPosY = __instance.itemSlotIconFrames[0].rectTransform.anchoredPosition.y;

            /*if (ConfigSettings.centerInventoryUIFix.Value)
			{
				var rectTransform = __instance.Inventory?.canvasGroup?.transform as RectTransform;
				if (rectTransform != null)
				{
					rectTransform.pivot = new Vector2(0.5f, rectTransform.pivot.y);
					rectTransform.anchorMin = new Vector2(0.5f, rectTransform.anchorMin.y);
					rectTransform.anchorMax = new Vector2(0.5f, rectTransform.anchorMax.y);
					rectTransform.anchoredPosition3D = new Vector3(0, rectTransform.anchoredPosition3D.y, rectTransform.anchoredPosition3D.z);
				}
			}*/
		}


        public static void ResizeHotbarSlotsHUD()
		{
			List<Image> itemSlotIconFrames = new List<Image>(HUDManager.Instance.itemSlotIconFrames);
			List<Image> itemSlotIcons = new List<Image>(HUDManager.Instance.itemSlotIcons);

            float uiSpacing = (hotbarSlotSize + ConfigSettings.overrideHotbarSpacingConfig.Value) * ConfigSettings.overrideHotbarHudSizeConfig.Value;
			float yPos = defaultItemSlotPosY + 36 * ((ConfigSettings.overrideHotbarHudSizeConfig.Value - 1) / 2f);

            Vector3 iconFrameRotation = itemSlotIconFrames[0].rectTransform.eulerAngles;
			Vector3 iconRotation = itemSlotIcons[0].rectTransform.eulerAngles;

			mainItemSlotFrames.Clear();
			mainItemSlotIcons.Clear();

            for (int i = 0; i < Mathf.Max(SyncManager.currentHotbarSize, mainHotbarSize); i++)
			{
				if (i >= SyncManager.currentHotbarSize)
				{
					GameObject.Destroy(itemSlotIconFrames[SyncManager.currentHotbarSize]);
					GameObject.Destroy(itemSlotIcons[SyncManager.currentHotbarSize]);
					itemSlotIconFrames.RemoveAt(SyncManager.currentHotbarSize);
					itemSlotIcons.RemoveAt(SyncManager.currentHotbarSize);
					continue;
				}
				if (i >= mainHotbarSize)
				{
					Image itemSlotFrame = Image.Instantiate(itemSlotIconFrames[0], itemSlotIconFrames[0].transform.parent);

                    itemSlotIconFrames.Insert(i, itemSlotFrame);
					itemSlotIconFrames[i].transform.SetSiblingIndex(itemSlotIconFrames[i - 1].transform.GetSiblingIndex() + 1);

					Image itemIcon = itemSlotIconFrames[i].transform.GetChild(0).GetComponent<Image>();
					itemIcon.sprite = null;
					itemIcon.enabled = false;
                    itemSlotIcons.Insert(i, itemIcon);

					itemSlotIconFrames[i].fillMethod = itemSlotIconFrames[0].fillMethod;
					itemSlotIconFrames[i].sprite = itemSlotIconFrames[0].sprite;
					itemSlotIconFrames[i].material = itemSlotIconFrames[0].material;
					if (Plugin.IsModLoaded("xuxiaolan.hotbarrd"))
						itemSlotIconFrames[i].overrideSprite = itemSlotIconFrames[0].overrideSprite;
				}

				mainItemSlotFrames.Insert(i, itemSlotIconFrames[i]);
				mainItemSlotIcons.Insert(i, itemSlotIcons[i]);

				itemSlotIconFrames[i].name = string.Format("Slot{0}", i);
				itemSlotIconFrames[i].rectTransform.anchoredPosition = Vector2.up * yPos;
				itemSlotIconFrames[i].rectTransform.eulerAngles = iconFrameRotation;

				itemSlotIcons[i].name = "Icon";
				itemSlotIcons[i].rectTransform.eulerAngles = iconRotation;
			}

			mainHotbarSize = SyncManager.currentHotbarSize;
			HUDManager.Instance.itemSlotIconFrames = itemSlotIconFrames.ToArray();
			HUDManager.Instance.itemSlotIcons = itemSlotIcons.ToArray();

			UpdateUI();
        }


		internal static void UpdateUI()
        {
			var itemSlotIconFrames = HUDManager.Instance.itemSlotIconFrames;
			var itemSlotIcons = HUDManager.Instance.itemSlotIcons;

			float uiSpacing = (hotbarSlotSize + ConfigSettings.overrideHotbarSpacingConfig.Value) * ConfigSettings.overrideHotbarHudSizeConfig.Value;
			float yPos = defaultItemSlotPosY + 36 * ((ConfigSettings.overrideHotbarHudSizeConfig.Value - 1) / 2f);

			// Recenter
			float totalWidth = uiSpacing * (mainHotbarSize - 1);
			float offset = totalWidth / 2f;

			for (int i = 0; i < mainHotbarSize; i++)
			{
				float newXPos = (i * uiSpacing) - offset;
				itemSlotIconFrames[i].rectTransform.anchoredPosition = new Vector2(newXPos, yPos);
				itemSlotIconFrames[i].rectTransform.sizeDelta = defaultItemFrameSize * ConfigSettings.overrideHotbarHudSizeConfig.Value;
				itemSlotIcons[i].rectTransform.sizeDelta = defaultItemIconSize * ConfigSettings.overrideHotbarHudSizeConfig.Value;
			}

			currentOverrideHotbarSpacing = ConfigSettings.overrideHotbarSpacingConfig.Value;
			currentOverrideHotbarHudScale = ConfigSettings.overrideHotbarHudSizeConfig.Value;
		}


		[HarmonyPatch(typeof(HUDManager), "PingHUDElement")]
		[HarmonyPrefix]
		public static void OnPingHUDElement(HUDElement element, ref float startAlpha, ref float endAlpha, HUDManager __instance)
		{
			if (element != __instance.Inventory)
				return;

			if (endAlpha == 0.13f)
			{
				if (startAlpha == 0.13f && StartOfRound.Instance.localPlayerController.twoHanded)
					endAlpha = 1.0f;
				else
				{
					endAlpha = Mathf.Clamp(ConfigSettings.overrideFadeHudAlphaConfig.Value, 0, 1);
					if (startAlpha == 0.13f)
						startAlpha = endAlpha;
				}
			}
		}


        [HarmonyPatch(typeof(QuickMenuManager), "CloseQuickMenu")]
        [HarmonyPrefix]
        public static void OnCloseQuickMenu()
        {
            if (ReservedItemSlots_Compat.Enabled || ConfigSettings.overrideHotbarHudSizeConfig.Value != currentOverrideHotbarHudScale || ConfigSettings.overrideHotbarSpacingConfig.Value != currentOverrideHotbarSpacing)
                ResizeHotbarSlotsHUD();
        }
    }
}