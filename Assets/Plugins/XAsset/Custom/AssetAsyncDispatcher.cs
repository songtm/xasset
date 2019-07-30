using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace XAsset.Plugins.XAsset.Custom
{
    public class AssetBundleRequestWrapper
    {
        private AssetBundleRequest _request;
        public bool loaded;
        public void Loaded(AssetBundleRequest req)
        {
            _request = req;
            loaded = true;
        }

        public float progress => _request?.progress ?? 0;
        public Object asset => _request.asset;
        public bool isDone => _request?.isDone ?? false;

        public AssetBundle bundle;
        public string assetName;
        public Type assetType;
    }
    public static  class AssetAsyncDispatcher
    {
        public static int maxCountPerFrame = 1;
        private static readonly Stack<AssetBundleRequestWrapper> _asyncInfos = new Stack<AssetBundleRequestWrapper>();

        public static void Append(AssetBundleRequestWrapper info)
        {
            if (!info.loaded) _asyncInfos.Push(info);
        }
        public static void Update()
        {
            if (_asyncInfos.Count <= 0) return;

            int loaded = 0;
            while (_asyncInfos.Count > 0 && loaded < maxCountPerFrame)
            {
                var info = _asyncInfos.Pop();
                if (!info.loaded)
                {
                    var request = info.bundle.LoadAssetAsync(info.assetName, info.assetType);
                    info.Loaded(request);
                    loaded++;
                }

            }
        }
    }
}