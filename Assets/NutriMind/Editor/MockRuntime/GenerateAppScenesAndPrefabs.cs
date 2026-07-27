#if UNITY_EDITOR
using System.IO;
using NutriMind.App.Composition;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace NutriMind.Editor.MockRuntime
{
    /// <summary>
    /// Generates App lifetime prefab, four application scenes, and Build Settings entries
    /// using EditorSceneManager (never invents scene YAML GUIDs by hand).
    /// </summary>
    public static class GenerateAppScenesAndPrefabs
    {
        private const string ScenesFolder = "Assets/NutriMind/App/Scenes";
        private const string PrefabsFolder = "Assets/NutriMind/App/Prefabs";
        private const string PrefabPath = PrefabsFolder + "/PFB_AppLifetime.prefab";

        private const string BootstrapUxml = "Assets/NutriMind/App/UI/UXML/BootstrapPanel.uxml";
        private const string LoginUxml = "Assets/NutriMind/App/UI/UXML/LoginPanel.uxml";
        private const string AppShellUxml = "Assets/NutriMind/App/UI/UXML/AppShell.uxml";
        private const string HomeUxml = "Assets/NutriMind/App/UI/UXML/HomePanel.uxml";
        private const string QuizListUxml = "Assets/NutriMind/App/UI/UXML/QuizListPanel.uxml";
        private const string PanelSettings = "Assets/NutriMind/Settings/UI/PS_AppPanels.asset";

        [MenuItem("NutriMind/Mock Runtime/Generate App Scenes And Prefabs")]
        public static void Generate()
        {
            EnsureFolder("Assets/NutriMind");
            EnsureFolder("Assets/NutriMind/App");
            EnsureFolder(ScenesFolder);
            EnsureFolder(PrefabsFolder);

            string prefabPath = CreateOrUpdateLifetimePrefab();
            CreateBootstrapScene(prefabPath);
            CreateAuthenticationScene();
            CreateMainScene();
            CreateQuizPortalScene();
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "NutriMind App Scenes",
                "Generated PFB_AppLifetime and SCN_App_* scenes. Build Settings updated.",
                "OK");
        }

        [MenuItem("NutriMind/Mock Runtime/Open Mock Runtime Window")]
        public static void OpenWindow()
        {
            MockRuntimeEditorWindow.ShowWindow();
        }

        private static string CreateOrUpdateLifetimePrefab()
        {
            var go = new GameObject(AppLifetime.LifetimeObjectName);
            AppLifetime lifetime = go.AddComponent<AppLifetime>();
            SerializedObject so = new SerializedObject(lifetime);
            SerializedProperty options = so.FindProperty("_runtimeOptions");
            if (options != null)
            {
                // Leave defaults (Mock mode) — Inspector can override on the prefab.
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            string path = PrefabPath;
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return path;
        }

        private static void CreateBootstrapScene(string prefabPath)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject lifetime = PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)) as GameObject;
            if (lifetime == null)
            {
                lifetime = new GameObject(AppLifetime.LifetimeObjectName);
                lifetime.AddComponent<AppLifetime>();
            }

            GameObject ui = new GameObject("BootstrapUI");
            UIDocument document = ui.AddComponent<UIDocument>();
            AssignPanelSettings(document);
            AssignSourceAsset(document, BootstrapUxml);
            ui.AddComponent<AppBootstrapSceneRoot>();

            EditorSceneManager.SaveScene(scene, ScenesFolder + "/" + AppSceneNavigator.BootstrapSceneName + ".unity");
        }

        private static void CreateAuthenticationScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject ui = new GameObject("AuthenticationUI");
            UIDocument document = ui.AddComponent<UIDocument>();
            AssignPanelSettings(document);
            AssignSourceAsset(document, LoginUxml);
            ui.AddComponent<AppAuthenticationSceneRoot>();
            EditorSceneManager.SaveScene(
                scene,
                ScenesFolder + "/" + AppSceneNavigator.AuthenticationSceneName + ".unity");
        }

        private static void CreateMainScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject ui = new GameObject("MainUI");
            UIDocument document = ui.AddComponent<UIDocument>();
            AssignPanelSettings(document);
            AssignSourceAsset(document, AppShellUxml);
            AppShellController shell = ui.AddComponent<AppShellController>();
            AppMainSceneRoot root = ui.AddComponent<AppMainSceneRoot>();

            SerializedObject rootSo = new SerializedObject(root);
            rootSo.FindProperty("_shellDocument").objectReferenceValue = document;
            rootSo.FindProperty("_shellController").objectReferenceValue = shell;
            rootSo.FindProperty("_homePanelAsset").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HomeUxml);
            rootSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenesFolder + "/" + AppSceneNavigator.MainSceneName + ".unity");
        }

        private static void CreateQuizPortalScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject ui = new GameObject("QuizPortalUI");
            UIDocument document = ui.AddComponent<UIDocument>();
            AssignPanelSettings(document);
            AssignSourceAsset(document, AppShellUxml);
            AppShellController shell = ui.AddComponent<AppShellController>();
            AppQuizPortalSceneRoot root = ui.AddComponent<AppQuizPortalSceneRoot>();

            SerializedObject rootSo = new SerializedObject(root);
            rootSo.FindProperty("_shellDocument").objectReferenceValue = document;
            rootSo.FindProperty("_shellController").objectReferenceValue = shell;
            rootSo.FindProperty("_quizListTreeAsset").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(QuizListUxml);
            rootSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(
                scene,
                ScenesFolder + "/" + AppSceneNavigator.QuizPortalSceneName + ".unity");
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(
                    ScenesFolder + "/" + AppSceneNavigator.BootstrapSceneName + ".unity", true),
                new EditorBuildSettingsScene(
                    ScenesFolder + "/" + AppSceneNavigator.AuthenticationSceneName + ".unity", true),
                new EditorBuildSettingsScene(
                    ScenesFolder + "/" + AppSceneNavigator.MainSceneName + ".unity", true),
                new EditorBuildSettingsScene(
                    ScenesFolder + "/" + AppSceneNavigator.QuizPortalSceneName + ".unity", true)
            };

            EditorBuildSettings.scenes = scenes;
        }

        private static void AssignPanelSettings(UIDocument document)
        {
            PanelSettings settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettings);
            if (settings == null)
            {
                Debug.LogWarning("[NutriMind] PS_AppPanels not found at " + PanelSettings);
                return;
            }

            document.panelSettings = settings;
        }

        private static void AssignSourceAsset(UIDocument document, string uxmlPath)
        {
            VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (asset == null)
            {
                Debug.LogWarning("[NutriMind] UXML not found: " + uxmlPath);
                return;
            }

            document.visualTreeAsset = asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }

    public sealed class MockRuntimeEditorWindow : EditorWindow
    {
        [MenuItem("NutriMind/Mock Runtime/Mock Controls")]
        public static void ShowWindow()
        {
            GetWindow<MockRuntimeEditorWindow>("NutriMind Mock");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Mock Runtime Controls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Editor / DEVELOPMENT_BUILD only. Does not ship in production UI.",
                MessageType.Info);

            if (!Application.isPlaying || !AppLifetime.HasInstance)
            {
                EditorGUILayout.HelpBox("Enter Play Mode from SCN_App_Bootstrap to use live controls.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || !AppLifetime.HasInstance);

            MockApiScenario scenario = (MockApiScenario)EditorGUILayout.EnumPopup(
                "Scenario",
                MockRuntimeControls.SelectedScenario);
            if (scenario != MockRuntimeControls.SelectedScenario)
            {
                MockRuntimeControls.SelectedScenario = scenario;
            }

            bool online = EditorGUILayout.Toggle("Online", MockRuntimeControls.IsOnline);
            if (online != MockRuntimeControls.IsOnline)
            {
                MockRuntimeControls.IsOnline = online;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Database path", MockRuntimeControls.DatabasePath);
            EditorGUILayout.LabelField("Outbox count", MockRuntimeControls.GetOutboxCount().ToString());
            EditorGUILayout.LabelField("Cache keys");
            foreach (string key in MockRuntimeControls.GetKnownCacheKeys())
            {
                EditorGUILayout.LabelField(" • " + key);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Reset Mock Server State"))
            {
                MockRuntimeControls.ResetMockServer();
            }

            if (GUILayout.Button("Reset Database…"))
            {
                MockRuntimeControls.ResetDatabase();
            }

            if (GUILayout.Button("Full Installation Reset…"))
            {
                MockRuntimeControls.FullInstallationReset();
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif
