using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using AIProject;
using AIProject.SaveData;
using AIProject.UI;
using BepInEx.Logging;
using HarmonyLib;
using Housing;
using MapIntegration;
using UnityEngine;
using UnityEx;
using Resources = Manager.Resources;

namespace MapIntegrationPluginComponents
{
    public static class MapHooks
    {
        public static void InitializeOpcodePatch()
        {
            var harmony = new Harmony("AI_MapIntergrationHarmony");
            harmony.Patch(
                AccessTools.Method(typeof(Manager.Housing).GetNestedType("<LoadHousing>c__Iterator1", AccessTools.all), "MoveNext"),
                null,
                null,
                new HarmonyMethod(typeof(MapHooks), nameof(OpCodeMapInfo))
            );

            /*harmony.Patch(
                AccessTools.Method(typeof(Manager.Housing).GetNestedType("<LoadExcelDataCoroutine>c__Iterator0", AccessTools.all), "MoveNext"),
                null,
                null,
                new HarmonyMethod(typeof(MapHooks), nameof(OpCodeHousingData))
            );*/

            harmony.Patch(
                AccessTools.Method(typeof(CharaMigrateUI).GetNestedType("<OnBeforeStart>c__AnonStorey6", AccessTools.all), "<>m__0"),
                null,
                null,
                new HarmonyMethod(typeof(MapHooks), nameof(OpCodeMigrateUI))
            );
        }


        /*[HarmonyPostfix]
        [HarmonyPatch(typeof(HousingData), "UpdateDiff")]
        public static void RegisterHousingData(HousingData __instance)
        {
            if (!Singleton<Manager.Housing>.IsInstance())
            {
                foreach (var pairs in Data.RegisteredBuildZones)
                {
                    var id = pairs.Key;
                    var size = pairs.Value;
                    if (!__instance.CraftInfos.ContainsKey(id))
                        __instance.CraftInfos.Add(id, new CraftInfo(size, id));
                }
            }
            else
            {
                foreach (var areaInfo in
                    Singleton<Manager.Housing>.Instance.dicAreaInfo
                        .Where(areaInfo => !__instance.CraftInfos.ContainsKey(areaInfo.Key)))
                {
                    var v2 = (!Singleton<Manager.Housing>.Instance.dicAreaSizeInfo.TryGetValue(areaInfo.Value.size, out var areaSizeInfo))
                        ? new Vector3Int(100, 80, 100)
                        : areaSizeInfo.limitSize;
                    var value = new CraftInfo(v2, areaInfo.Value.no);
                    __instance.CraftInfos.Add(areaInfo.Key, value);
                }
            }
        }*/


        public static IEnumerable<CodeInstruction> OpCodeMigrateUI(IEnumerable<CodeInstruction> instructions)
        {
            // just stop
            var il = instructions.ToList();
            il.Insert(0, new CodeInstruction(OpCodes.Ret));
            return il;
        }

        public static void InjectMapID(object instance)
        {
            var traverse = Traverse.Create(instance);
            var _type = traverse.Field("_type").GetValue<int>();
            if (_type <= Data.RESERVED_ID) return;
            traverse.Field("$locvar1").Field("keys").SetValue(Data.RegisteredBuildZones.Keys.ToArray());
        }

