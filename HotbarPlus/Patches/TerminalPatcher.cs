using UnityEngine;
using HarmonyLib;
using HotbarPlus.Networking;

namespace HotbarPlus.Patches
{
    [HarmonyPatch]
    internal static class TerminalPatcher
    {
        public static Terminal terminalInstance;
        public static bool initializedTerminalNodes = false;

        private static bool inHotbarPlusTerminalMenu = false;
        private static bool purchasingHotbarSlot = false;
        internal static int nextHotbarSlotPrice { get { return SyncManager.purchasableHotbarSlotsPrice + SyncManager.purchasedHotbarSlots * SyncManager.purchasableHotbarSlotsPriceIncrease; } }

        [HarmonyPatch(typeof(Terminal), "Awake")]
        [HarmonyPrefix]
        public static void InitializeTerminal(Terminal __instance)
        {
            terminalInstance = __instance;
            initializedTerminalNodes = false;
            EditExistingTerminalNodes();
        }


        [HarmonyPatch(typeof(Terminal), "BeginUsingTerminal")]
        [HarmonyPrefix]
        public static void OnBeginUsingTerminal(Terminal __instance)
        {
            if (!initializedTerminalNodes && SyncManager.isSynced)
                EditExistingTerminalNodes();
        }


        public static void EditExistingTerminalNodes()
        {
            if (!SyncManager.isSynced)
                return;

            initializedTerminalNodes = true;

            if (SyncManager.purchasedHotbarSlots >= SyncManager.purchasableHotbarSlots)
                return;

            foreach (TerminalNode node in terminalInstance.terminalNodes.specialNodes)
            {
                if (node.name == "Start" && !node.displayText.Contains("[HotbarPlus]"))
                {
                    string keyword = "Type \"Help\" for a list of commands.";
                    int insertIndex = node.displayText.IndexOf(keyword);
                    if (insertIndex != -1)
                    {
                        insertIndex += keyword.Length;
                        string addText = "\n\n[HotbarPlus]\n" +
                            "Type \"HotbarPlus\" to buy additional hotbar slots.";
                        node.displayText = node.displayText.Insert(insertIndex, addText);
                    }
                    else
                        Plugin.LogError("Failed to add HotbarPlus tips to terminal. Maybe an update broke it?");
                }

                else if (node.name == "HelpCommands" && !node.displayText.Contains(">HOTBARPLUS"))
                {
                    string keyword = "[numberOfItemsOnRoute]";
                    int insertIndex = node.displayText.IndexOf(keyword);
                    if (insertIndex != -1)
                    {
                        string addText = ">HOTBARPLUS\n";
                        addText += "Purchase additional hotbar slots.\n\n";
                        node.displayText = node.displayText.Insert(insertIndex, addText);
                    }
                }
            }
        }


        /*[HarmonyPatch(typeof(Terminal), "TextPostProcess")]
        [HarmonyPrefix]
        public static void TextPostProcess(ref string modifiedDisplayText, TerminalNode node)
        {
            if (modifiedDisplayText.Length <= 0)
                return;

            string placeholderText = "[[[hotbarPlus]]]";
            if (modifiedDisplayText.Contains(placeholderText))
            {
                int index0 = modifiedDisplayText.IndexOf(placeholderText);
                int index1 = index0 + placeholderText.Length;
                string textToReplace = modifiedDisplayText.Substring(index0, index1 - index0);
                string replacementText = "";
                if (SyncManager.purchasableHotbarSlots <= 0)
                    replacementText += "Purchasing additional hotbar slots is not enabled.\n\n";
                else if (SyncManager.purchasedHotbarSlots >= SyncManager.purchasableHotbarSlots)
                    replacementText += "You have purchased all of the additional hotbar slots.\n\n";
                else
                {
                    replacementText += "To purchase an additional hotbar slot, type the following command.\n" +
                        "> HOTBARPLUS BUY\n\n";
                }
                modifiedDisplayText = modifiedDisplayText.Replace(textToReplace, replacementText);
            }
        }*/




        [HarmonyPatch(typeof(Terminal), "ParsePlayerSentence")]
        [HarmonyPrefix]
        public static bool ParsePlayerSentence(ref TerminalNode __result, Terminal __instance)
        {
            if (__instance.screenText.text.Length <= 0)
            {
                inHotbarPlusTerminalMenu = false;
                purchasingHotbarSlot = false;
                return true;
            }

            string input = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded).ToLower();
            string[] args = input.Split(' ');

            if (!SyncManager.isSynced)
            {
                if (input.StartsWith("hotbarplus"))
                {
                    __result = BuildTerminalNodeHostDoesNotHaveMod();
                    return false;
                }
                else
                    return true;
            }


