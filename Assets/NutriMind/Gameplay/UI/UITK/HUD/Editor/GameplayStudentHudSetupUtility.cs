using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using NutriMind.Gameplay.UI;

namespace NutriMind.Editor
{
    /// <summary>
    /// Idempotent editor utility for the student gameplay HUD preview setup.
    /// </summary>
    public static class GameplayStudentHudSetupUtility
    {
        private const string MenuPath = "NutriMind/Gameplay UI/Create or Refresh Student HUD Preview";
        private const string PanelSettingsPath = "Assets/NutriMind/Gameplay/UI/Settings/PS_GameplayStudentHud.asset";
        private const string UxmlPath = "Assets/NutriMind/Gameplay/UI/UITK/HUD/UXML/GameplayStudentHud.uxml";
        private const string PrefabFolderPath = "Assets/NutriMind/Gameplay/UI/UITK/HUD/Prefabs";
        private const string PrefabPath = PrefabFolderPath + "/PF_GameplayStudentHudPreview.prefab";
        private const int GameplayHudSortingOrder = 100;

        [MenuItem(MenuPath)]
        public static void CreateOrRefreshStudentHudPreview()
        {
            EnsureFolder("Assets/NutriMind/Gameplay/UI/Settings");
            EnsureFolder(PrefabFolderPath);

            PanelSettings panelSettings = CreateOrRefreshPanelSettings();
            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (uxml == null)
            {
                Debug.LogError("[GameplayStudentHudSetupUtility] Missing UXML at " + UxmlPath);
                return;
            }

            GameObject previewRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (previewRoot == null)
            {
                previewRoot = new GameObject("PF_GameplayStudentHudPreview");
            }

            UIDocument uiDocument = previewRoot.GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = previewRoot.AddComponent<UIDocument>();
            }

            if (previewRoot.GetComponent<GameplayStudentHudPreviewController>() == null)
            {
                previewRoot.AddComponent<GameplayStudentHudPreviewController>();
            }

            uiDocument.panelSettings = panelSettings;
            uiDocument.visualTreeAsset = uxml;
            uiDocument.sortingOrder = GameplayHudSortingOrder;

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(previewRoot, PrefabPath);
            Object.DestroyImmediate(previewRoot);

            Selection.activeObject = savedPrefab != null ? savedPrefab : panelSettings;
            EditorGUIUtility.PingObject(Selection.activeObject);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[GameplayStudentHudSetupUtility] Student HUD preview refreshed. PanelSettings: "
                + PanelSettingsPath
                + ", Prefab: "
                + PrefabPath);
        }

        private static PanelSettings CreateOrRefreshPanelSettings()
        {
            PanelSettings existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (existing != null)
            {
                ConfigurePanelSettings(existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            PanelSettings created = ScriptableObject.CreateInstance<PanelSettings>();
            ConfigurePanelSettings(created);
            AssetDatabase.CreateAsset(created, PanelSettingsPath);
            return created;
        }

        private static void ConfigurePanelSettings(PanelSettings panelSettings)
        {
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            panelSettings.sortingOrder = GameplayHudSortingOrder;
            panelSettings.clearColor = true;
            panelSettings.colorClearValue = new Color(0f, 0f, 0f, 0f);
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            string normalized = assetFolderPath.Replace('\\', '/');
            string parent = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            string folderName = Path.GetFileName(normalized);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
