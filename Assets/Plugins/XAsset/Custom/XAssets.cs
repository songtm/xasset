using System;
using System.Collections.Generic;
using System.Diagnostics;
using Plugins.XAsset;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Debug = UnityEngine.Debug;

namespace XAsset.Plugins.XAsset.Custom
{
    public static class XAssets
    {
        private static HashSet<string> _assetSet;
        public static void Initialize(Action onSuccess, Action<string> onError)
        {
            Bundles.OverrideBaseDownloadingUrl += bundleName =>
            {
                //todo 自定义bunel加载路径! 是远程下载还是cache,还是安装包里
                return null;
            };

            //todo bundle异步加载分发策略 目前web bundle好像不能控制下载缓存路径什么的,要不要写一个httpBundle?
            //Bundles.OverrideBundleDispater

            InitEditorAssetLoader();

            Assets.Initialize(onSuccess, onError);
        }

        [Conditional("UNITY_EDITOR")]
        private static void InitEditorAssetLoader()
        {
            if (!Utility.assetBundleMode)
            {
                _assetSet = new HashSet<string>();
                AssetsManifest manifest = AssetDatabase.LoadAssetAtPath<AssetsManifest>(Utility.AssetsManifestAsset);
                if (manifest == null)
                {
                    Debug.LogError("Pls gen manifest first:<");
                }

                string[] dirs = manifest.dirs;
                foreach (var assetData in manifest.assets)
                {
                    _assetSet.Add(dirs[assetData.dir] + "/" + assetData.name);
                }
            }

            Utility.loadDelegate = (assetPath, type) =>
            {
                if (Utility.assetBundleMode)
                    Debug.LogError("bundle mode load with editor: " + assetPath);
                else
                {
                    if (!_assetSet.Contains(assetPath))
                    {
                        Debug.LogWarning("Pls config/update manifest for " + assetPath);
                    }
                }
                return AssetDatabase.LoadAssetAtPath(assetPath, type);
            };
        }
    }
}