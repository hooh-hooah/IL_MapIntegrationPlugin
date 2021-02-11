using System.Linq;
using System.Xml.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MapIntegration;
using MapIntegrationPluginComponents;

[BepInPlugin(Constant.GUID, "AI_MapIntegrationPlugin", Constant.VERSION)]
[BepInDependency(Sideloader.Sideloader.GUID)]
public class MapIntegrationPlugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;

    private void Start()
    {
        Logger = base.Logger;
        MapHooks.Logger = Logger;
        MapHooks.InstallHooks();

        foreach (var xDocument in Sideloader.Sideloader.Manifests.Values.Where(x => x.manifestDocument != null).Select(x => x.manifestDocument))
        {
            if (xDocument.Root == null) continue;
            var root = xDocument.Element("manifest");
            if (root == null) continue;

            foreach (var data in root.Elements("ai-maps")?.Elements("map-data"))
            {
                if (!CustomMapInformation.TryMakeFromXML(data, out var result)) continue;
                Data.CustomMapInformations.Add(result.ID, result);
            }
        }

        Logger.LogWarning($"Map Integration Plugin found {Data.CustomMapInformations.Count} maps from the mod folder");
        foreach (var data in Data.CustomMapInformations.Values)
        {
            Logger.LogWarning($"Map Integration Plugin successfully Registered Custom Map {data.Name}({data.ID}).");
        }
    }
}
