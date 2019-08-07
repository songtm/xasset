using UnityEditor.Experimental.Networking.PlayerConnection;
using UnityEngine;

namespace XAsset.Plugins.XAsset.Editor.AutoBundle
{
    public class AssetsPathPanel
    {
        public static void OnGUI()
        {
            //todo 遍历出bundleRule里面的ManulAsset(非auto的部分), 并遍历搜索脚本代码文件看是否有引用!,没有引用的就显示出来!
            //todo 遍历出脚本里面的资源路径,看是否存在这个资源, 不存在就打印出来(如策划不小心改了路径什么的!)
            if (GUILayout.Button("CheckManulAssetAndUsage"))
            {
                var manualRefAssets = AssetTarget.GetManualRefAssets();
                foreach (var asset in manualRefAssets)
                {
                    Debug.Log(asset);
                }
            }


        }
    }
}