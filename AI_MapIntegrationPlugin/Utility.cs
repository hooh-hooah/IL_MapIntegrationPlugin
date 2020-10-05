using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using BepInEx.Logging;
using MapIntegration.Utility;
using UnityEx;

namespace MapIntegration.Utility
{
    public static class Integration
    {
        public struct AssetBundleValues
        {
            public string AssetBundle;
            public string Asset;
            public string Name;
            public string Manifest;

            public static AssetBundleValues FromXML(XElement element)
            {
                return new AssetBundleValues
                {
                    Asset = element.Attribute("asset")?.Value,
                    AssetBundle = element.Attribute("bundle")?.Value,
                    Manifest = element.Attribute("manifest")?.Value,
                    Name = element.Attribute("name")?.Value
                };
            }
        }

        public static ManualLogSource Logger { get; set; }

        public static bool TryParseXML(AssetBundleValues defaultValues, XElement element, out AssetBundleInfo data)
        {
            data = default;
            if (element == null) return false;

            data = new AssetBundleInfo
            {
                asset = element.Attribute("asset")?.Value ?? defaultValues.Asset,
                assetbundle = element.Attribute("bundle")?.Value ?? defaultValues.AssetBundle,
                manifest = element.Attribute("manifest")?.Value ?? defaultValues.Manifest,
                name = element.Attribute("name")?.Value ?? defaultValues.Name
            };
            return true;
        }


        // TODO: Map linking???
        public static void RegisterMapData(XElement dataXML)
        {
            Logger.LogError("Got some bullshit");
            if (dataXML == null) return;
            var defaultValue = AssetBundleValues.FromXML(dataXML);
            var id = int.Parse(dataXML.Attribute("id")?.Value ?? "-1");
            Logger.LogError(id);
            Logger.LogError(defaultValue.Name);
            if (id <= 50) return; // just in case
            foreach (var bundleGroup in dataXML.Elements())
            {
                var tagName = bundleGroup.Name.LocalName;

                if (!Data.MapInformation.ContainsKey(id)) Data.MapInformation[id] = new Dictionary<string, List<AssetBundleInfo>>();
                if (!Data.MapInformation[id].ContainsKey(tagName)) Data.MapInformation[id][tagName] = new List<AssetBundleInfo>();

                foreach (var info in bundleGroup.Elements("bundle"))
                {
                    if (!TryParseXML(defaultValue, info, out var assetBundleInfo)) continue;
                    Data.MapInformation[id][tagName].Add(assetBundleInfo);
                    Logger.LogDebug($"Registered {id}, {tagName}, {assetBundleInfo.ToString()}");
                }
            }
        }
    }
}