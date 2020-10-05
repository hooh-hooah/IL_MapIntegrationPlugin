using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using MapIntegration.Utility;
using MapIntegrationPluginComponents;

[BepInPlugin(Constant.GUID, "AI_MapIntegrationPlugin", Constant.VERSION)]
[BepInDependency(Sideloader.Sideloader.GUID)]
public class MapIntegrationPlugin : BaseUnityPlugin
{
    public const string GUID = "com.hooh.heelz";
    public const string VERSION = "1.13.0";

    internal new static ManualLogSource Logger;

    private void Start()
    {
        Logger = base.Logger;
        HarmonyLib.Harmony.CreateAndPatchAll(typeof(MapHooks));
        Integration.Logger = Logger;
        MapHooks.Logger = Logger;
        MapHooks.InitializeOpcodePatch();

        foreach (var xDocument in Sideloader.Sideloader.Manifests.Values.Where(x => x.manifestDocument != null).Select(x => x.manifestDocument))
        {
            if (xDocument.Root == null) continue;
            var root = xDocument.Element("manifest");
            if (root == null) continue;

            foreach (var maps in root.Elements("ai-maps"))
            {
                foreach (var data in maps.Elements("map-data"))
                {
                    Integration.RegisterMapData(data);
                }
            }
        }
    }
}