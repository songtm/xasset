using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using XAsset.Plugins.XAsset.Editor.AutoBundle;
using Object = UnityEngine.Object;

namespace Plugins.XAsset.Editor.AutoBundle
{

    public class HideXassetMemu
    {
        [MenuItem("Assets/AssetBundles/按目录标记", true)]
        static bool Hide1() => false;
        [MenuItem("Assets/AssetBundles/按文件标记", true)]
        static bool Hide2() => false;


        [MenuItem("Assets/AssetBundles/按名称标记", true)]
        static bool Hide3() =>false;
        [MenuItem("Assets/AssetBundles/生成配置", true)]
        static bool Hide4() =>false;
        [MenuItem("Assets/AssetBundles/生成播放器", true)]
        static bool Hide5() =>false;
        [MenuItem("标记资源", true)]
        static bool Hide6() =>false;


    }
    public static class ExtClass
    {
        public static IEnumerable<FileInfo> GetFilesByExtensions(this DirectoryInfo dirInfo,  string[] extensions, SearchOption option)
        {
            var allowedExtensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);

            return dirInfo.GetFiles("*.*", option)
                .Where(f => allowedExtensions.Contains(f.Extension));
        }

    }

    public class AssetBundleBuildPanel : EditorWindow
    {
        [MenuItem("Assets/AssetBundles/AutoBundleSystem")]
        static void Open()
        {
            GetWindow<AssetBundleBuildPanel>("ABSystem", true);
        }

        static void BuildAssetBundles()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit play mode before build AssetBundle!");
                return;
            }

            AssetBundleBuildConfig config = AssetDatabase.LoadAssetAtPath<AssetBundleBuildConfig>(savePath);

            if (config == null) return;

            AssetTarget.IgnoreDepFindExt.Clear();
            var spriteExts = config.SpriteExtension.Split(';');
            foreach (var spriteExt in spriteExts)
            {
                AssetTarget.IgnoreDepFindExt.Add(spriteExt);
            }
            AssetTarget.IgnoreDepFindExt.Add(".mp3");
            AssetTarget.IgnoreDepFindExt.Add(".mp4");//todo add some more!

            AssetTarget.AllAssetTargts.Clear();
            AssetTarget.AutoAssetDirs.Clear();
            Debug.Log("111----- "+Time.realtimeSinceStartup);

            foreach (var f in config.filters)
            {
                if (f.valid && f.packMode == PackMode.EachDirAuto)
                {
                    string[] directories = Directory.GetDirectories(f.path, "*", SearchOption.AllDirectories);
                    AssetTarget.AutoAssetDirs.Add(f.path);
                    foreach (var dir in directories)
                    {
                        AssetTarget.AutoAssetDirs.Add(dir);
                    }
                }
            }
            foreach (var f in config.filters)
            {
                if (f.valid && (f.packMode == PackMode.EachDirAtlasAuto || f.packMode == PackMode.EachDirAtlasManul))
                {
                    AddRootTargets(new DirectoryInfo(f.path), f.packMode, config.SpriteExtension,
                        f.packMode == PackMode.EachDirAtlasAuto
                            ? AssetBundleExportType.AtlasUnused
                            : AssetBundleExportType.AtlasUsed);
                }
            }
            Debug.Log("222----- "+Time.realtimeSinceStartup);
            foreach (var f in config.filters)
            {
                if (f.valid && f.packMode != PackMode.EachDirAtlasAuto
                            && f.packMode != PackMode.EachDirAtlasManul && f.packMode != PackMode.EachDirAuto)
                    AddRootTargets(new DirectoryInfo(f.path), f.packMode, f.filter, AssetBundleExportType.Root);
            }
            Debug.Log("333----- "+Time.realtimeSinceStartup);

            var bundleMap = AssetTarget.ProcessRelations(config.AtlasOutputDir);

            GenXssetManifest(bundleMap);



            AssetDatabase.Refresh();
            Debug.Log("---end"+Time.realtimeSinceStartup);
        }

        private static void GenXssetManifest(Dictionary<string, List<string>> bundleMap)
        {
            AssetsManifest assetsManifest = BuildScript.GetManifest();
            assetsManifest.dirs = new string[0];
            assetsManifest.assets = new AssetData[0];
            assetsManifest.bundles = new string[0];
            assetsManifest.activeVariants = new string[0];

            var bundlenames = bundleMap.Keys.ToArray();
            assetsManifest.bundles = bundlenames;
            var bundleNameDic = new Dictionary<string, int>();
            for (var i = 0; i < bundlenames.Length; i++)
            {
                bundleNameDic[bundlenames[i]] = i;
            }

            var dirset = new HashSet<string>();
            foreach (var keyValuePair in bundleMap)
            {
                foreach (var s in keyValuePair.Value)
                {
                    var dir = Path.GetDirectoryName(s).Replace("\\", "/");
                    dirset.Add(dir);
                }
            }

            var dirs = dirset.ToArray();
            var dirDic = new Dictionary<string, int>();
            for (var i = 0; i < dirs.Length; i++)
            {
                dirDic[dirs[i]] = i;
            }
            assetsManifest.dirs = dirs;

            var  assetDatas = new List<AssetData>(1000);
            foreach (var keyValuePair in bundleMap)
            {
                var bundleName = keyValuePair.Key;
                foreach (var assetPath in keyValuePair.Value)
                {
                    var dir = Path.GetDirectoryName(assetPath).Replace("\\", "/");
                    var data = new AssetData
                    {
                        bundle = bundleNameDic[bundleName],
                        dir = dirDic[dir],
                        name = Path.GetFileName(assetPath),
                        variant = -1
                    };
                    assetDatas.Add(data);
                }
            }

            assetsManifest.assets = assetDatas.ToArray();
            EditorUtility.SetDirty(assetsManifest);
            AssetDatabase.SaveAssets();


        }
        private static void AddRootTargets(DirectoryInfo bundleDir, PackMode fPackMode, string pattern,
            AssetBundleExportType exportType,
            SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (string.IsNullOrEmpty(pattern))
                pattern = ".prefab";
            var pans = pattern.Split(';');
            var prefabs = bundleDir.GetFilesByExtensions(pans, searchOption);
            foreach (FileInfo file in prefabs)
            {
                if (file.Extension.Contains("meta"))
                    continue;

                var assetPath = "Assets" + file.FullName.Replace(Application.dataPath, "");
                var bundleName = AssetTarget.GetBundleName(bundleDir, file, fPackMode, pattern);
                new AssetTarget(assetPath, bundleName,fPackMode, exportType);
            }
        }

        public static string savePath = "Assets/bundle_rule.asset";

        private AssetBundleBuildConfig _config;
        private ReorderableList _list;
        private Vector2 _scrollPosition = Vector2.zero;

        AssetBundleBuildPanel()
        {
        }

        void OnListElementGUI(Rect rect, int index, bool isactive, bool isfocused)
        {
            const float GAP = 5;

            AssetBundleFilter filter = _config.filters[index];
            rect.y++;

            Rect r = rect;
            r.width = 16;
            r.height = 18;
            filter.valid = GUI.Toggle(r, filter.valid, GUIContent.none);

            r.xMin = r.xMax + GAP;
            r.xMax = rect.xMax - 300;
            GUI.enabled = false;
            filter.path = GUI.TextField(r, filter.path);
            GUI.enabled = true;

            r.xMin = r.xMax + GAP;
            r.width = 50;
            if (GUI.Button(r, "Select"))
            {
                var path = SelectFolder();
                if (path != null)
                    filter.path = path;
            }

            r.xMin = r.xMax + GAP;
            r.width = 120;
            filter.packMode = (PackMode) EditorGUI.EnumPopup(r, filter.packMode);
            if (filter.packMode != PackMode.EachDirAtlasAuto && filter.packMode != PackMode.EachDirAtlasManul
                && filter.packMode != PackMode.EachDirAuto)
            {
                r.xMin = r.xMax + GAP;
                r.xMax = rect.xMax;
                filter.filter = GUI.TextField(r, filter.filter);
            }
        }

        string SelectFolder()
        {
            string dataPath = Application.dataPath;
            string selectedPath = EditorUtility.OpenFolderPanel("Path", dataPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(dataPath))
                {
                    return "Assets/" + selectedPath.Substring(dataPath.Length + 1);
                }
                else
                {
                    ShowNotification(new GUIContent("不能在Assets目录之外!"));
                }
            }

            return null;
        }

        void OnListHeaderGUI(Rect rect)
        {
            EditorGUI.LabelField(rect, "Asset Filter");
        }

        void InitConfig()
        {
            _config = AssetDatabase.LoadAssetAtPath<AssetBundleBuildConfig>(savePath);
            if (_config == null)
            {
                _config = CreateInstance<AssetBundleBuildConfig>();
            }
        }

        void InitFilterListDrawer()
        {
            _list = new ReorderableList(_config.filters, typeof(AssetBundleFilter));
            _list.drawElementCallback = OnListElementGUI;
            _list.drawHeaderCallback = OnListHeaderGUI;
            _list.draggable = true;
            _list.elementHeight = 22;
            _list.onAddCallback = (list) => Add();
        }

        void Add()
        {
            string path = SelectFolder();
            if (!string.IsNullOrEmpty(path))
            {
                var filter = new AssetBundleFilter();
                filter.path = path;
                _config.filters.Add(filter);
            }
        }

        void OnGUI()
        {
            if (_config == null)
            {
                InitConfig();
            }

            if (_list == null)
            {
                InitFilterListDrawer();
            }

            bool execBuild = false;
            //tool bar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (GUILayout.Button("Add", EditorStyles.toolbarButton))
                {
                    Add();
                }

                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    Save();
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Build", EditorStyles.toolbarButton))
                {
                    execBuild = true;
                }
            }
            GUILayout.EndHorizontal();

            //context
            GUILayout.BeginVertical();
            {
                //format
                GUILayout.BeginHorizontal();
                {
                    _config.AtlasOutputDir = EditorGUILayout.TextField("AtlasOutputDir", _config.AtlasOutputDir);
                    _config.SpriteExtension = EditorGUILayout.TextField("SpriteExtension", _config.SpriteExtension);
                    _config.graphMode = (AssetBundleBuildConfig.GraphMode) EditorGUILayout.EnumPopup("Graph Mode", _config.graphMode);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                //Filter item list
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
                {
                    _list.DoLayoutList();
                }
                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            //set dirty
            if (GUI.changed)
                EditorUtility.SetDirty(_config);

            if (execBuild)
                Build();
        }

        private void Build()
        {
            Save();
            BuildAssetBundles();
        }

        void Save()
        {
            if (AssetDatabase.LoadAssetAtPath<AssetBundleBuildConfig>(savePath) == null)
            {
                AssetDatabase.CreateAsset(_config, savePath);
            }
            else
            {
                EditorUtility.SetDirty(_config);
            }
        }
    }
}
