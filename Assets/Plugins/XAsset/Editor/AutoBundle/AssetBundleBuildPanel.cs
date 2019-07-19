using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Plugins.XAsset.Editor.AutoBundle
{
    public class AssetBundleBuildPanel : EditorWindow
    {
        [MenuItem("ABSystem/Builder Panel")]
        static void Open()
        {
            GetWindow<AssetBundleBuildPanel>("ABSystem", true);
        }

        [MenuItem("ABSystem/Builde AssetBundles")]
        static void BuildAssetBundles()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Exit play mode before build AssetBundle!");
                return;
            }
            AssetBundleBuildConfig config = LoadAssetAtPath<AssetBundleBuildConfig>(savePath);

            if (config == null)
                return;

//			ABBuilder builder = new AssetBundleBuilder5x(new AssetBundlePathResolver());
//
//            builder.SetDataWriter(config.depInfoFileFormat == AssetBundleBuildConfig.Format.Text ? new AssetBundleDataWriter() : new AssetBundleDataBinaryWriter());
//
//            builder.Begin();

            for (int i = 0; i < config.filters.Count; i++)
            {
                AssetBundleFilter f = config.filters[i];
                if (f.valid)
                    AddRootTargets(new DirectoryInfo(f.path), f.packMode, f.filter);
            }
//            builder.Export();
//            builder.End();
        }

        private static void  AddRootTargets(DirectoryInfo bundleDir, PackMode fPackMode, string parttern = null,
            SearchOption searchOption = SearchOption.AllDirectories)
        {
            if (string.IsNullOrEmpty(parttern))
                parttern = "*.*";

            FileInfo[] prefabs = bundleDir.GetFiles(parttern, searchOption);

            foreach (FileInfo file in prefabs)
            {
                if (file.Extension.Contains("meta"))
                    continue;

                var assetPath = "Assets" + file.FullName.Replace(Application.dataPath, "");
                var bundleName = GetBundleName(bundleDir, file, fPackMode, parttern);
                var exportType = AssetBundleExportType.Root;

                AssetTarget target = new AssetTarget(assetPath, bundleName, exportType);
            }
        }

        public static string GetBundleName(DirectoryInfo bundleDir, FileInfo file, PackMode fPackMode, string parttern)
        {
            switch (fPackMode)
            {
                case PackMode.Indepent:
                    var path = file.FullName.Replace(Application.dataPath, "");
                    return path;
                case PackMode.AllInOne:
                    var str1 = "__"+bundleDir.ToString()+parttern+fPackMode;
//                    abName =  HashUtil.Get(str1)+".ab";
                    return bundleDir.ToString() + "/" + parttern + "("+fPackMode+")";
                case PackMode.PerAnyDir:
                    var d = file.Directory;
                    var str2 = bundleDir.ToString() + d.FullName.Replace(bundleDir.FullName, "");
//                    abName = HashUtil.Get("_" + str2) + ".ab";
                    return str2 + "/" + parttern + "("+fPackMode+")";
                case PackMode.PerSubDir:
                    var dir = file.Directory;
                    var subDir = "";
                    while (dir.FullName != bundleDir.FullName)
                    {
                        subDir = dir.Name + "/";
                        dir = dir.Parent;
                    }
                    var str = "____"+bundleDir.ToString()+subDir+parttern+fPackMode;
//                    abName = HashUtil.Get(str)+".ab";
                    return bundleDir.ToString()+"/"+subDir+parttern + "("+fPackMode+")";
                default:
                    return null;
            }
        }

		static T LoadAssetAtPath<T>(string path) where T:Object
		{
#if UNITY_5 || UNITY_2017_1_OR_NEWER
			return AssetDatabase.LoadAssetAtPath<T>(path);
#else
			return (T)AssetDatabase.LoadAssetAtPath(path, typeof(T));
#endif
		}

        const string savePath = "Assets/bundle_rule.asset";

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

            r.xMin = r.xMax + GAP;
            r.xMax = rect.xMax;
            filter.filter = GUI.TextField(r, filter.filter);
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
                    _config.graphMode = (AssetBundleBuildConfig.GraphMode)EditorGUILayout.EnumPopup("Graph Mode", _config.graphMode);
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