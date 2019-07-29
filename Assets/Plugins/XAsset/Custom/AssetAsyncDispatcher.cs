using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
//todo 后面再次请求在列表中的提升!!

namespace XAsset.Plugins.XAsset.Custom
{
    public class AssetBundleRequestWrapper
    {
        private AssetBundleRequest _request;

        public void Loaded(AssetBundleRequest req)
        {
            _request = req;
        }

        public float progress => _request?.progress ?? 0;
        public Object asset => _request.asset;
        public bool isDone => _request?.isDone ?? false;
    }

    public struct LoadAssetAsyncInfo
    {
        public AssetBundle bundle;
        public string assetName;
        public Type assetType;
        public AssetBundleRequestWrapper reqeustWrapper;
    }
    public static  class AssetAsyncDispatcher
    {
        public static int maxCountPerFrame = 1;
        private static readonly Stack<LoadAssetAsyncInfo> _asyncInfos = new Stack<LoadAssetAsyncInfo>();

        public static void Append(LoadAssetAsyncInfo info)
        {
            _asyncInfos.Push(info);
        }
        public static void Update()
        {
            if (_asyncInfos.Count <= 0) return;
            int num = Math.Min(maxCountPerFrame, _asyncInfos.Count);
            for (int i = 0; i < num; i++)
            {
                var info = _asyncInfos.Pop();
                var request = info.bundle.LoadAssetAsync(info.assetName, info.assetType);
                info.reqeustWrapper.Loaded(request);
            }
        }
    }
}