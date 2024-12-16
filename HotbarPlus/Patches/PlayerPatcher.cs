using System.Collections.Generic;
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
using HotbarPlus.Compatibility;


namespace HotbarPlus.Patches
{
	[HarmonyPatch]
	public class PlayerPatcher
	{
		static PlayerControllerB localPlayerController { get { return StartOfRound.Instance?.localPlayerController; } }
		public static int vanillaHotbarSize = -1;
		public static int mainHotbarSize = -1;


        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        private static void ResetValues(StartOfRound __instance)
		{
			vanillaHotbarSize = -1;
            mainHotbarSize = vanillaHotbarSize;
        }


        [HarmonyPatch(typeof(PlayerControllerB), "Awake")]
		[HarmonyPostfix]
		public static void GetInitialHotbarSize(PlayerControllerB __instance)
		{
			if (vanillaHotbarSize == -1)
			{
				vanillaHotbarSize = __instance.ItemSlots.Length;
				mainHotbarSize = vanillaHotbarSize;
			}
		}


		[HarmonyPatch(typeof(PlayerControllerB), "ConnectClientToPlayerObject")]
		[HarmonyPostfix]
		public static void InitializeLocalPlayer(PlayerControllerB __instance)
		{
            HUDManager.Instance.itemSlotIconFrames[Mathf.Max(__instance.currentItemSlot, 0)].GetComponent<Animator>().SetBool("selectedSlot", true);
			HUDManager.Instance.PingHUDElement(HUDManager.Instance.Inventory, 0.1f, 0.13f, 0.13f);
        }


		public static void ResizeInventory()
		{
			int dHotbarSize = SyncManager.currentHotbarSize - mainHotbarSize;

			if (dHotbarSize == 0)
                return;

			Plugin.LogWarning("Resizing main hotbar to: " + SyncManager.currentHotbarSize + ". Previous: " + mainHotbarSize);
            foreach (var playerController in StartOfRound.Instance.allPlayerScripts)
			{
				var inventory = new List<GrabbableObject>(playerController.ItemSlots);
				// If increasing hotbar size
				if (dHotbarSize > 0)
				{
                    for (int i = 0; i < Mathf.Abs(dHotbarSize); i++)
					{
                        inventory.Insert(mainHotbarSize, null);
						if (playerController.currentItemSlot >= mainHotbarSize)
							playerController.currentItemSlot++;
					}
				}
				// If decreasing hotbar size
				else
				{
                    for (int i = 0; i < Mathf.Abs(dHotbarSize); i++)
					{
                        inventory.RemoveAt(SyncManager.currentHotbarSize);
						if (playerController.currentItemSlot >= SyncManager.currentHotbarSize)
                            playerController.currentItemSlot--;
					}
                }
				playerController.ItemSlots = inventory.ToArray();
			}

			mainHotbarSize = SyncManager.currentHotbarSize;
        }


        public static int CallGetNextItemSlot(PlayerControllerB __instance, bool forward, int index)
		{
			int currentItemSlot = __instance.currentItemSlot;
			__instance.currentItemSlot = index;
			MethodInfo method = __instance.GetType().GetMethod("NextItemSlot", BindingFlags.NonPublic | BindingFlags.Instance);
			index = (int)method.Invoke(__instance, new object[] { forward });
			__instance.currentItemSlot = currentItemSlot;
			return index;
		}


        public static void CallSwitchToItemSlot(PlayerControllerB __instance, int index, GrabbableObject fillSlotWithItem = null)
        {
            MethodInfo method = __instance.GetType().GetMethod("SwitchToItemSlot", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(__instance, new object[] { index, fillSlotWithItem });
			SetTimeSinceSwitchingSlots(__instance, 0);
        }


        public static float GetTimeSinceSwitchingSlots(PlayerControllerB playerController) => (float)Traverse.Create(playerController).Field("timeSinceSwitchingSlots").GetValue();
        public static void SetTimeSinceSwitchingSlots(PlayerControllerB playerController, float value) => Traverse.Create(playerController).Field("timeSinceSwitchingSlots").SetValue(value);


        // Faster actions patches
        [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed")]
		[HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> PatchSwitchItemInterval(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);
			if (!ConfigSettings.disableFasterHotbarSwappingConfig.Value && !GeneralImprovements_Compat.Enabled)
			{
				for (int i = 0; i < codes.Count; i++)
				{
					if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.3f)
					{
						codes[i].operand = ConfigSettings.minSwapItemInterval;
						break;
					}
				}
			}
			return codes.AsEnumerable();
		}


		[HarmonyPatch(typeof(PlayerControllerB), "ActivateItem_performed")]
		[HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> PatchActivateItemInterval(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);
            if (!ConfigSettings.disableFasterItemActivateConfig.Value && !GeneralImprovements_Compat.Enabled)
			{
				for (int i = 0; i < codes.Count; i++)
				{
					if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.075f)
					{
						codes[i].operand = ConfigSettings.minActivateItemInterval;
						break;
					}
				}
			}
			return codes.AsEnumerable();
		}


		[HarmonyPatch(typeof(PlayerControllerB), "Discard_performed")]
		[HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> PatchDiscardItemInterval(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);
            if (!ConfigSettings.disableFasterItemDroppingConfig.Value)
			{
				for (int i = 0; i < codes.Count; i++)
				{
					if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.2f)
						codes[i].operand = ConfigSettings.minDiscardItemInterval;
				}
			}
			return codes.AsEnumerable();
		}


		[HarmonyPatch(typeof(PlayerControllerB), "Interact_performed")]
		[HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> PatchInteractInterval(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);
			if (!GeneralImprovements_Compat.Enabled)
			{
				for (int i = 0; i < codes.Count; i++)
				{
					if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.2f)
					{
						codes[i].operand = ConfigSettings.minInteractInterval;
						break;
					}
				}
			}
			return codes.AsEnumerable();
		}


		[HarmonyPatch(typeof(PlayerControllerB), "PerformEmote")]
		[HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> PatchPerformEmoteInterval(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);
			for (int i = 0; i < codes.Count; i++)
			{
				if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.5f)
				{
					codes[i].operand = ConfigSettings.minUseEmoteInterval;
					break;
				}
			}
			return codes.AsEnumerable();
		}

		
		[HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed")]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> InvertHotbarScrollDirection(IEnumerable<CodeInstruction> instructions)
		{
			var codes = new List<CodeInstruction>(instructions);
			if (ConfigSettings.invertHotbarScrollDirectionConfig.Value)
            {
				for (int i = 1; i < codes.Count; i++)
				{
					if (codes[i].opcode == OpCodes.Ble_Un && codes[i - 1].opcode == OpCodes.Ldc_R4 && (float)codes[i - 1].operand == 0f)
					{
						codes[i].opcode = OpCodes.Bge_Un;
						break;
					}
				}
            }
			return codes.AsEnumerable();
		}
	}
}