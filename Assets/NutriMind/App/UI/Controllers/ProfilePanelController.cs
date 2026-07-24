using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>
    /// Layout-only profile panel wiring for UI Toolkit preview.
    /// Handles responsive classes, static nav active state, the Sign Out
    /// confirmation preview, and static "View All" feedback.
    /// Does not perform routing, profile loading, sign-out, or networking —
    /// all actions here are static preview responses (Debug.Log only).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ProfilePanelController : MonoBehaviour
    {
        private const string CompactClass = "profile-panel--compact";
        private const string NarrowClass = "profile-panel--narrow";
        private const string MobileClass = "mobile";
        private const string ConfirmHiddenClass = "profile-panel__confirm-backdrop--hidden";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _nav;
        private VisualElement _signOutConfirmBackdrop;
        private Button _signOutButton;
        private Button _signOutCancelButton;
        private Button _signOutConfirmButton;
        private Button _avatarEditButton;
        private Button _achievementsViewAllButton;
        private Button _activityViewAllButton;
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

            _root = _uiDocument.rootVisualElement?.Q<VisualElement>("profile-root");
            if (_root == null)
            {
                Invoke(nameof(BindWhenReady), 0.05f);
                return;
            }

            var panelRoot = _uiDocument.rootVisualElement;
            panelRoot.style.flexGrow = 1;
            panelRoot.style.width = Length.Percent(100);
            panelRoot.style.height = Length.Percent(100);

            _nav = _root.Q<VisualElement>("profile-nav");
            if (_nav != null)
            {
                foreach (var button in _nav.Query<Button>(className: "profile-panel__nav-item").ToList())
                {
                    button.RegisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            _signOutConfirmBackdrop = _root.Q<VisualElement>("sign-out-confirm-backdrop");
            _signOutButton = _root.Q<Button>("sign-out-button");
            _signOutCancelButton = _root.Q<Button>("sign-out-cancel");
            _signOutConfirmButton = _root.Q<Button>("sign-out-confirm");
            _avatarEditButton = _root.Q<Button>("avatar-edit-button");
            _achievementsViewAllButton = _root.Q<Button>("achievements-view-all");
            _activityViewAllButton = _root.Q<Button>("activity-view-all");

            _signOutButton?.RegisterCallback<ClickEvent>(OnSignOutClicked);
            _signOutCancelButton?.RegisterCallback<ClickEvent>(OnSignOutCancelClicked);
            _signOutConfirmButton?.RegisterCallback<ClickEvent>(OnSignOutConfirmClicked);
            _avatarEditButton?.RegisterCallback<ClickEvent>(OnAvatarEditClicked);
            _achievementsViewAllButton?.RegisterCallback<ClickEvent>(OnAchievementsViewAllClicked);
            _activityViewAllButton?.RegisterCallback<ClickEvent>(OnActivityViewAllClicked);

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        private void Unbind()
        {
            if (_nav != null)
            {
                foreach (var button in _nav.Query<Button>(className: "profile-panel__nav-item").ToList())
                {
                    button.UnregisterCallback<ClickEvent>(OnNavClickEvent);
                }
            }

            _signOutButton?.UnregisterCallback<ClickEvent>(OnSignOutClicked);
            _signOutCancelButton?.UnregisterCallback<ClickEvent>(OnSignOutCancelClicked);
            _signOutConfirmButton?.UnregisterCallback<ClickEvent>(OnSignOutConfirmClicked);
            _avatarEditButton?.UnregisterCallback<ClickEvent>(OnAvatarEditClicked);
            _achievementsViewAllButton?.UnregisterCallback<ClickEvent>(OnAchievementsViewAllClicked);
            _activityViewAllButton?.UnregisterCallback<ClickEvent>(OnActivityViewAllClicked);

            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            _root = null;
            _nav = null;
            _signOutConfirmBackdrop = null;
            _signOutButton = null;
            _signOutCancelButton = null;
            _signOutConfirmButton = null;
            _avatarEditButton = null;
            _achievementsViewAllButton = null;
            _activityViewAllButton = null;
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

            _nav.Query<Button>(className: "profile-panel__nav-item").ForEach(button =>
            {
                button.EnableInClassList("is-active", button == selected);
            });
        }

        private void OnSignOutClicked(ClickEvent evt)
        {
            _signOutConfirmBackdrop?.RemoveFromClassList(ConfirmHiddenClass);
        }

        private void OnSignOutCancelClicked(ClickEvent evt)
        {
            _signOutConfirmBackdrop?.AddToClassList(ConfirmHiddenClass);
        }

        private void OnSignOutConfirmClicked(ClickEvent evt)
        {
            // Static preview only — actual sign-out (session clear, routing to
            // Login) is wired once App routing and auth exist.
            Debug.Log("[ProfilePanelController] Sign out confirmed (static preview — no session was cleared).");
            _signOutConfirmBackdrop?.AddToClassList(ConfirmHiddenClass);
        }

        private void OnAvatarEditClicked(ClickEvent evt)
        {
            Debug.Log("[ProfilePanelController] Avatar editor opened (static preview).");
        }

        private void OnAchievementsViewAllClicked(ClickEvent evt)
        {
            Debug.Log("[ProfilePanelController] View All Achievements tapped (static preview).");
        }

        private void OnActivityViewAllClicked(ClickEvent evt)
        {
            Debug.Log("[ProfilePanelController] View All Activity tapped (static preview).");
        }
    }
}
