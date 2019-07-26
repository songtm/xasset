using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Plugins.XAsset.Editor
{
    [CustomEditor(typeof(AssetsManifest))]
    public class ManifestInspector : UnityEditor.Editor
    {
        private DateTime _lastModify = DateTime.MinValue;
        private string _manifestStr;
        private int _startLine;
        private const int _displayLineNum = 60;
        private List<int> _lineIndex;
        private Vector2 _scrollPosition = Vector2.zero;

        private void OnEnable()
        {
            _lineIndex = new List<int>();
            buildCahe();
        }

        private void OnDisable()
        {
            _manifestStr = null;
        }

        private void ResetManifest()
        {
            var manifest = BuildScript.GetManifest();
            manifest.assets = new AssetData[0];
            manifest.dirs = new string[0];
            manifest.bundles = new string[0];
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            buildCahe();
        }

        private void buildCahe()
        {
            Debug.Log("rebuild");
            _lastModify = File.GetLastWriteTime(Utility.AssetsManifestAsset);
            var manifest = BuildScript.GetManifest();
            var sb = new StringBuilder(512);

            for (var i = 0; i < manifest.bundles.Length; i++)
            {
                sb.AppendLine("bundle" + i + " " + manifest.bundles[i]);
            }

            sb.AppendLine("----------");
            for (var i = 0; i < manifest.dirs.Length; i++)
            {
                sb.AppendLine("dir" + i + " " + manifest.dirs[i]);
            }
            sb.AppendLine("----------");

            for (int i = 0; i < manifest.assets.Length; i++)
            {
                var assetData = manifest.assets[i];
                var desc = string.Format("asset{0} bundle:{1} variant:{2}, dir:{3}, name:{4}", i,
                    assetData.bundle, assetData.variant, assetData.dir, assetData.name);
                sb.AppendLine(desc);
            }


            _manifestStr = sb.ToString();
            _lineIndex.Clear();
            int curLine = 0;
            _lineIndex.Add(-1);
            for (var i = 0; i < _manifestStr.Length; i++)
            {
                if (_manifestStr[i] == '\n')
                {
                    curLine++;
                    _lineIndex.Add(i);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (_lastModify != File.GetLastWriteTime(Utility.AssetsManifestAsset))
                buildCahe();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("downloadURL"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activeVariants"), true);


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<<", EditorStyles.label, GUILayout.MaxWidth(40)))
            {
                _startLine -= _displayLineNum;
            }

            _startLine = (int) GUILayout.HorizontalSlider(_startLine, 0, _lineIndex.Count - _displayLineNum - 1);
            if (GUILayout.Button(">>", EditorStyles.label, GUILayout.MaxWidth(40)))
            {
                _startLine += _displayLineNum;
            }

            if (GUILayout.Button("clear", EditorStyles.toolbarButton, GUILayout.MaxWidth(60)))
            {
                if (EditorUtility.DisplayDialog("Clear manifest!", "Do you really want to  clear the manifest?", "OK",
                    "Cancel"))
                    ResetManifest();
            }

            if (GUILayout.Button("refresh", EditorStyles.toolbarButton, GUILayout.MaxWidth(60)))
            {
                buildCahe();
            }


            GUILayout.EndHorizontal();

            var maxLine = _lineIndex.Count - 1;
            _startLine = Math.Max(Math.Min(_startLine, maxLine - _displayLineNum), 0);
            int start = _startLine;
            int end = Math.Min(_startLine + _displayLineNum, maxLine);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            GUI.enabled = false;
            EditorGUILayout.TextArea(_manifestStr.Substring(_lineIndex[start] + 1,
                _lineIndex[end] - _lineIndex[start] - 1));
            GUI.enabled = true;
            GUILayout.EndScrollView();
        }
    }
}