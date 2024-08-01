using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;

namespace MoreHarvests
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static Harmony _harmony;
        internal static ManualLogSource Log;

        private static ConfigEntry<bool> _debugLogging;
        private static ConfigEntry<int> _extraBerryCount;
        private static ConfigEntry<int> _extraMiscCount;
        private static ConfigEntry<int> _extraCropCount;
        private static ConfigEntry<int> _extraTreeCount;
        private static ConfigEntry<int> _extraHerbCount;
        private static ConfigEntry<int> _extraRockCount;
        private static ConfigEntry<int> _extraTreeChopCount;

        public Plugin()
        {
            // bind to config settings
            _debugLogging       = Config.Bind("Debug", "Debug Logging", false, "Logs additional information to console");
            _extraBerryCount    = Config.Bind("General", "Extra Berry Count", 0, "Number of extra berries to generate (set to 0 to disable)");
            _extraMiscCount     = Config.Bind("General", "Extra Misc Count",  0, "Number of extra misc items (sticks, junk, mussels) to generate (set to 0 to disable)");
            _extraCropCount     = Config.Bind("General", "Extra Crop Count",  0, "Number of extra crops to generate (set to 0 to disable)");
            _extraTreeCount     = Config.Bind("General", "Extra Tree Count (Harvest)",  3, "Number of extra tree items to generate when harvesting (set to 0 to disable)");
            _extraHerbCount     = Config.Bind("General", "Extra Herb Count",  3, "Number of extra herbs to generate (set to 0 to disable)");
            _extraRockCount     = Config.Bind("General", "Extra Rock Count",  0, "Number of extra rocks to generate on each hit (set to 0 to disable)");
            _extraTreeChopCount = Config.Bind("General", "Extra Tree Multiplier (Chop)", 0, "Multiplier for items dropped when a tree is cut down; set to 0 to disable");
        }

        private void Awake()
        {
            // Plugin startup logic
            Log = base.Logger;
            _harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
        public static void DebugLog(string message)
        {
            // Log a message to console only if debug is enabled in console
            if (_debugLogging.Value)
            {
                Log.LogInfo(String.Format("NepMoreHarvests: Debug: {0}", message));
            }
        }

        //////////////////////////////////////////////////////////////////
        //  Rock (mineable rocks)

        [HarmonyPatch(typeof(Rock), "Chop")]
        [HarmonyPrefix]
        static void RockChopPrefix(Rock __instance)
        {
            if (_extraRockCount.Value > 0)
            {
                DebugLog("In Rock Chop Prefix");
                //Just going to drop the first item again on each chop. Should make this a random item from the array.
                Item firstRock = __instance.droppedItems[0].item;
                if (firstRock != null)
                {
                    DroppedItem.SpawnDroppedItem(__instance.gameObject.transform.position, firstRock, _extraRockCount.Value, false, false, 0);
                }
            }
        }

        //////////////////////////////////////////////////////////////////
        //  BushHarvest (berry bushes)

        [HarmonyPatch(typeof(BushHarvest), "MouseUp")]
        [HarmonyPostfix]
        static void BushHarvestMouseUpPostfix(BushHarvest __instance, ref bool __result)
        {
            DebugLog(String.Format("In BushHarvest MouseUp Postfix: return value: {0}", __result ? "TRUE" : "FALSE"));
            if (__result && (_extraBerryCount.Value > 0))
            {
                DroppedItem.SpawnDroppedItem(__instance.gameObject.transform.position, __instance.harvestedItems, _extraBerryCount.Value, false, false, 0);
            }
            return;
        }

        //////////////////////////////////////////////////////////////////
        //  MiscellaneousHarvest (sticks, junk, shellfish)

        [HarmonyPatch(typeof(MiscellaneousHarvest), "MouseUp")]
        [HarmonyPostfix]
        static void MiscellaneousHarvesttMouseUpPostfix(MiscellaneousHarvest __instance, ref bool __result)
        {
            DebugLog(String.Format("In MiscellaneousHarvest MouseUp Postfix: return value: {0}", __result ? "TRUE" : "FALSE"));
            if (__result && (_extraMiscCount.Value > 0))
            {
                DroppedItem.SpawnDroppedItem(__instance.gameObject.transform.position, __instance.harvestedItems.item, _extraMiscCount.Value, false, false, 0);
 
            }
            return;
        }


        //////////////////////////////////////////////////////////////////
        //  MiscItemHarvest (herbs)

        [HarmonyPatch(typeof(MiscItemHarvest), "MouseUp")]
        [HarmonyPostfix]
        static void MiscItemHarvestMouseUpPostfix(MiscItemHarvest __instance, ref bool __result)
        {
            DebugLog(String.Format("In MiscItemHarvest MouseUp Postfix: return value: {0}", __result ? "TRUE" : "FALSE"));
            if (__result && (_extraHerbCount.Value > 0))
            {
                DroppedItem.SpawnDroppedItem(__instance.gameObject.transform.position, __instance.harvestedItems.item, _extraHerbCount.Value, false, false, 0);

            }
            return;
        }



        //////////////////////////////////////////////////////////////////
        //  Harvestable (crops, trees)

        [HarmonyPatch(typeof(Harvestable), "HarvestAction")]
        [HarmonyPrefix]
        static void HarvestableHarvestActionPrefix(Harvestable __instance)
        {

            // The challenge is this method goes into this.HEMOKEKCEFD(LCJKCBNBMHN)to do the actual dropping of the crops
            // HEMOKEKCEFD : drops the items from this.harvestedItems[]
            //               drops items with a probability from this.harvestedItemsProb[]
            //               calls this.OLMBADPCGGC to actually place the items


            if (__instance.cropSetter != null)
            {
                if ((_extraCropCount.Value > 0) || (_extraTreeCount.Value > 0))
                {
                    bool isTree = __instance.cropSetter.IsTreeCrop();
                    bool isGrown = __instance.cropSetter.growable.grown;
                    bool isHarvestable = __instance.isHarvestable;

                    int extraItems = 0;


                    if (isGrown && isHarvestable) // we're harvesting a crop or tree
                    {
                        if (isTree)
                        {
                            DebugLog("HarvestAction Prefix: I think it's a tree");
                            extraItems = _extraTreeCount.Value;
                        }
                        else
                        {
                            DebugLog("HarvestAction Prefix: I think it's a crop");
                            extraItems = _extraCropCount.Value;
                        }

                    }
                    else
                    {
                        DebugLog("HarvestAction Prefix: No idea what is being harvested!");
                    }
                    DebugLog(String.Format("HarvestAction Prefix: Extra item count {0}", extraItems));
                    if (extraItems > 0)
                    {
                        for (int i = 0; i < __instance.harvestedItems.Length; i++)
                        {
                            // Need to get rid of random junk here or will break every patch
                            // item.PNPJANDHIBH(true) ==> item.id modified by BIJCAFKFIOG()
                            // Use Reflection to get the item id, then manually remakes the transformations from BIJCAFKFIOG
                            // (Random function names as of 2024-07029; by the time you read this they will have changed!

                            int reflectedItemID = 0;
                            reflectedItemID = Traverse.Create(__instance.harvestedItems[i].item).Field("id").GetValue<int>();
                            if (reflectedItemID != 0)
                            {

                                //This is the transformation ormally done by function BIJCAFKFIOG
                                if (reflectedItemID == 1224)
                                {
                                    reflectedItemID = 1226;
                                }
                                if (Utils.dictReplaceItems.ContainsKey(reflectedItemID))
                                {
                                    reflectedItemID = Utils.dictReplaceItems[reflectedItemID];
                                }
                                //

                                DebugLog(String.Format("HarvestAction Prefix: itemID {0}", reflectedItemID));
                                //Item item = ItemDatabaseAccessor.GetItem(Utils.BIJCAFKFIOG(this.harvestedItems[i].item.PNPJANDHIBH(true), false), false, true); <-- Original line with "fun" functions
                                Item item = ItemDatabaseAccessor.GetItem(reflectedItemID);
                                if (item != null)
                                {
                                    DroppedItem.SpawnDroppedItem(__instance.gameObject.transform.position, item, extraItems, false, false, 0);
                                }
                            }

                        }
                    }
                }

            }
        }

        //////////////////////////////////////////////////////////////////
        //  Tree (chop down)
        [HarmonyPatch(typeof(Tree), "SpawnDroppedItems")]
        [HarmonyPostfix]
        static void HarvestableHarvestActionPrefix(Tree __instance)
        {
            if (_extraTreeChopCount.Value >= 2)
            {
                for (int i=1;i< _extraTreeChopCount.Value; i++) //not an off-by-one error, the first round of items drops happens in the normal game code
                {
                    Vector3 x = __instance.gameObject.transform.position + new Vector3(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(-0.25f, 0.25f));
                    DroppedItem.SpawnDroppedItems(__instance.droppedItems, __instance.droppedItemsProb, x, true);
                }

            }

        }

        
    }
}
