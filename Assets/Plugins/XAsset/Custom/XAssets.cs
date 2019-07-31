using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Plugins.XAsset;
using UnityEngine;
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
            BundlePathDelegate.Initialize(() =>
            {
                //todo bundle异步加载分发策略 目前web bundle好像不能控制下载缓存路径什么的,要不要写一个httpBundle?
                BundleDispatcher.Initialize(true, 2, 4);
                AssetAsyncDispatcher.Initialize(true, 40);
                InitEditorAssetLoader();

                Assets.Initialize(onSuccess, onError);
            });
        }

        private static void InitEditorAssetLoader()
        {
#if UNITY_EDITOR
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
#endif
        }

        public static void GetTextFromApp(string path, bool async, Action<string> callback)
        {
            var fromDataPath = Utility.GetWebUrlFromDataPath(path);
            var asset = async
                ? Assets.LoadAsync(fromDataPath, typeof(TextAsset))
                : Assets.Load(fromDataPath, typeof(TextAsset));

            asset.completed += delegate
            {
                if (asset.error != null)
                {
                    Debug.LogError("Error: can't find file " + path);
                    callback(null);
                    return;
                }

                var dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, asset.text);
                callback(asset.text);
                asset.Release();
            };
        }
        //callback(string, isfromCache)
        public static void GetTextFromCacheOrApp(string path, bool async, Action<string, bool> callback)
        {
            path = Utility.GetRelativePath4Update(path);
            if (!File.Exists(path))
            {
                GetTextFromApp(path, async, s => callback(s, false));
            }
            else
            {
                callback(File.ReadAllText(path), true);
            }
        }
    }
}