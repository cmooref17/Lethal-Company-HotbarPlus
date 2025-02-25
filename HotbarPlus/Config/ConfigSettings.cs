﻿using BepInEx.Configuration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;
using HarmonyLib;
using BepInEx;
using System.IO;
using System;
using UnityEngine;

namespace HotbarPlus.Config
{
    public static class ConfigSettings
    {
        public static ConfigEntry<int> hotbarSizeConfig;
        public static ConfigEntry<int> purchasableHotbarSlotsConfig;
        public static ConfigEntry<int> purchasableHotbarSlotsPriceConfig;
        public static ConfigEntry<int> purchasableHotbarSlotsPriceIncreaseConfig;

        public static ConfigEntry<bool> useHotbarNumberHotkeysConfig;
        public static ConfigEntry<bool> invertHotbarScrollDirectionConfig;
        public static ConfigEntry<bool> useItemQuickDropConfig;
        public static ConfigEntry<bool> disableFasterHotbarSwappingConfig;
        public static ConfigEntry<bool> disableFasterItemDroppingConfig;
        public static ConfigEntry<bool> disableFasterItemActivateConfig;
        public static ConfigEntry<bool> applyFormattingToReservedItemSlotsConfig;

        public static ConfigEntry<bool> disableEnergyBarsConfig;
        public static ConfigEntry<bool> disableItemStaticWarningsConfig;
        //public static ConfigEntry<bool> centerInventoryUIFixConfig;
        public static ConfigEntry<int> overrideHotbarSpacingConfig;
        public static ConfigEntry<float> overrideFadeHudAlphaConfig;
        public static ConfigEntry<float> overrideHotbarHudSizeConfig;

        public static ConfigEntry<bool> verboseLogs;

        public static Dictionary<string, ConfigEntryBase> currentConfigEntries = new Dictionary<string, ConfigEntryBase>();

        // Unchangable right now
        public static float minSwapItemInterval = 0.05f;
        public static float minActivateItemInterval = 0.05f;
        public static float minDiscardItemInterval = 0.05f;
        public static float minInteractInterval = 0.05f;
        public static float minUseEmoteInterval = 0.25f;

        public static List<ConfigEntryBase> configEntries = new List<ConfigEntryBase>();

        public static void BindConfigSettings()
        {
            Plugin.Log("BindingConfigs");

            hotbarSizeConfig = AddConfigEntry(Plugin.instance.Config.Bind("Server-side", "NumHotbarSlots", 4, new ConfigDescription("[Host only] The amount of hotbar slots player will have. This will sync with other clients who have the mod.", new AcceptableValueRange<int>(0, 20))));
            purchasableHotbarSlotsConfig = AddConfigEntry(Plugin.instance.Config.Bind("Server-side", "PurchasableHotbarSlots", 0, new ConfigDescription("[Host only] The amount of hotbar slots that can be purchased", new AcceptableValueRange<int>(0, 10))));
            purchasableHotbarSlotsPriceConfig = AddConfigEntry(Plugin.instance.Config.Bind("Server-side", "PurchasableHotbarSlotsPrice", 200, "[Host only] The price of purchasing a hotbar slot."));
            purchasableHotbarSlotsPriceIncreaseConfig = AddConfigEntry(Plugin.instance.Config.Bind("Server-side", "PurchasableHotbarSlotsPriceIncrease", 100, "[Host only] The price increase on hotbar slots after each purchase."));

            useHotbarNumberHotkeysConfig = AddConfigEntry(Plugin.instance.Config.Bind("Client-side", "UseHotbarNumberHotkeys", true, "Use the quick item selection numerical hotkeys."));
            invertHotbarScrollDirectionConfig = AddConfigEntry(Plugin.instance.Config.Bind("Client-side", "InvertHotbarScrollDirection", true, "Inverts the direction in which you scroll on the hotbar. Will not affect the terminal scrolling direction."));
            useItemQuickDropConfig = AddConfigEntry(Plugin.instance.Config.Bind("Client-side", "UseItemQuickDropConfig", true, "If enabled, dropping an item will automatically swap to the next item for easier chain dropping. This may not work if the host does not have the mod. This is for stability reasons, and to help reduce de-sync."));
            disableFasterHotbarSwappingConfig = AddConfigEntry(Plugin.instance.Config.Bind("Client-side", "UseDefaultItemSwapInterval", false, "If true, the interval (delay) between swapping items will not be reduced by this mod."));
            disableFasterItemDroppingConfig = AddConfigEntry(Plugin.instance.Config.Bind("Client-side", "UseDefaultItemDropInterval", false, "If true, the interval (delay) between dropping items will not be reduced by this mod."));
            disableFasterItemActivateConfig = AddConfigEntry(Plugin.instance.Config.Bind("Client-side", "UseDefaultItemActivateInterval", false, "If true, the interval (delay) between activating items will not be reduced by this mod."));

            disableEnergyBarsConfig = AddConfigEntry(Plugin.instance.Config.Bind("UI", "DisableEnergyBars", false, "Disables/hides the energy bars for items in the HUD."));
            disableItemStaticWarningsConfig = AddConfigEntry(Plugin.instance.Config.Bind("UI", "DisableItemStaticWarnings", false, "Disables the lightning indicator that appears over item slots of held metal items that are about to be struck with lightning."));
            //centerInventoryUIFix = AddConfigEntry(Plugin.instance.Config.Bind("UI", "CenterInventoryUIFix", true, "Fixes the Inventory UI alignment to keep the HUD elements centered across all window sizes. Disable this if it's conflicting with other mods."));
            overrideHotbarSpacingConfig = AddConfigEntry(Plugin.instance.Config.Bind("UI", "OverrideHotbarHudSpacing", 10, new ConfigDescription("The spacing between each hotbar slot UI element.", new AcceptableValueRange<int>(0, 100))));
            overrideHotbarHudSizeConfig = AddConfigEntry(Plugin.instance.Config.Bind("UI", "OverrideHotbarHudSize", 1.0f, new ConfigDescription("Scales the hotbar slot HUD elements by a multiplier. HUD spacing/position should be scaled automatically.", new AcceptableValueRange<float>(0.1f, 10.0f))));
            overrideFadeHudAlphaConfig = AddConfigEntry(Plugin.instance.Config.Bind("UI", "OverrideFadeHudAlpha", 0.13f, new ConfigDescription("Sets the alpha for when the hotbar hud fades. Default = 0.13 | Fade completely: 0 | Never fade: 1", new AcceptableValueRange<float>(0f, 1f))));

            verboseLogs = AddConfigEntry(Plugin.instance.Config.Bind("Other", "VerboseLogs", true, "If enabled, extra logs will be created for debugging. This may be useful for tracking down various issues."));

            hotbarSizeConfig.Value = Mathf.Max(hotbarSizeConfig.Value, 0);
            purchasableHotbarSlotsConfig.Value = Mathf.Max(purchasableHotbarSlotsConfig.Value, 0);
            purchasableHotbarSlotsPriceConfig.Value = Mathf.Max(purchasableHotbarSlotsPriceConfig.Value, 1);
            purchasableHotbarSlotsPriceIncreaseConfig.Value = Mathf.Max(purchasableHotbarSlotsPriceIncreaseConfig.Value, 0);

            TryRemoveOldConfigSettings();
        }

