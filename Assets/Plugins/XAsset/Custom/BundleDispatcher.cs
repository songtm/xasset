using System;
using System.Collections.Generic;
using Plugins.XAsset;

namespace XAsset.Plugins.XAsset.Custom
{
    public static class BundleDispatcher
    {
        public static int webBundleMax = 2;
        public static int asyncBundleMax = 3;
        private static readonly List<Bundle> _asyncLoading = new List<Bundle>();
        private static readonly List<Bundle> _webLoading = new List<Bundle>();
        private static readonly Stack<Bundle> _asyncReady = new Stack<Bundle>();
        private static readonly Stack<Bundle> _webReady = new Stack<Bundle>();

        public static void Initialize()
        {
            //Bundles.OverrideBunbleDispater = DispatchBundles;
        }

        private static void DispatchBundles(List<Bundle> ready2Load, Action<Bundle> doLoad)
        {
            if (ready2Load.Count > 0)
            {
                foreach (var bundle in ready2Load) //todo check 是不是由前往后的
                {
                    if (bundle is WebBundle)
                    {
                        _webReady.Push(bundle);
                    }
                    else if (bundle is BundleAsync)
                    {
                        _asyncReady.Push(bundle);
                    }
                }

                ready2Load.Clear();
            }

            var asyncCanLoad = Math.Min(asyncBundleMax - _asyncLoading.Count, _asyncReady.Count);
            for (var i = 0; i < asyncCanLoad; i++)
            {
                var bundle = _asyncReady.Pop();
                doLoad(bundle);
                _asyncLoading.Add(bundle);
            }

            var webCanLoad = Math.Min(webBundleMax - _webLoading.Count, _webReady.Count);
            for (var i = 0; i < webCanLoad; i++)
            {
                var bundle = _webReady.Pop();
                doLoad(bundle);
                _webLoading.Add(bundle);
            }

            for (int i = 0; i < _asyncLoading.Count; i++)
            {
                var item = _asyncLoading[i];
                if (item.loadState == LoadState.Loaded || item.loadState == LoadState.Unload)
                {
                    _asyncLoading.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < _webLoading.Count; i++)
            {
                var item = _webLoading[i];
                if (item.loadState == LoadState.Loaded || item.loadState == LoadState.Unload)
                {
                    _webLoading.RemoveAt(i);
                    i--;
                }
            }

        }
    }
}