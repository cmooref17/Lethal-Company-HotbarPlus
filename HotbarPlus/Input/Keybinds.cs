using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine.InputSystem;
using HotbarPlus.Config;
using HotbarPlus.Patches;
using System.ComponentModel;
using System.Xml.Linq;
using LethalCompanyInputUtils.Api;
using UnityEngine;
using HotbarPlus.Networking;
using HotbarPlus.Compatibility;

namespace HotbarPlus.Input
{
	[HarmonyPatch]
	public class Keybinds
	{
		static PlayerControllerB localPlayerController { get { return StartOfRound.Instance?.localPlayerController; } }
		static bool setHotbarSize = false;

		public static InputActionAsset Asset;
		public static InputActionMap ActionMap;
		public static InputAction quickHotbarSlotHotkey1;
        public static InputAction quickHotbarSlotHotkey2;
        public static InputAction quickHotbarSlotHotkey3;
        public static InputAction quickHotbarSlotHotkey4;
        public static InputAction quickHotbarSlotHotkey5;
        public static InputAction quickHotbarSlotHotkey6;
        public static InputAction quickHotbarSlotHotkey7;
        public static InputAction quickHotbarSlotHotkey8;



        //static InputAction[] quickItemShortcutActions;

		public static void InitKeybinds()
        {
			setHotbarSize = false;
            if (!ConfigSettings.useHotbarNumberHotkeysConfig.Value)
                return;

			if (InputUtilsCompat.Enabled)
			{
                Asset = InputUtilsCompat.Asset;
                quickHotbarSlotHotkey1 = InputUtilsCompat.QuickHotbarSlotHotkey1;
                quickHotbarSlotHotkey2 = InputUtilsCompat.QuickHotbarSlotHotkey2;
                quickHotbarSlotHotkey3 = InputUtilsCompat.QuickHotbarSlotHotkey3;
                quickHotbarSlotHotkey4 = InputUtilsCompat.QuickHotbarSlotHotkey4;
                quickHotbarSlotHotkey5 = InputUtilsCompat.QuickHotbarSlotHotkey5;
                quickHotbarSlotHotkey6 = InputUtilsCompat.QuickHotbarSlotHotkey6;
                quickHotbarSlotHotkey7 = InputUtilsCompat.QuickHotbarSlotHotkey7;
                quickHotbarSlotHotkey8 = InputUtilsCompat.QuickHotbarSlotHotkey8;
            }
			else
			{
                Asset = ScriptableObject.CreateInstance<InputActionAsset>();
                ActionMap = new InputActionMap("HotbarPlus");
                Asset.AddActionMap(ActionMap);

                quickHotbarSlotHotkey1 = ActionMap.AddAction("[HB+] Quick Slot 1", binding: "<Keyboard>/1", interactions: "Press");
                quickHotbarSlotHotkey2 = ActionMap.AddAction("[HB+] Quick Slot 2", binding: "<Keyboard>/2", interactions: "Press");
                quickHotbarSlotHotkey3 = ActionMap.AddAction("[HB+] Quick Slot 3", binding: "<Keyboard>/3", interactions: "Press");
                quickHotbarSlotHotkey4 = ActionMap.AddAction("[HB+] Quick Slot 4", binding: "<Keyboard>/4", interactions: "Press");
                quickHotbarSlotHotkey5 = ActionMap.AddAction("[HB+] Quick Slot 5", binding: "<Keyboard>/5", interactions: "Press");
                quickHotbarSlotHotkey6 = ActionMap.AddAction("[HB+] Quick Slot 6", binding: "<Keyboard>/6", interactions: "Press");
                quickHotbarSlotHotkey7 = ActionMap.AddAction("[HB+] Quick Slot 7", binding: "<Keyboard>/7", interactions: "Press");
                quickHotbarSlotHotkey8 = ActionMap.AddAction("[HB+] Quick Slot 8", binding: "<Keyboard>/8", interactions: "Press");
            }
		}


        public static void OnSetHotbarSize() => setHotbarSize = true;