        public static ConfigEntry<T> AddConfigEntry<T>(ConfigEntry<T> configEntry)
        {
            currentConfigEntries.Add(configEntry.Definition.Key, configEntry);
            return configEntry;
        }


        public static void TryRemoveOldConfigSettings()
        {
            HashSet<string> headers = new HashSet<string>();
            HashSet<string> keys = new HashSet<string>();

            foreach (ConfigEntryBase entry in currentConfigEntries.Values)
            {
                headers.Add(entry.Definition.Section);
                keys.Add(entry.Definition.Key);
            }

            try
            {
                ConfigFile config = Plugin.instance.Config;
                string filepath = config.ConfigFilePath;

                if (File.Exists(filepath))
                {
                    string contents = File.ReadAllText(filepath);
                    string[] lines = File.ReadAllLines(filepath); // Because contents.Split('\n') is adding strange characters...

                    string currentHeader = "";

                    for (int i = 0; i < lines.Length; i++)
                    {
                        lines[i] = lines[i].Replace("\n", "");
                        if (lines[i].Length <= 0)
                            continue;

                        if (lines[i].StartsWith("["))
                        {
                            if (currentHeader != "" && !headers.Contains(currentHeader))
                            {
                                currentHeader = "[" + currentHeader + "]";
                                int index0 = contents.IndexOf(currentHeader);
                                int index1 = contents.IndexOf(lines[i]);
                                contents = contents.Remove(index0, index1 - index0);
                            }
                            currentHeader = lines[i].Replace("[", "").Replace("]", "").Trim();
                        }

                        else if (currentHeader != "")
                        {
                            if (i <= (lines.Length - 4) && lines[i].StartsWith("##"))
                            {
                                int numLinesEntry = 1;
                                while (i + numLinesEntry < lines.Length && lines[i + numLinesEntry].Length > 3) // 3 because idc
                                    numLinesEntry++;

                                if (headers.Contains(currentHeader))
                                {
                                    int indexAssignOperator = lines[i + numLinesEntry - 1].IndexOf("=");
                                    string key = lines[i + numLinesEntry - 1].Substring(0, indexAssignOperator - 1);
                                    if (!keys.Contains(key))
                                    {
                                        int index0 = contents.IndexOf(lines[i]);
                                        int index1 = contents.IndexOf(lines[i + numLinesEntry - 1]) + lines[i + numLinesEntry - 1].Length;
                                        contents = contents.Remove(index0, index1 - index0);
                                    }
                                }
                                i += (numLinesEntry - 1);
                            }
                            else if (lines[i].Length > 3)
                                contents = contents.Replace(lines[i], "");
                        }
                    }

                    if (!headers.Contains(currentHeader))
                    {
                        currentHeader = "[" + currentHeader + "]";
                        int index0 = contents.IndexOf(currentHeader);
                        contents = contents.Remove(index0, contents.Length - index0);
                    }

                    while (contents.Contains("\n\n\n"))
                        contents = contents.Replace("\n\n\n", "\n\n");

                    File.WriteAllText(filepath, contents);
                    config.Reload();
                }
            }
            catch { } // Probably okay
        }
    }
}
