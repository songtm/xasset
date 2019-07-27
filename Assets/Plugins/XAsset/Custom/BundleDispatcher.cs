using System;
using System.Collections.Generic;
using Plugins.XAsset;
//todo 出错后的 重试机制??
namespace XAsset.Plugins.XAsset.Custom
{
    public static class BundleDispatcher
    {
        public static int webBundleMax = 2;
        public static int asyncBundleMax = 3;
        // ReSharper disable once InconsistentNaming
        private static readonly List<Bundle> _asyncLoading = new List<Bundle>();
        // ReSharper disable once InconsistentNaming
        private static readonly List<Bundle> _webLoading = new List<Bundle>();
        // ReSharper disable once InconsistentNaming
        private static readonly Stack<Bundle> _asyncReady = new Stack<Bundle>();
        // ReSharper disable once InconsistentNaming
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
                    if (bundle is WebBundle)//优先级处理,比如一个bundle的所有依赖都要提升!
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

            PatchBundleEach(asyncBundleMax, _asyncReady, _asyncLoading, doLoad);
            PatchBundleEach(webBundleMax, _webReady, _webLoading, doLoad);

        }

        private static void PatchBundleEach(int max, Stack<Bundle> readyBundles, List<Bundle> loading,
            Action<Bundle> doLoad)
        {
            while (max - loading.Count > 0 && readyBundles.Count > 0)
            {
                var bundle = readyBundles.Pop();
                if (bundle.loadState == LoadState.Init)//避免可能其它地方调用了开始
                {
                    doLoad(bundle);
                    loading.Add(bundle);
                }
            }

            for (int i = 0; i < loading.Count; i++)
            {
                var item = loading[i];
                if (item.loadState == LoadState.Loaded || item.loadState == LoadState.Unload) //todo 如果出错会怎么样?
                {
                    loading.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}