            if (purchasingHotbarSlot)
            {
                inHotbarPlusTerminalMenu = false;
                purchasingHotbarSlot = false;
                if ("confirm".StartsWith(input))
                {
                    if (SyncManager.purchasableHotbarSlots <= 0)
                    {
                        Plugin.LogWarning("Attempted to purchase additional hotbar slot while this is not enabled by the host.");
                        __result = BuildTerminalNodePurchasingSlotsNotEnabled();
                    }
                    else if (SyncManager.purchasedHotbarSlots >= SyncManager.purchasableHotbarSlots)
                    {
                        Plugin.LogWarning("Attempted to purchase additional hotbar slot while the maximum slots have already been purchased.");
                        __result = BuildTerminalNodeMaxHotbarSlotsPurchased();
                    }
                    else if (terminalInstance.groupCredits < nextHotbarSlotPrice)
                    {
                        Plugin.LogWarning("Attempted to purchase additional hotbar slot with insufficient credits. Current credits: " + terminalInstance.groupCredits + " Required credits: " + nextHotbarSlotPrice);
                        __result = BuildTerminalNodeInsufficientFunds();
                    }
                    else
                    {
                        Plugin.Log("Purchasing additional hotbar slot for " + nextHotbarSlotPrice + " credits. New num hotbar slots purchased: " + (SyncManager.purchasedHotbarSlots + 1));
                        terminalInstance.groupCredits -= nextHotbarSlotPrice;
                        terminalInstance.BuyItemsServerRpc(new int[0], terminalInstance.groupCredits, terminalInstance.numberOfItemsInDropship);

                        if (terminalInstance.groupCredits < nextHotbarSlotPrice + SyncManager.purchasableHotbarSlotsPriceIncrease && SyncManager.purchasedHotbarSlots + 1 < SyncManager.purchasableHotbarSlots)
                            inHotbarPlusTerminalMenu = true;

                        __result = BuildTerminalNodeOnPurchased(terminalInstance.groupCredits);
                        SyncManager.SendPurchaseHotbarSlotToServer(SyncManager.purchasedHotbarSlots + 1);
                    }
                }
                else
                {
                    Plugin.Log("Canceling order.");
                    __result = BuildCustomTerminalNode("Canceled order.\n\n");
                }
                return false;
            }
            purchasingHotbarSlot = false;

            if (args.Length > 0 && ((args[0] == "hotbarplus" || args[0] == "hotbar") || (inHotbarPlusTerminalMenu && ("buy".StartsWith(args[0]) || "purchase".StartsWith(args[0])))))
            {
                if (args[0] == "hotbarplus" || args[0] == "hotbar")
                {
                    if (args.Length == 1)
                    {
                        __result = BuildTerminalNodeHome();
                        inHotbarPlusTerminalMenu = true;
                        return false;
                    }
                    else if (!"buy".StartsWith(args[1]) && !"purchase".StartsWith(args[1]))
                    {
                        __result = BuildCustomTerminalNode("Invalid command. Type \"HotbarPlus\" to view the HotbarPlus terminal menu.\n\n");
                        return false;
                    }
                }

                if (SyncManager.purchasableHotbarSlots <= 0)
                {
                    Plugin.LogWarning("Attempted to purchase additional hotbar slot, but the host does not have this setting enabled.");
                    __result = BuildTerminalNodePurchasingSlotsNotEnabled();
                }
                else if (SyncManager.purchasedHotbarSlots >= SyncManager.purchasableHotbarSlots)
                {
                    Plugin.LogWarning("Attempted to purchase additional hotbar slot while the maximum slots have already been purchased.");
                    __result = BuildTerminalNodeMaxHotbarSlotsPurchased();
                }
                else if (terminalInstance.groupCredits < nextHotbarSlotPrice)
                {
                    Plugin.LogWarning("Attempted to purchase additional hotbar slot with insufficient credits. Current credits: " + terminalInstance.groupCredits + " Required credits: " + nextHotbarSlotPrice);
                    __result = BuildTerminalNodeInsufficientFunds();
                }
                else
                {
                    Plugin.Log("Starting purchase on additional hotbar slot for " + nextHotbarSlotPrice + " credits.");
                    __result = BuildTerminalNodeConfirmDenyPurchase();
                    purchasingHotbarSlot = true;
                }
                inHotbarPlusTerminalMenu = false;
                return false;
            }
            inHotbarPlusTerminalMenu = false;
            return true;
        }


