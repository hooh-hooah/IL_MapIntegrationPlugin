using System.Collections.Generic;
using UnityEngine;
using UnityEx;

namespace MapIntegration
{
    public static class Data
    {
        public const int RESERVED_ID = 50;
        public static Dictionary<int, Dictionary<string, List<AssetBundleInfo>>> MapInformation = new Dictionary<int, Dictionary<string, List<AssetBundleInfo>>>();
        public static Dictionary<int, Vector3> RegisteredBuildZones = new Dictionary<int, Vector3>()
        {
            {69000001, new Vector3(1000f, 500f, 1000f)}
        };
    }
}