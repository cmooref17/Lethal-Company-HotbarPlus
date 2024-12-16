using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using HotbarPlus.Config;
using HotbarPlus.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HotbarPlus
{
	[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
	{
		public static Plugin instance;
		private Harmony _harmony;
        private static ManualLogSource logger;

        internal static GameObject energyBarPrefab;
        internal static GameObject lightningIndicatorPrefab;


        private void Awake()
		{
			instance = this;
            CreateCustomLogger();
			ConfigSettings.BindConfigSettings();
			Keybinds.InitKeybinds();

            LoadUIAssets();

            this._harmony = new Harmony(PluginInfo.PLUGIN_NAME);
			PatchAll();
			Log(PluginInfo.PLUGIN_NAME + " loaded");
        }


        private void LoadUIAssets()
        {
            try
            {
                string assetsPath = Path.Combine(Path.GetDirectoryName(instance.Info.Location), "Assets/hotbarplus_assets");
                AssetBundle assetBundle = AssetBundle.LoadFromFile(assetsPath);
                energyBarPrefab = assetBundle.LoadAsset<GameObject>("energy_bar");
                lightningIndicatorPrefab = assetBundle.LoadAsset<GameObject>("lightning_indicator");
            }
            catch
            {
                LogError("Failed to load UI assets from Asset Bundle.");
            }
        }


        private void PatchAll()
        {
            IEnumerable<Type> types;
            try { types = Assembly.GetExecutingAssembly().GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null); }
            foreach (var type in types)
                this._harmony.PatchAll(type);
        }

        private void CreateCustomLogger()
        {
            try { logger = BepInEx.Logging.Logger.CreateLogSource(string.Format("{0}-{1}", PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)); }
            catch { logger = Logger; }
        }

        public static void Log(string message) => logger.LogInfo(message);
        public static void LogError(string message) => logger.LogError(message);
        public static void LogWarning(string message) => logger.LogWarning(message);

        public static bool IsModLoaded(string guid) => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(guid);
    }
}
