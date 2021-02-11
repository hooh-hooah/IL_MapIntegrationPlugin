using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using UnityEngine;
using UnityEx;
using Resources = Manager.Resources;

namespace MapIntegration
{
    // contains informtaions
    public struct CustomMapInformation
    {
        public int ID;
        public string Name;
        public List<int> Zones;
        public HashSet<int> Nodes;
        public Dictionary<string, List<AssetBundleInfo>> Bundles;

        public CustomMapInformation(int id, string name, string manifest = "abdata")
        {
            ID = id;
            Name = name;
            /*
             * will be used for establishing links between the map nodes.
             * maybe add something later?
             */
            Zones = new List<int>();
            Nodes = new HashSet<int>();
            Bundles = new Dictionary<string, List<AssetBundleInfo>>()
            {
                {"map", new List<AssetBundleInfo>()},
                {"navmesh", new List<AssetBundleInfo>()},
                {"action-point", new List<AssetBundleInfo>()},
                {"base-point", new List<AssetBundleInfo>()},
                {"device-point", new List<AssetBundleInfo>()},
                {"ship-point", new List<AssetBundleInfo>()},
                {"chunk", new List<AssetBundleInfo>()},
                {"cam-collider", new List<AssetBundleInfo>()},
                {"merchant-point", new List<AssetBundleInfo>()}
            };
        }

        public bool CanAccess(int mapID)
        {
            return true;
        }


        public void Register(Resources.MapTables instances)
        {
            // maybe add default ...?

            var id = ID;
            Bundles["map"].ForEach(x => instances.MapList.Add(id, x));
            Bundles["navmesh"].ForEach(x => instances.NavMeshSourceList.Add(id, x));
            Bundles["action-point"].ForEach(x => instances.ActionPointGroupTable.Add(id, x));
            Bundles["base-point"].ForEach(x => instances.BasePointGroupTable.Add(id, x));
            Bundles["device-point"].ForEach(x => instances.DevicePointGroupTable.Add(id, x));
            Bundles["ship-point"].ForEach(x => instances.ShipPointGroupTable.Add(id, x));
            Bundles["chunk"].ForEach(x => instances.ChunkList.Add(id, x));
            Bundles["cam-collider"].ForEach(x => instances.CameraColliderList.Add(id, x));
            // idk why illusion pulled of this one
            var merchantIndex = 0;
            instances.MerchantPointGroupTable[id] = Bundles["merchant-point"].ToDictionary(x => merchantIndex++, x => x);
        }

        public static bool TryMakeFromXML(XElement xml, out CustomMapInformation result)
        {
            result = default;
            if (xml == null) return false;

            var info = new CustomMapInformation(
                int.Parse(xml.Attribute("id")?.Value ?? "-1"),
                xml.Attribute("name")?.Value ?? "Invalid Name"
            );
            if (info.ID < Data.RESERVED_ID) return false;

            foreach (var bundle in xml.Elements("bundle"))
            {
                var type = bundle.Attribute("type")?.Value ?? "0";
                if (!info.Bundles.TryGetValue(type, out var list)) continue;
                list.Add(new AssetBundleInfo
                {
                    assetbundle = bundle.Attribute("asset-bundle")?.Value ?? "0",
                    asset = bundle.Attribute("asset")?.Value ?? "0",
                    manifest = bundle.Attribute("manifest")?.Value ?? "abdata",
                    name = bundle.Attribute("name")?.Value,
                });
            }

            foreach (var zone in xml.Elements("housing-zone"))
                if (int.TryParse(zone.Attribute("id")?.Value, out int zoneID) && zoneID > 5)
                    info.Zones.Add(zoneID);

            result = info;
            return true;
        }
    }

    public static class Data
    {
        public static Dictionary<int, CustomMapInformation> CustomMapInformations = new Dictionary<int, CustomMapInformation>();
        public const int RESERVED_ID = 50;
    }
}
