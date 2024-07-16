using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReservedItemSlotCore;
using ReservedItemSlotCore.Data;


namespace HotbarPlus.Compatibility
{
    internal static class ReservedItemSlots_Compat
    {
        public static bool Enabled { get { return Plugin.IsModLoaded("FlipMods.ReservedItemSlotCore"); } }


        public static bool IsToggledInReservedItemSlots()
        {
            if (!Enabled)
                return false;

            return ReservedHotbarManager.isToggledInReservedSlots;
        }


        public static bool IsItemSlotReserved(int index)
        {
            if (!Enabled) 
                return false;

            return ReservedPlayerData.localPlayerData.IsReservedItemSlot(index);
        }


        public static bool ShouldDisableEnergyBarsReservedItemSlots()
        {
            if (!Enabled)
                return false;
            try { return ReservedItemSlotCore.Config.ConfigSettings.disableHotbarPlusEnergyBars.Value; }
            catch { return false; }
        }
    }
}
