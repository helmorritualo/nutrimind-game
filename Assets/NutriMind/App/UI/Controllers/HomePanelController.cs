using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Layout-only home panel wiring for UI Toolkit preview.
    /// Handles responsive classes, bottom nav selection, and static click
    /// feedback (Debug.Log plus a small toast) for Play Adventure, Quiz
    /// Portal, the avatar, and every nav item.
    /// Does not perform routing, progress loading, sync, or networking.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class HomePanelController : MonoBehaviour
    {
        private const string CompactClass = "home-panel--compact";
        private const string NarrowClass = "home-panel--narrow";
        private const string MobileClass = "mobile";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;
        private const float ToastDurationSeconds = 2.5f;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _nav;
        private VisualElement _toast;
        private Label _toastLabel;
        private Button _playContinueButton;
        private Button _quizGoButton;
        private VisualElement _avatar;
        private Clickable _avatarClickable;
        private float _lastWidth = -1f;

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            BindWhenReady();
        }

        private void OnDisable()
        {
            Unbind();
            CancelInvoke(nameof(BindWhenReady));
            CancelInvoke(nameof(HideToast));
        }

        private void Update()
        {
            if (_root == null)
            {
                return;
            }

            float width = _root.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0f || Mathf.Approximately(width, _lastWidth))
            {
                return;
            }

            _lastWidth = width;
            ApplyResponsiveClasses(width);
        }

        private void BindWhenReady()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument == null)
            {
                return;
            }

            _root = _uiDocument.rootVisualElement?.Q<VisualElement>("home-root");
            if (_root == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            var panelRoot = _uiDocument.rootVisualElement;
            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            _toast = _root.Q<VisualElement>("home-toast");
            _toastLabel = _root.Q<Label>("home-toast-label");

            _nav = _root.Q<VisualElement>("home-nav");
            if (_nav != null)
            {
                foreach (var button in _nav.Query<Button>(className: "home-panel__nav-item").ToList())
                {
                    button.RegisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            _playContinueButton = _root.Q<Button>("play-continue-button");
            _playContinueButton?.RegisterCallback<ClickEvent>(OnPlayContinueClicked);

            _quizGoButton = _root.Q<Button>("quiz-go-button");
            _quizGoButton?.RegisterCallback<ClickEvent>(OnQuizGoClicked);

            _avatar = _root.Q<VisualElement>("home-avatar");
            if (_avatar != null)
            {
                _avatarClickable = new Clickable(OnAvatarClicked);
                _avatar.AddManipulator(_avatarClickable);
            }

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        private void Unbind()
        {
            if (_nav != null)
            {
                foreach (var button in _nav.Query<Button>(className: "home-panel__nav-item").ToList())
                {
                    button.UnregisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            _playContinueButton?.UnregisterCallback<ClickEvent>(OnPlayContinueClicked);
            _quizGoButton?.UnregisterCallback<ClickEvent>(OnQuizGoClicked);

            if (_avatar != null && _avatarClickable != null)
            {
                _avatar.RemoveManipulator(_avatarClickable);
            }

            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            _root = null;
            _nav = null;
            _toast = null;
            _toastLabel = null;
            _playContinueButton = null;
            _quizGoButton = null;
            _avatar = null;
            _avatarClickable = null;
            _lastWidth = -1f;
        }

        private void OnNavClickEvent(ClickEvent evt)
        {
            if (evt.currentTarget is Button button)
            {
                OnNavClicked(button);
            }
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            ApplyResponsiveClasses(evt.newRect.width);
        }

        private void ApplyResponsiveClasses(float width)
        {
            if (_root == null || float.IsNaN(width) || width <= 0f)
            {
                return;
            }

            bool compact = width < CompactBreakpoint;
            bool narrow = width < NarrowBreakpoint;

            _root.EnableInClassList(CompactClass, compact);
            _root.EnableInClassList(NarrowClass, narrow);
            _root.EnableInClassList(MobileClass, narrow);
        }

        private void OnNavClicked(Button selected)
        {
            if (_nav == null || selected == null)
            {
                return;
            }

            _nav.Query<Button>(className: "home-panel__nav-item").ForEach(button =>
            {
                button.EnableInClassList("is-active", button == selected);
            });

            string navLabel = selected.Q<Label>(className: "home-panel__nav-label")?.text ?? selected.name;
            Debug.Log($"[HomePanel] Nav item selected: {selected.name} ({navLabel}).");
            ShowToast($"{navLabel} selected — preview only.");
        }

        private void OnPlayContinueClicked(ClickEvent evt)
        {
            Debug.Log("[HomePanel] Play Adventure > Continue tapped.");
            ShowToast("Preview only — Play Adventure will launch the mission scene.");
        }

        private void OnQuizGoClicked(ClickEvent evt)
        {
            Debug.Log("[HomePanel] Quiz Portal > Go to Quizzes tapped.");
            ShowToast("Preview only — this will open the Quiz Portal.");
        }

        private void OnAvatarClicked()
        {
            Debug.Log("[HomePanel] Avatar tapped.");
            ShowToast("Preview only — this will open your Profile.");
        }

        private void ShowToast(string message)
        {
            if (_toast == null || _toastLabel == null)
            {
                return;
            }

            _toastLabel.text = message ?? string.Empty;
            _toast.EnableInClassList("is-visible", true);
            CancelInvoke(nameof(HideToast));
            Invoke(nameof(HideToast), ToastDurationSeconds);
        }

        private void HideToast()
        {
            _toast?.EnableInClassList("is-visible", false);
        }
    }
}
