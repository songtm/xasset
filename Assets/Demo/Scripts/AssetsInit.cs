using UnityEngine;
using Plugins.XAsset;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using XAsset.Plugins.XAsset.Custom;

public class AssetsInit : MonoBehaviour
{
    public string assetPath;

    // Start is called before the first frame update
    void Start()
    {
        /// 初始化
        XAssets.Initialize(this, OnInitialized, (error) => { Debug.Log(error); });
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Assets.LoadAsync("Assets/Demo/Prefabs/UIRoot.prefab", typeof(GameObject)).completed += asset =>
            {
                var go = Instantiate(asset.asset);
                asset.Require(this);
                asset.Release();
            };
        }

        if (Input.GetMouseButtonDown(1))
        {
            Assets.LoadAsync("Assets/Demo/Prefabs/Canvas 1.prefab", typeof(GameObject)).completed += asset =>
            {
                var go = Instantiate(asset.asset);
                asset.Require(this);
                asset.Release();
            };
        }
    }

    private void OnInitialized()
    {

        for (int i = 1; i < 100; i++)
        {
            Assets.LoadAsync($"Assets/Demo/Prefabs/Canvas {i}.prefab", typeof(GameObject)).completed += asset =>
            {
//                Debug.Log("finished frame"+Time.frameCount + "  " + asset.name);
                asset.Release();
            };

        }
        if (assetPath.EndsWith(".prefab", StringComparison.CurrentCulture))
        {
            Debug.Log(Time.frameCount);
            var asset = Assets.LoadAsync(assetPath, typeof(UnityEngine.Object));
            asset.completed += delegate(Asset a)
            {
                Debug.Log(Time.frameCount);
                var go = Instantiate(a.asset);
                go.name = a.asset.name;
                /// 设置关注对象，当关注对象销毁时，回收资源
                a.Require(go);
                Destroy(go, 3);
                /// 设置关注对象后，只需要释放一次，可以按自己的喜好调整，
                /// 例如 ABSystem 中，不需要 调用这个 Release，
                /// 这里如果之前没有调用 Require，下一帧这个资源就会被回收
                a.Release();
            };
        }
        else if(assetPath.EndsWith(".unity", StringComparison.CurrentCulture))
        {
            StartCoroutine(LoadSceneAsync());
        }
    }

    IEnumerator LoadSceneAsync()
    {
        var sceneAsset = Assets.LoadScene(assetPath, true, true);
        while(!sceneAsset.isDone)
        {
            Debug.Log(sceneAsset.progress);
            yield return null;
        }

        yield return new WaitForSeconds(3);
        Assets.Unload(sceneAsset);
    }
}