        private static TerminalNode BuildTerminalNodeHome()
        {
            TerminalNode homeTerminalNode = new TerminalNode
            {
                displayText = "[HotbarPlus]\n\n",
                clearPreviousText = true,
                acceptAnything = false
            };

            if (SyncManager.purchasableHotbarSlots <= 0)
                homeTerminalNode.displayText += "Additional hotbar slots cannot be purchased.\n\nHost does not have this setting enabled.\n\n";
            else if (SyncManager.purchasedHotbarSlots >= SyncManager.purchasableHotbarSlots)
                homeTerminalNode.displayText += "Additional hotbar slots cannot be purchased.\n\nMaximum purchasable slots has been reached.\n\n";
            else
                homeTerminalNode.displayText += "Purchase additional hotbar slot: $" + nextHotbarSlotPrice + "\n> PURCHASE\n\n";

            return homeTerminalNode;
        }


        private static TerminalNode BuildTerminalNodeConfirmDenyPurchase()
        {
            TerminalNode terminalNode = new TerminalNode
            {
                displayText = "You have requested to purchase an additional hotbar slot for $" + nextHotbarSlotPrice + " credits.\n\n",
                isConfirmationNode = true,
                acceptAnything = false,
                clearPreviousText = true
            };

            terminalNode.displayText += "Credit balance: $" + terminalInstance.groupCredits + "\n";
            terminalNode.displayText += "Current purchased hotbar slots: (" + SyncManager.purchasedHotbarSlots + "/" + SyncManager.purchasableHotbarSlots + ")\n\n";
            terminalNode.displayText += "Please CONFIRM or DENY.\n\n";

            return terminalNode;
        }


        private static TerminalNode BuildTerminalNodeOnPurchased(int newGroupCredits)
        {
            TerminalNode terminalNode = new TerminalNode
            {
                displayText = "You have successfully purchased an additional hotbar slot!\n\n" +
                "New main hotbar size: " + (SyncManager.currentHotbarSize + 1) + "\n" +
                "New credit balance: $" + newGroupCredits + "\n\n",
                buyUnlockable = true,
                clearPreviousText = true,
                acceptAnything = false,
                playSyncedClip = 0
            };

            if (terminalInstance.groupCredits < nextHotbarSlotPrice + SyncManager.purchasableHotbarSlotsPriceIncrease && SyncManager.purchasedHotbarSlots + 1 < SyncManager.purchasableHotbarSlots)
                terminalNode.displayText += "Purchase additional hotbar slot: $" + (nextHotbarSlotPrice + 1) + "\n> PURCHASE\n\n";

            return terminalNode;
        }


        private static TerminalNode BuildTerminalNodePurchasingSlotsNotEnabled()
        {
            TerminalNode terminalNode = new TerminalNode
            {
                displayText = "Cannot purchase additional hotbar slots.\n\nHost does not have this setting enabled.\n\n",
                clearPreviousText = false,
                acceptAnything = false
            };

            return terminalNode;
        }


        private static TerminalNode BuildTerminalNodeMaxHotbarSlotsPurchased()
        {
            TerminalNode terminalNode = new TerminalNode
            {
                displayText = "Cannot purchase more hotbar slots.\n\n" +
                "Maximum purchasable slots has been reached.\n\n",
                clearPreviousText = false,
                acceptAnything = false
            };

            return terminalNode;
        }


        private static TerminalNode BuildTerminalNodeInsufficientFunds()
        {
            TerminalNode terminalNode = new TerminalNode
            {
                displayText = "You cannot afford to purchase an additional hotbar slot.\n\n" +
                "Credit balance is $" + terminalInstance.groupCredits + "\n" +
                "Additional hotbar slot price: " + nextHotbarSlotPrice + "\n\n",
                clearPreviousText = true,
                acceptAnything = false
            };

            return terminalNode;
        }


        private static TerminalNode BuildTerminalNodeHostDoesNotHaveMod()
        {
            TerminalNode terminalNode = new TerminalNode
            {
                displayText = "You cannot use purchase additional hotbar slots until you have synced with the host.\n\n" +
                    "You may also be seeing this because the host does not have this mod.\n\n",
                clearPreviousText = true,
                acceptAnything = false
            };
            return terminalNode;
        }


        private static TerminalNode BuildCustomTerminalNode(string displayText, bool clearPreviousText = false, bool acceptAnything = false, bool isConfirmationNode = false)
        {
            TerminalNode terminalNode = new TerminalNode
            {
                displayText = displayText,
                clearPreviousText = clearPreviousText,
                acceptAnything = false,
                isConfirmationNode = isConfirmationNode
            };
            return terminalNode;
        }
    }
}