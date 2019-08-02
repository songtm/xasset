using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Plugins.XAsset;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Debug = UnityEngine.Debug;
using Utility = Plugins.XAsset.Utility;

namespace XAsset.Plugins.XAsset.Custom
{
    public static class XAssets
    {
        private static HashSet<string> _assetSet;
        private static MonoBehaviour owner;

        public static void Initialize(MonoBehaviour monoBehaviour, Action onSuccess, Action<string> onError)
        {
            owner = monoBehaviour;
            WebBundleEx.downloadCheck = false;//下载 bundle 的时候是否校验(todo 测试计算文件 shasum会卡?)
            BundlePathDelegate.Initialize(() =>
            {
                BundleDispatcher.Initialize(true, 1, 3);
                AssetAsyncDispatcher.Initialize(true, 30);
                InitEditorAssetLoader();

                Assets.Initialize(onSuccess, onError);
            }, onError);
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

        private static IEnumerator GetText(string url, Action<string> callback)
        {
            using (UnityWebRequest www = new UnityWebRequest(url))
            {
                www.downloadHandler = new DownloadHandlerBuffer();
                yield return www.SendWebRequest();
                if (!string.IsNullOrEmpty(www.error))
                {
                    Debug.LogError("error: load url:" + url + " " + www.error);
                    callback(null);
                }
                else
                    callback(www.downloadHandler.text);
            }
        }

        public static void GetTextFromApp(string path, Action<string> callback)
        {
            var fromDataPath = Utility.GetWebUrlFromDataPath(path);
            owner.StartCoroutine(GetText(fromDataPath, callback));
        }

        public static void GetTextFromCacheOrApp(string path, Action<string> callback)
        {
            var cachePath = Utility.GetRelativePath4Update(path);
            if (!File.Exists(cachePath))
            {
                GetTextFromApp(path, callback);
            }
            else
            {
                callback(File.ReadAllText(cachePath));
            }
        }
    }
}