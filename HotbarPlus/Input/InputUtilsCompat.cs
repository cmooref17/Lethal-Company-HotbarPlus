using BepInEx.Bootstrap;
using LethalCompanyInputUtils.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.InputSystem;

namespace HotbarPlus.Input
{
    internal class InputUtilsCompat
    {
        internal static InputActionAsset Asset { get { return IngameKeybinds.GetAsset(); } }
        internal static bool Enabled => Plugin.IsModLoaded("com.rune580.LethalCompanyInputUtils");

        public static InputAction QuickHotbarSlotHotkey1 => IngameKeybinds.Instance.QuickHotbarSlotHotkey1;
        public static InputAction QuickHotbarSlotHotkey2 => IngameKeybinds.Instance.QuickHotbarSlotHotkey2;
        public static InputAction QuickHotbarSlotHotkey3 => IngameKeybinds.Instance.QuickHotbarSlotHotkey3;
        public static InputAction QuickHotbarSlotHotkey4 => IngameKeybinds.Instance.QuickHotbarSlotHotkey4;
        public static InputAction QuickHotbarSlotHotkey5 => IngameKeybinds.Instance.QuickHotbarSlotHotkey5;
        public static InputAction QuickHotbarSlotHotkey6 => IngameKeybinds.Instance.QuickHotbarSlotHotkey6;
    }
}
