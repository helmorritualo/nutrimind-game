using NutriMind.App.Presentation;
using NutriMind.App.Routing;
using NutriMind.App.UI;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Utilities;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.Composition
{
    /// <summary>
    /// Quiz Portal scene root.
    /// Creates <see cref="QuizPortalScreenCoordinator"/> and <see cref="AppShellRuntimeController"/>
    /// when the shell document is ready. All quiz-route UXML assets are assigned here in the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppQuizPortalSceneRoot : MonoBehaviour
    {
        [Header("Shell")]
        [SerializeField]
        private UIDocument _shellDocument;

        [SerializeField]
        private AppShellController _shellController;

        [SerializeField]
        private VisualTreeAsset _confirmDialogAsset;

        [SerializeField]
        private VisualTreeAsset _systemDialogAsset;

        [Header("Quiz Route UXML Assets")]
        [SerializeField]
        private VisualTreeAsset _quizListTreeAsset;

        [SerializeField]
        private VisualTreeAsset _quizDetailAsset;

        [SerializeField]
        private VisualTreeAsset _quizAttemptAsset;

        [SerializeField]
        private VisualTreeAsset _quizResultAsset;

        [SerializeField]
        private VisualTreeAsset _quizHistoryAsset;

        [SerializeField]
        private VisualTreeAsset _dataStatePanelAsset;

        private QuizPortalScreenCoordinator _coordinator;
        private AppShellRuntimeController _shellRuntime;

        private void Awake()
        {
            if (_shellDocument == null)
            {
                _shellDocument = GetComponent<UIDocument>();
            }

            if (_shellController == null)
            {
                _shellController = GetComponent<AppShellController>();
            }
        }

        private void OnEnable()
        {
            if (AppLifetime.HasInstance)
            {
                AppLifetime.Instance.Router?.EnsureQuizPortalRoot();
            }

            BindWhenReady();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(BindWhenReady));
            TeardownCoordinator();
        }

        private void BindWhenReady()
        {
            if (_shellDocument == null)
            {
                _shellDocument = GetComponent<UIDocument>();
            }

            if (_shellController == null)
            {
                _shellController = GetComponent<AppShellController>();
            }

            VisualElement root = _shellDocument != null ? _shellDocument.rootVisualElement : null;
            if (root == null || root.Q<VisualElement>("app-shell-root") == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            StretchDocument(root);

            if (!AppLifetime.HasInstance || !AppLifetime.Instance.IsReady)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            TeardownCoordinator();
            BuildCoordinator();

            NutriMindLog.Runtime("AppQuizPortalSceneRoot ready — coordinator active.");
        }

        private void BuildCoordinator()
        {
            AppLifetime lifetime = AppLifetime.Instance;

            AppModalHost modalHost = null;
            VisualElement modalLayer = _shellController?.GetModalLayer();
            if (modalLayer != null)
            {
                modalHost = new AppModalHost(modalLayer, _confirmDialogAsset, _systemDialogAsset);
            }

            _shellRuntime = new AppShellRuntimeController(
                _shellController,
                lifetime.Router,
                lifetime.AuthenticatedStudentState,
                lifetime.Connectivity,
                lifetime.SyncCoordinator,
                modalHost,
                lifetime.LifetimeToken);

            _shellRuntime.SignOutConfirmed += OnSignOutConfirmed;

            var assets = new QuizPortalScreenAssets
            {
                QuizListAsset = _quizListTreeAsset,
                QuizDetailAsset = _quizDetailAsset,
                QuizAttemptAsset = _quizAttemptAsset,
                QuizResultAsset = _quizResultAsset,
                QuizHistoryAsset = _quizHistoryAsset,
                DataStatePanelAsset = _dataStatePanelAsset
            };

            _coordinator = new QuizPortalScreenCoordinator(
                lifetime,
                _shellController,
                _shellRuntime,
                assets);

            _coordinator.ApplyCurrentRoute();
        }

        private void TeardownCoordinator()
        {
            if (_shellRuntime != null)
            {
                _shellRuntime.SignOutConfirmed -= OnSignOutConfirmed;
                _shellRuntime.Dispose();
                _shellRuntime = null;
            }

            _coordinator?.Dispose();
            _coordinator = null;
        }

        private void OnSignOutConfirmed()
        {
            if (!AppLifetime.HasInstance)
            {
                return;
            }

            TaskUtilities.ForgetSafely(
                AppLifetime.Instance.HandleUnauthorizedAsync(AppLifetime.Instance.LifetimeToken),
                AppLifetime.Instance.LifetimeToken,
                "QuizPortal.SignOut");
        }

        private static void StretchDocument(VisualElement root)
        {
            root.style.flexGrow = 1;
            root.style.width = Length.Percent(100);
            root.style.height = Length.Percent(100);
        }
    }
}