		[HarmonyPatch(typeof(StartOfRound), "OnEnable")]
		[HarmonyPostfix]
		public static void OnEnable()
		{
            if (!ConfigSettings.useHotbarNumberHotkeysConfig.Value)
                return;

            Asset.Enable();
            quickHotbarSlotHotkey1.performed += OnPressItemSlotHotkeyAction1;
            quickHotbarSlotHotkey2.performed += OnPressItemSlotHotkeyAction2;
            quickHotbarSlotHotkey3.performed += OnPressItemSlotHotkeyAction3;
            quickHotbarSlotHotkey4.performed += OnPressItemSlotHotkeyAction4;
            quickHotbarSlotHotkey5.performed += OnPressItemSlotHotkeyAction5;
            quickHotbarSlotHotkey6.performed += OnPressItemSlotHotkeyAction6;
            quickHotbarSlotHotkey7.performed += OnPressItemSlotHotkeyAction7;
            quickHotbarSlotHotkey8.performed += OnPressItemSlotHotkeyAction8;
        }

		[HarmonyPatch(typeof(StartOfRound), "OnDisable")]
		[HarmonyPostfix]
		public static void OnDisable()
		{
            if (!ConfigSettings.useHotbarNumberHotkeysConfig.Value)
                return;

            Asset.Disable();
            quickHotbarSlotHotkey1.performed -= OnPressItemSlotHotkeyAction1;
            quickHotbarSlotHotkey2.performed -= OnPressItemSlotHotkeyAction2;
            quickHotbarSlotHotkey3.performed -= OnPressItemSlotHotkeyAction3;
            quickHotbarSlotHotkey4.performed -= OnPressItemSlotHotkeyAction4;
            quickHotbarSlotHotkey5.performed -= OnPressItemSlotHotkeyAction5;
            quickHotbarSlotHotkey6.performed -= OnPressItemSlotHotkeyAction6;
            quickHotbarSlotHotkey7.performed -= OnPressItemSlotHotkeyAction7;
            quickHotbarSlotHotkey8.performed -= OnPressItemSlotHotkeyAction8;
        }

        private static void OnPressItemSlotHotkeyAction1(InputAction.CallbackContext context) => OnPressItemSlotHotkeyAction(context, 0);
        private static void OnPressItemSlotHotkeyAction2(InputAction.CallbackContext context) => OnPressItemSlotHotkeyAction(context, 1);
        private static void OnPressItemSlotHotkeyAction3(InputAction.CallbackContext context) => OnPressItemSlotHotkeyAction(context, 2);
        private static void OnPressItemSlotHotkeyAction4(InputAction.CallbackContext context) => OnPressItemSlotHotkeyAction(context, 3);
        private static void OnPressItemSlotHotkeyAction5(InputAction.CallbackContext context) => OnPressItemSlotHotkeyAction(context, 4);
        private static void OnPressItemSlotHotkeyAction6(InputAction.CallbackContext context) => OnPressItemSlotHotkeyAction(context, 5);
        private static void OnPressItemSlotHotkeyAction7(InputAction.CallbackContext context) => OnPressItemSlotHotkeyAction(context, 6);
        private static void OnPressItemSlotHotkeyAction8(InputAction.CallbackContext context) => OnPressItemSlotHotkeyAction(context, 7);


        private static void OnPressItemSlotHotkeyAction(InputAction.CallbackContext context, int slot)
		{
            if (localPlayerController == null || !localPlayerController.IsOwner || !localPlayerController.isPlayerControlled || !SyncManager.isSynced)
                return;
            if (!context.performed || !ConfigSettings.useHotbarNumberHotkeysConfig.Value || !setHotbarSize || slot < 0 || slot >= SyncManager.currentHotbarSize)
                return;

            bool throwingObject = (bool)Traverse.Create(localPlayerController).Field("throwingObject").GetValue();
            if (throwingObject || localPlayerController.isTypingChat || localPlayerController.inTerminalMenu || localPlayerController.quickMenuManager.isMenuOpen || localPlayerController.isPlayerDead || localPlayerController.isGrabbingObjectAnimation || localPlayerController.activatingItem || localPlayerController.inSpecialInteractAnimation || localPlayerController.twoHanded || localPlayerController.jetpackControls || localPlayerController.disablingJetpackControls || PlayerPatcher.GetTimeSinceSwitchingSlots(localPlayerController) < (!ConfigSettings.disableFasterHotbarSwappingConfig.Value ? ConfigSettings.minSwapItemInterval : 0.3f))
                return;

            SyncManager.SwapHotbarSlot(slot);
        }
	}
}