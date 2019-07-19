using System.Collections.Generic;
using UnityEngine;

namespace Plugins.XAsset.Editor.AutoBundle
{
    public class AssetBundleBuildConfig : ScriptableObject
    {
        public enum GraphMode
        {
            MergeLink,
            NoMergeLink,
            ShowLinkName
        }

        public GraphMode graphMode = GraphMode.ShowLinkName;


        public List<AssetBundleFilter> filters = new List<AssetBundleFilter>();
    }

    public enum PackMode
    {
        Indepent,
        AllInOne,
        PerAnyDir,
        PerSubDir
    }
    [System.Serializable]
    public class AssetBundleFilter
    {
        public bool valid = true;
        public string path = string.Empty;
        public string filter = "*.prefab";
        public PackMode packMode = PackMode.Indepent;
    }
}