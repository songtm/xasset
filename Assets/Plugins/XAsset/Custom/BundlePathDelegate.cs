using System;
using System.Collections.Generic;
using System.IO;
using Plugins.XAsset;
using UnityEngine;

namespace XAsset.Plugins.XAsset.Custom
{
    public static class BundlePathDelegate
    {
        private static readonly Dictionary<string, string> bundleNeedVerDic = new Dictionary<string, string>(); //bundle->ver
        private static readonly Dictionary<string, string>
            interValidBundleDic = new Dictionary<string, string>(); //bundlename->bundle_v22r1.ab

        private static string _cachePath;
        private static string _appDataPath;

        internal static void Initialize(Action sucess, Action<string> onError)
        {
            if (!Utility.assetBundleMode)
            {
                sucess();
                return;
            }

            var platform = Utility.GetPlatform();
            _cachePath = Path.Combine(Application.persistentDataPath, Path.Combine(Utility.AssetBundles, platform));
            _appDataPath = Path.Combine(Application.streamingAssetsPath, Path.Combine(Utility.AssetBundles, platform));
            if (string.IsNullOrEmpty(Utility.dataPath)) Utility.dataPath = Application.streamingAssetsPath;
            bundleNeedVerDic.Clear();

            XAssets.GetTextFromCacheOrApp("flist.txt", s =>
            {
                if (s == null) onError("error load curversion flist.txt");
                ParseFlist(s, bundleNeedVerDic);
                XAssets.GetTextFromApp("flist.txt", s1 =>
                {
                    if (s1 == null) onError("error load internal flist.txt");
                    else
                    {
                        ParseFlist(s1, interValidBundleDic, true);
                        InitPathSearcher();
                        sucess();
                    }

                });
            });
        }

        private static void ParseFlist(string flist, Dictionary<string, string> dic, bool parseInteralBundle = false)
        {
            if (flist == null) return;
            using (var reader = new StringReader(flist))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var fields = line.Split(':');
                    if (fields.Length >= 5)
                    {
                        var bundleName = fields[0];
                        var ver = fields[1];
                        var postStr = fields[2];
//                        var fielsize = fields[3];
//                        var shasum = fields[4];
                        var webBundle = fields[5];
                        if (parseInteralBundle)
                        {
                            var needVer = bundleNeedVerDic[bundleName];
                            if (needVer == ver && webBundle == "False")
                                dic.Add(bundleName, $"{bundleName}{ver}{postStr}");
                        }
                        else
                            dic.Add(bundleName, ver);
                    }
                }
            }
        }


        private static void InitPathSearcher()
        {
            Bundles.OverrideBaseDownloadingUrl += bundleName =>
            {
                string res;
                var bundleCacheName = bundleName + bundleNeedVerDic[bundleName];
                if (interValidBundleDic.ContainsKey(bundleName))
                {
                    var name = interValidBundleDic[bundleName];
                    res = Path.Combine(_appDataPath, name);
                }
                else if (File.Exists(Path.Combine(_cachePath, bundleCacheName)))
                {
                    res = Path.Combine(_cachePath, bundleCacheName);
                }
                else
                {
                    res = Utility.GetDownloadURL(bundleCacheName);
                }

                Debug.Log(res);
                return res;
            };
        }
    }
}