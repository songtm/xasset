using System;
using System.Collections.Generic;
using Plugins.XAsset;
using UnityEngine;

//大量堆积的 WebBundle不能影响玩家操作
//大量堆积的 BundleAsync(如实现不卡操作的预加载/无缝加载) 也要排序
//todo 出错后的 重试机制??
//todo 后面再次请求 在列表中的bundle的提升! 再次添加到ready2Load调用dispatch!
namespace XAsset.Plugins.XAsset.Custom
{
    public static class BundleDispatcher
    {
        public static bool enabled = false;
        public static int webBundleMax = 2;
        public static int asyncBundleMax = 4;

        // ReSharper disable once InconsistentNaming
        private static readonly List<Bundle> _asyncLoading = new List<Bundle>();

        // ReSharper disable once InconsistentNaming
        private static readonly List<Bundle> _webLoading = new List<Bundle>();

        // ReSharper disable once InconsistentNaming
        private static readonly PriorityQueue<Bundle> _asyncBundleQueue = new PriorityQueue<Bundle>();

        // ReSharper disable once InconsistentNaming
        private static readonly PriorityQueue<Bundle> _webBundleQeue = new PriorityQueue<Bundle>();

        internal static void Initialize(bool enable, int webLimit, int asyncLimit)
        {
            webBundleMax = webLimit;
            asyncBundleMax = asyncLimit;
            enabled = enable;
        }

        public static void Upgrade(Bundle bundle)
        {
            bundle.ResetReqTime();
            UpgradOne(bundle);
            foreach (var dependency in bundle.dependencies)
            {
                dependency.ResetReqTime();
                UpgradOne(dependency);
            }
        }

        private static void UpgradOne(Bundle bundle)
        {
            if (!enabled) return;
            if (bundle.loadState == LoadState.Init)
            {
                if (bundle is WebBundle)
                {
                    _webBundleQeue.Up(bundle.queuePos);
                }

                if (bundle is BundleAsync)
                {
                    _asyncBundleQueue.Up(bundle.queuePos);
                }
            }
        }

        // ready2Load一般是一帧一帧来的, 所以基本里面包含了依赖的bundle
        public static bool DispatchBundles(List<Bundle> ready2Load, Action<Bundle> doLoad)
        {
            if (!enabled) return false;
            if (ready2Load.Count > 0)
            {
                foreach (var bundle in ready2Load) //todo check 是不是由前往后的
                {
                    if (bundle is WebBundle)
                        _webBundleQeue.Enqueue(bundle);
                    else if (bundle is BundleAsync)
                        _asyncBundleQueue.Enqueue(bundle);
                }

                ready2Load.Clear();
            }

            PatchBundleEach(asyncBundleMax, _asyncBundleQueue, _asyncLoading, doLoad);
            PatchBundleEach(webBundleMax, _webBundleQeue, _webLoading, doLoad);
            //todo 怎么判断当前的网速 扶不住了?

            return true;
        }

        private static void PatchBundleEach(int max, PriorityQueue<Bundle> readyBundles, List<Bundle> loading,
            Action<Bundle> doLoad)
        {
            while (max - loading.Count > 0 && readyBundles.Count() > 0)
            {
                var bundle = readyBundles.Dequeue();
                if (bundle.loadState == LoadState.Init) //避免可能其它地方调用了开始
                {
                    doLoad(bundle);
                    //Debug.Log(" begin loaded bundle" + bundle.name + " f:" + Time.frameCount);
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