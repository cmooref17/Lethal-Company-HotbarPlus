using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace HotbarPlus.Compatibility
{
    internal static class GeneralImprovements_Compat
    {
        public static bool Enabled { get { return Plugin.IsModLoaded("ShaosilGaming.GeneralImprovements"); } }
    }
}
