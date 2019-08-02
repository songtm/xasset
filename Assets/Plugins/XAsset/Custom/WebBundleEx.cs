using System.IO;
using Plugins.XAsset;
using UnityEngine;
using UnityEngine.Networking;

namespace XAsset.Plugins.XAsset.Custom
{
    public class WebBundleEx : Bundle
    {
        public string assetBundleName;
        private UnityWebRequest _webFileReq;
        private AssetBundleCreateRequest _bundlReq;
        private string _savePath;
        private string _error;
        public override string error => _error;

        public override bool isDone
        {
            get
            {
                if (_error != null) return true;

                switch (loadState)
                {
                    case LoadState.Init:
                        return false;
                    case LoadState.Loaded:
                        return true;
                    case LoadState.Unload:
                        return true;
                    case LoadState.LoadAssetBundle:
                        if (_webFileReq.isDone)
                        {
                            _error = _webFileReq.error;
                            _webFileReq.Dispose();
                            _webFileReq = null;
                            _bundlReq = AssetBundle.LoadFromFileAsync(_savePath);
                            loadState = LoadState.LoadAsset;
                        }
                        return false;
                    case LoadState.LoadAsset:  //借用下这个状态(当成下载包里面的资源吧)
                        if (_bundlReq.isDone)
                        {
                            asset = _bundlReq.assetBundle;
                            loadState = LoadState.Loaded;
                            return true;
                        }
                        return false;
                }

                return false;
            }
        }

        public override float progress
        {
            get
            {
                switch (loadState)
                {
                    case LoadState.Init:
                        return 0;
                    case LoadState.Loaded:
                        return 1;
                    case LoadState.Unload:
                        return 1;
                    case LoadState.LoadAssetBundle:
                        return _webFileReq.downloadProgress * 0.9f;
                    case LoadState.LoadAsset:  //借用下这个状态(当成下载包里面的资源吧)
                        return 0.9f + _bundlReq.progress * 0.1f;
                }
                return 0;
            }
        }

        internal override void Load()
        {
            _webFileReq = new UnityWebRequest(name);
            _savePath = Path.Combine(BundlePathDelegate.cachePath, assetBundleName);
            _webFileReq.downloadHandler = new DownloadHandlerFile(_savePath);
            _webFileReq.SendWebRequest();
            loadState = LoadState.LoadAssetBundle;

        }

        internal override void Unload()
        {
            loadState = LoadState.Unload;
            base.Unload();
        }
    }
}