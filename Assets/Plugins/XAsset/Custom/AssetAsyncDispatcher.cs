using System;
using System.Collections.Generic;
using Plugins.XAsset;
using UnityEngine;
using Object = UnityEngine.Object;

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

    public static  class AssetAsyncDispatcher
    {
        public static bool enabled = true;
        public static int maxCountPerFrame = 1;
        private static readonly PriorityQueue<BundleAssetAsync> _assetAsyncQueue = new PriorityQueue<BundleAssetAsync>();

        internal static void Initialize(bool enable)
        {
            enabled = enable;
        }

        public static void Upgrade(BundleAssetAsync assetAsync)
        {
            if (!enabled) return;

            if (assetAsync.loadState == LoadState.Init)//再次请求资源,但 bundle 还未就绪
            {
                //这种情况不存在
                Debug.LogError("ohno");
            }
            else if (assetAsync.loadState == LoadState.LoadAssetBundle)
            {
                BundleDispatcher.Upgrade(assetAsync.GetBundle());
            }
            else if (assetAsync.loadState == LoadState.LoadAsset)////再次请求资源,但 资源还未就绪
            {
                _assetAsyncQueue.Up(assetAsync.queuePos);
            }
        }

        public static void Append(BundleAssetAsync assetAsync)
        {
//            Debug.Log("append "+assetAsync.name);
            if (!enabled)
            {
                assetAsync._request.Loaded(assetAsync.LoadBundleAssetAsync());
            }
            else if (assetAsync.loadState == LoadState.LoadAsset)
            {
                _assetAsyncQueue.Enqueue(assetAsync);
            }
        }

        public static void Update()
        {
            if (!enabled) return;

            if (_assetAsyncQueue.Count() <= 0) return;

            int loaded = 0;
            while (_assetAsyncQueue.Count() > 0 && loaded < maxCountPerFrame)
            {
                var assetAsync = _assetAsyncQueue.Dequeue();
                if (assetAsync.loadState == LoadState.LoadAsset)
                {
                    assetAsync._request.Loaded(assetAsync.LoadBundleAssetAsync());
                    //Debug.Log(" began loaded asset " + assetAsync.name + " f:"+Time.frameCount);
                    loaded++;
                }

            }
        }
    }
}