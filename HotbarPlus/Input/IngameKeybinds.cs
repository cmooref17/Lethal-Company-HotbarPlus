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
    internal class IngameKeybinds : LcInputActions
    {
        internal static IngameKeybinds Instance = new IngameKeybinds();
        internal static InputActionAsset GetAsset() => Instance.Asset;

        [InputAction("<Keyboard>/1", Name = "[HB+] Quick Slot 1")]
        public InputAction QuickHotbarSlotHotkey1 { get; set; }

        [InputAction("<Keyboard>/2", Name = "[HB+] Quick Slot 2")]
        public InputAction QuickHotbarSlotHotkey2 { get; set; }

        [InputAction("<Keyboard>/3", Name = "[HB+] Quick Slot 3")]
        public InputAction QuickHotbarSlotHotkey3 { get; set; }

        [InputAction("<Keyboard>/4", Name = "[HB+] Quick Slot 4")]
        public InputAction QuickHotbarSlotHotkey4 { get; set; }

        [InputAction("<Keyboard>/5", Name = "[HB+] Quick Slot 5")]
        public InputAction QuickHotbarSlotHotkey5 { get; set; }

        [InputAction("<Keyboard>/6", Name = "[HB+] Quick Slot 6")]
        public InputAction QuickHotbarSlotHotkey6 { get; set; }

        [InputAction("<Keyboard>/7", Name = "[HB+] Quick Slot 7")]
        public InputAction QuickHotbarSlotHotkey7 { get; set; }

        [InputAction("<Keyboard>/8", Name = "[HB+] Quick Slot 8")]
        public InputAction QuickHotbarSlotHotkey8 { get; set; }
    }
}
