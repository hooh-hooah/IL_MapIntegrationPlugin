using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AIProject;
using BepInEx.Logging;
using HarmonyLib;
using MapIntegration;
using UnityEngine;
using Resources = Manager.Resources;

/*
 * todo: manage map links
 * todo: make it more easier to make and manager
 * todo: custom map ui
 *      todo: character migration ui
 *      todo: teleport ui
 */

namespace MapIntegrationPluginComponents
{
    public static class MapHooks
    {
        public static void InstallHooks()
        {
            var harmony = Harmony.CreateAndPatchAll(typeof(MapHooks));

            // When all of resource pack has been loaded.
            harmony.Patch(typeof(Resources.MapTables).GetMethod("Load"), null, new HarmonyMethod(typeof(MapHooks), nameof(PostLoadResources)));
            harmony.Patch(AccessTools.Method(typeof(Manager.Housing).GetNestedTypes(BindingFlags.NonPublic).FirstOrDefault(x => x.Name.StartsWith("<LoadHousing>")), "MoveNext"),
                null, null, new HarmonyMethod(typeof(MapHooks), nameof(TranspileMapInfo)));
        }

        public static IEnumerable<CodeInstruction> TranspileMapInfo(IEnumerable<CodeInstruction> instructions)
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

        public static void InjectMapID(object instance)
        {
            var traverse = Traverse.Create(instance);
            var id = traverse.Field("_type").GetValue<int>();
            if (id <= Data.RESERVED_ID) return;
            if (!Data.CustomMapInformations.TryGetValue(id, out var info)) return;
            traverse.Field("$locvar1").Field("keys").SetValue(info.Zones.ToArray());
        }

        internal static void PostLoadResources()
        {
            var instance = Singleton<Resources>.Instance.Map;
            if (instance == null) Logger.LogError("Something went wrong!!");

            foreach (var info in Data.CustomMapInformations.Values)
                info.Register(instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Resources.HSceneTables), "LoadAutoHPoint", typeof(int))]
        public static void LoadHPoints(Resources.HSceneTables __instance, int mapID)
        {
            if (__instance.hPointLists.ContainsKey(mapID)) return;
            __instance.hPointLists.Add(mapID, new GameObject("HPointLists").AddComponent<HPointList>());
            __instance.hPointLists[mapID].lst = new Dictionary<int, List<HPoint>>();
            //maybe automatically generate all possible hpoints from the list or the other shits idk maybe.
        }

        public static ManualLogSource Logger { get; set; }
    }
}
