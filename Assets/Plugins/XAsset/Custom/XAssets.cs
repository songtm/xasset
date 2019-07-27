using System;
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
        public static void Initialize(Action onSuccess, Action<string> onError)
        {
            Bundles.OverrideBaseDownloadingUrl += bundleName =>
            {
                //todo 自定义bunel加载路径! 是远程下载还是cache,还是安装包里
                return null;
            };

            //Bundles.OverrideBundleDispater //todo bundle异步加载分发策略
            InitEditorAssetLoader();

            Assets.Initialize(onSuccess, onError);
        }

        [Conditional("UNITY_EDITOR")]
        private static void InitEditorAssetLoader()
        {
            Utility.loadDelegate = (assetPath, type) =>
            {
                if (Utility.assetBundleMode) Debug.LogError("bundle mode load with editor: " + assetPath);
                return AssetDatabase.LoadAssetAtPath(assetPath, type);
            };
        }
    }
}