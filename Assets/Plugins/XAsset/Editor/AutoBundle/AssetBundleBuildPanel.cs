using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using XAsset.Plugins.XAsset.Editor.AutoBundle;

namespace Plugins.XAsset.Editor.AutoBundle
{
    public class AssetBundleBuildPanel : EditorWindow
    {
        [MenuItem("ABSystem/Builder Panel")]
        static void Open()
        {
            GetWindow<AssetBundleBuildPanel>("ABSystem", true);
        }

        [MenuItem("ABSystem/Build AssetBundles")]
        static void BuildAssetBundles()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit play mode before build AssetBundle!");
                return;
            }

            AssetBundleBuildConfig config = LoadAssetAtPath<AssetBundleBuildConfig>(savePath);

            if (config == null) return;

            AssetTarget.AllAssetTargts.Clear();
            AssetTarget.AllAtlasDirs.Clear();
            foreach (var f in config.filters)
            {
                if (f.valid && (f.packMode == PackMode.AtlasAuto || f.packMode == PackMode.AtlasManul))
                {
                    AddRootTargets(new DirectoryInfo(f.path), f.packMode, "*.png", //todo
                        f.packMode == PackMode.AtlasAuto
                            ? AssetBundleExportType.AtlasUnused
                            : AssetBundleExportType.AtlasUsed);
                    AssetTarget.AllAtlasDirs.Add(f.path, f.packMode);
                }
            }

            foreach (var f in config.filters)
            {
                if (f.valid && f.packMode != PackMode.AtlasAuto && f.packMode != PackMode.AtlasManul)
                    AddRootTargets(new DirectoryInfo(f.path), f.packMode, f.filter, AssetBundleExportType.Root);
            }


            AssetsManifest assetsManifest = BuildScript.GetManifest();
            assetsManifest.dirs = new string[0];
            assetsManifest.assets = new AssetData[0];
            assetsManifest.bundles = new string[0];
            assetsManifest.activeVariants = new string[0];

            var bundleMap = AssetTarget.ProcessRelations();
            foreach (var keyValuePair in bundleMap)
            {
                Debug.Log("bundle: " + keyValuePair.Key);
                foreach (var s in keyValuePair.Value)
                {
                    Debug.Log("\tasset: " + s);
                    BuildScript.SetAssetBundleNameAndVariant(s, keyValuePair.Key, null);
                }
            }
        }

        private static void AddRootTargets(DirectoryInfo bundleDir, PackMode fPackMode, string pattern,
            AssetBundleExportType exportType,
            SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (string.IsNullOrEmpty(pattern))
                pattern = "*.*";

            FileInfo[] prefabs = bundleDir.GetFiles(pattern, searchOption);
            foreach (FileInfo file in prefabs)
            {
                if (file.Extension.Contains("meta"))
                    continue;

                var assetPath = "Assets" + file.FullName.Replace(Application.dataPath, "");
                var bundleName = AssetTarget.GetBundleName(bundleDir, file, fPackMode, pattern);
                new AssetTarget(assetPath, bundleName, exportType);
            }
        }


        static T LoadAssetAtPath<T>(string path) where T : Object
        {
#if UNITY_5 || UNITY_2017_1_OR_NEWER
            return AssetDatabase.LoadAssetAtPath<T>(path);
#else
			return (T)AssetDatabase.LoadAssetAtPath(path, typeof(T));
#endif
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
            r.width = 80;
            filter.packMode = (PackMode) EditorGUI.EnumPopup(r, filter.packMode);
            if (filter.packMode != PackMode.AtlasAuto && filter.packMode != PackMode.AtlasManul)
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
            _config = LoadAssetAtPath<AssetBundleBuildConfig>(savePath);
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
                    _config.graphMode =
                        (AssetBundleBuildConfig.GraphMode) EditorGUILayout.EnumPopup("Graph Mode", _config.graphMode);
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
            if (LoadAssetAtPath<AssetBundleBuildConfig>(savePath) == null)
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