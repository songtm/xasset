using System;
using System.Collections.Generic;
using Plugins.XAsset;
//todo 出错后的 重试机制??
//todo 后面再次请求 在列表中的bundle的提升! 再次添加到ready2Load调用dispatch!
namespace XAsset.Plugins.XAsset.Custom
{
    public static class BundleDispatcher
    {
        public static int webBundleMax = 2;
        public static int asyncBundleMax = 3;
        public static bool webBundlePriority = false;//开了之后表示要处理优先级
        public static bool asyncBundlePriority = false;//可用于实现预加载bunble? 但是预加载资源呢?有加载数量限制么?

        // ReSharper disable once InconsistentNaming
        private static readonly List<Bundle> _asyncLoading = new List<Bundle>();
        // ReSharper disable once InconsistentNaming
        private static readonly List<Bundle> _webLoading = new List<Bundle>();
        // ReSharper disable once InconsistentNaming
        private static readonly Stack<Bundle> _asyncReady = new Stack<Bundle>();

        // ReSharper disable once InconsistentNaming
        private static readonly Stack<Bundle> _webReady = new Stack<Bundle>();

        private static readonly HashSet<Bundle> _read2LoadSet = new HashSet<Bundle>();

        internal static void Initialize()
        {
            //Bundles.OverrideBunbleDispater = DispatchBundles;
        }

        // ready2Load一般是一帧一帧来的, 所以基本里面包含了依赖的bundle
        private static void DispatchBundles(List<Bundle> ready2Load, Action<Bundle> doLoad)
        {
            if (ready2Load.Count > 0)
            {
                _read2LoadSet.Clear();
                foreach (var bundle in ready2Load)
                {
                    _read2LoadSet.Add(bundle);
                }

                foreach (var bundle in ready2Load) //todo check 是不是由前往后的
                {
                    if (bundle is WebBundle)//优先级处理,比如一个bundle的所有依赖都要提升!
                    {
                        //_ready2Load是先放主,再放依赖, 比如已经在_webReady的话,如何提升!
                        //哦 直接看bundle的引用计算就行了!
                        _webReady.Push(bundle);
                        if (webBundlePriority)
                        {
                            foreach (var depBundle in bundle.dependencies)
                            {
                                if (!_read2LoadSet.Contains(depBundle) && depBundle.loadState == LoadState.Init&&
                                    depBundle is WebBundle)
                                {
                                    _webReady.Push(depBundle);
                                }
                            }
                        }


                    }
                    else if (bundle is BundleAsync)
                    {
                        _asyncReady.Push(bundle);
                        if (asyncBundlePriority)
                        {
                            foreach (var depBundle in bundle.dependencies)
                            {
                                if (!_read2LoadSet.Contains(depBundle) && depBundle.loadState == LoadState.Init&&
                                    depBundle is BundleAsync)
                                {
                                    _asyncReady.Push(depBundle);
                                }
                            }
                        }
                    }
                }

                ready2Load.Clear();
                _read2LoadSet.Clear();
            }

            PatchBundleEach(asyncBundleMax, _asyncReady, _asyncLoading, doLoad);
            PatchBundleEach(webBundleMax, _webReady, _webLoading, doLoad);
            //todo 怎么判断当前的网速 扶不住了?
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
                //todo 如果出错会怎么样?, 不能用isDone,因为里面有些操作如LoadAsset什么的!
                if (item.loadState == LoadState.Loaded || item.loadState == LoadState.Unload)
                {
                    loading.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}