        /*public static void InjectMapInfo(object instance)
        {
            var managerInstance = Singleton<Manager.Housing>.Instance;
            if (managerInstance == null) return;

            foreach (var pairs in Data.RegisteredBuildZones)
            {
                managerInstance.dicAreaInfo.Add(pairs.Key, new Manager.Housing.AreaInfo(new List<string>()
                {
                    pairs.Key.ToString(),
                    "1", //idk what this is yet.
                    "housing/base/00.unity3d", //preset bundle
                    "housing02" // preset asset
                }));
                managerInstance.dicAreaSizeInfo.Add(pairs.Key, new Manager.Housing.AreaSizeInfo(new List<string>()
                {
                    pairs.Key.ToString(),
                    Mathf.RoundToInt(pairs.Value.x).ToString(),
                    Mathf.RoundToInt(pairs.Value.y).ToString(),
                    Mathf.RoundToInt(pairs.Value.z).ToString(),
                    // compatibility. for now, it's not compatible to others.
                    pairs.Key.ToString()
                    // string.join("/", compatArray)
                }));
            }
        }

        public static IEnumerable<CodeInstruction> OpCodeHousingData(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Call && (instruction.operand as MethodInfo)?.Name == "set_IsLoadList");
            if (index <= 0) return il;

            il.InsertRange(index, new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MapHooks), nameof(InjectMapInfo)))
            });

            return il;
        }*/

        public static IEnumerable<CodeInstruction> OpCodeMapInfo(IEnumerable<CodeInstruction> instructions)
        {
            var il = instructions.ToList();
            var index = il.FindIndex(instruction => instruction.opcode == OpCodes.Callvirt && (instruction.operand as MethodInfo)?.Name == "get_CraftInfos");
            if (index <= 0) return il;

            il.InsertRange(index - 2, new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MapHooks), nameof(InjectMapID))),
                new CodeInstruction(OpCodes.Ldarg_0)
            });

            return il;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Resources.HSceneTables), "LoadAutoHPoint", typeof(int))]
        public static void LoadHPoints(Resources.HSceneTables __instance, int mapID)
        {
            if (__instance.hPointLists.ContainsKey(mapID)) return;
            __instance.hPointLists.Add(mapID, new GameObject("HPointLists").AddComponent<HPointList>());
            __instance.hPointLists[mapID].lst = new Dictionary<int, List<HPoint>>();

            //maybe automatically generate all possible hpoints from the list or the other shits idk maybe. 
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Resources.MapTables), "Load", typeof(DefinePack))]
        public static void OnMapInfoLoaded(Resources.MapTables __instance, DefinePack definePack)
        {
            var instance = Singleton<Resources>.Instance.Map;
            foreach (var idPair in Data.MapInformation)
            {
                var mapID = idPair.Key;
                var data = idPair.Value;

                foreach (var mapInfo in data)
                {
                    var type = mapInfo.Key;
                    var bundles = mapInfo.Value;

                    switch (type)
                    {
                        case "map":
                            foreach (var assetBundleInfo in bundles) instance.MapList.Add(mapID, assetBundleInfo);
                            break;
                        case "navmesh":
                            foreach (var assetBundleInfo in bundles) instance.NavMeshSourceList.Add(mapID, assetBundleInfo);
                            break;
                        case "action-point":
                            foreach (var assetBundleInfo in bundles) instance.ActionPointGroupTable.Add(mapID, assetBundleInfo);
                            break;
                        case "base-point":
                            foreach (var assetBundleInfo in bundles) instance.BasePointGroupTable.Add(mapID, assetBundleInfo);
                            break;
                        case "device-point":
                            foreach (var assetBundleInfo in bundles) instance.DevicePointGroupTable.Add(mapID, assetBundleInfo);
                            break;
                        case "ship-point":
                            foreach (var assetBundleInfo in bundles) instance.ShipPointGroupTable.Add(mapID, assetBundleInfo);
                            break;
                        case "chunk":
                            foreach (var assetBundleInfo in bundles) instance.ChunkList.Add(mapID, assetBundleInfo);
                            break;
                        case "cam-collider":
                            foreach (var assetBundleInfo in bundles) instance.CameraColliderList.Add(mapID, assetBundleInfo);
                            break;
                        case "merchant-point":
                            foreach (var assetBundleInfo in bundles)
                            {
                                instance.MerchantPointGroupTable.Add(mapID, new Dictionary<int, AssetBundleInfo>()
                                {
                                    {0, assetBundleInfo}
                                });
                            }

                            break;
                    }
                }
            }
        }

        public static ManualLogSource Logger { get; set; }
    }
}