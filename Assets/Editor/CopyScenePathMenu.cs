using UnityEngine;
using UnityEditor;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class CopyScenePathMenu
{
    private static bool EnableRegexFiltering
    {
        get { return EditorPrefs.GetBool("CopyScenePathMenu_EnableRegexFiltering", false); }
        set { EditorPrefs.SetBool("CopyScenePathMenu_EnableRegexFiltering", value); }
    }
    private static string RegexPattern
    {
        get { return EditorPrefs.GetString("CopyScenePathMenu_RegexPattern", @"Effect\d*"); }
        set { EditorPrefs.SetString("CopyScenePathMenu_RegexPattern", value); }
    }
    private static bool RemoveMatchedParts
    {
        get { return EditorPrefs.GetBool("CopyScenePathMenu_RemoveMatchedParts", true); }
        set { EditorPrefs.SetBool("CopyScenePathMenu_RemoveMatchedParts", value); }
    }
    private static bool ShowPreviewBeforeCopy
    {
        get { return EditorPrefs.GetBool("CopyScenePathMenu_ShowPreviewBeforeCopy", true); }
        set { EditorPrefs.SetBool("CopyScenePathMenu_ShowPreviewBeforeCopy", value); }
    }
    private static Regex cachedRegex;
    private static string lastRegexPattern;
    [MenuItem("GameObject/Copy Scene Path", false, 0)]
    private static void CopyScenePath()
    {
        if (Selection.gameObjects.Length > 1)
        {
            CopyMultipleScenePaths();
            return;
        }
        GameObject selected = Selection.activeGameObject;
        if (selected == null) return;
        string path = GetScenePath(selected);
        if (EnableRegexFiltering)
        {
            path = ApplyRegexFiltering(path);
        }
        if (ShowPreviewBeforeCopy)
        {
            ShowPathPreview(new[] { path });
        }
        else
        {
            GUIUtility.systemCopyBuffer = path;
        }
    }
    private static void CopyMultipleScenePaths()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if (selectedObjects.Length == 0) return;
        List<string> paths = new List<string>(selectedObjects.Length);
        foreach (GameObject obj in selectedObjects)
        {
            string path = GetScenePath(obj);
            if (EnableRegexFiltering)
            {
                path = ApplyRegexFiltering(path);
            }
            paths.Add(path);
        }

        if (ShowPreviewBeforeCopy)
        {
            ShowPathPreview(paths.ToArray());
        }
        else
        {
            string result = string.Join("\n", paths);
            GUIUtility.systemCopyBuffer = result;
        }
    }
    private static void ShowPathPreview(string[] paths)
    {
        PathPreviewWindow window = EditorWindow.GetWindow<PathPreviewWindow>(true, "Path Preview", true);
        window.SetPaths(paths);
        window.minSize = new Vector2(400, 300);
        window.Show();
    }
    [MenuItem("Tools/Scene Path Copy/Configure", false, 100)]
    private static void ConfigureRegexSettings()
    {
        ConfigWindow window = EditorWindow.GetWindow<ConfigWindow>(true, "Path Copy Config", true);
        window.minSize = new Vector2(350, 200);
        window.Show();
    }
    [MenuItem("GameObject/Copy Scene Path", true, 0)]
    private static bool ValidateCopyScenePath()
    {
        return Selection.activeGameObject != null;
    }
    private static string GetScenePath(GameObject obj)
    {
        if (obj == null) return string.Empty;
        StringBuilder path = new StringBuilder(128);
        Transform current = obj.transform;
        Stack<string> pathParts = new Stack<string>();
        while (current != null)
        {
            pathParts.Push(current.gameObject.name);
            current = current.parent;
        }
        bool isFirst = true;
        foreach (string part in pathParts)
        {
            if (!isFirst)
            {
                path.Append('/');
            }
            path.Append(part);
            isFirst = false;
        }
        return path.ToString();
    }
    private static string ApplyRegexFiltering(string path)
    {
        if (cachedRegex == null || lastRegexPattern != RegexPattern)
        {
            try
            {
                cachedRegex = new Regex(RegexPattern, RegexOptions.Compiled);
                lastRegexPattern = RegexPattern;
            }
            catch (System.ArgumentException ex)
            {
                Debug.LogError($"Invalid regex pattern: {ex.Message}");
                return path;
            }
        }
        if (RemoveMatchedParts)
        {
            string result = cachedRegex.Replace(path, "");
            result = Regex.Replace(result, "//+", "/");
            result = result.Trim('/');
            return result;
        }
        else
        {
            StringBuilder result = new StringBuilder();
            string[] parts = path.Split('/');
            bool isFirst = true;
            foreach (string part in parts)
            {
                if (cachedRegex.IsMatch(part))
                {
                    if (!isFirst)
                    {
                        result.Append('/');
                    }
                    result.Append(part);
                    isFirst = false;
                }
            }
            return result.ToString();
        }
    }
    public class ConfigWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            GUILayout.Label("Scene Path Copy Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            bool enableRegex = EnableRegexFiltering;
            enableRegex = EditorGUILayout.Toggle("Enable Regex Filtering", enableRegex);
            if (enableRegex != EnableRegexFiltering)
            {
                EnableRegexFiltering = enableRegex;
            }
            EditorGUI.BeginDisabledGroup(!enableRegex);
            string pattern = RegexPattern;
            pattern = EditorGUILayout.TextField("Regex Pattern", pattern);
            if (pattern != RegexPattern)
            {
                RegexPattern = pattern;
                cachedRegex = null;
            }
            bool removeMatched = RemoveMatchedParts;
            removeMatched = EditorGUILayout.Toggle("Remove Matched Parts", removeMatched);
            if (removeMatched != RemoveMatchedParts)
            {
                RemoveMatchedParts = removeMatched;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
            bool showPreview = ShowPreviewBeforeCopy;
            showPreview = EditorGUILayout.Toggle("Show Preview Before Copy", showPreview);
            if (showPreview != ShowPreviewBeforeCopy)
            {
                ShowPreviewBeforeCopy = showPreview;
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Apply"))
            {
                this.Close();
            }
            EditorGUILayout.HelpBox("Example: Pattern 'Effect\\d*' will match 'Effect', 'Effect1', 'Effect123', etc.", MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
    }
    public class PathPreviewWindow : EditorWindow
    {
        private string[] paths;
        private string[] editablePaths;
        private Vector2 scrollPosition;
        private bool showEditMode = false;
        private GUIStyle textAreaStyle;
        private bool isDirty = false;
        public void SetPaths(string[] paths)
        {
            this.paths = paths;
            this.editablePaths = new string[paths.Length];
            System.Array.Copy(paths, editablePaths, paths.Length);
            isDirty = false;
        }
        private void OnGUI()
        {
            if (paths == null || paths.Length == 0)
            {
                EditorGUILayout.HelpBox("No paths to display", MessageType.Info);
                return;
            }
            if (textAreaStyle == null)
            {
                textAreaStyle = new GUIStyle(EditorStyles.textArea);
                textAreaStyle.wordWrap = true;
            }
            GUILayout.Label("Path Preview", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool newEditMode = EditorGUILayout.Toggle("Edit Mode", showEditMode);
            if (newEditMode != showEditMode)
            {
                showEditMode = newEditMode;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < paths.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                if (showEditMode)
                {
                    editablePaths[i] = EditorGUILayout.TextField($"Path {i+1}", editablePaths[i], textAreaStyle);
                    if (GUILayout.Button("Reset", GUILayout.Width(50)))
                    {
                        editablePaths[i] = paths[i];
                        GUI.FocusControl(null);
                    }
                }
                else
                {
                    EditorGUILayout.SelectableLabel(editablePaths[i], EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }
                EditorGUILayout.EndHorizontal();
            }
            if (EditorGUI.EndChangeCheck())
            {
                isDirty = true;
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = true;
            if (GUILayout.Button("Copy All to Clipboard"))
            {
                GUIUtility.systemCopyBuffer = string.Join("\n", editablePaths);
                this.Close();
            }
            GUI.enabled = isDirty;
            if (GUILayout.Button("Copy Original"))
            {
                GUIUtility.systemCopyBuffer = string.Join("\n", paths);
                this.Close();
            }
            GUI.enabled = true;
            if (GUILayout.Button("Cancel"))
            {
                this.Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}