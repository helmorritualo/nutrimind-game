using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.App.UI
{
    /// <summary>Presentation-only Profile route view.</summary>
    public sealed class ProfilePanelView : IAppScreenView
    {
        private const string RootName = "profile-root";
        private const string CompactClass = "profile-panel--compact";
        private const string NarrowClass = "profile-panel--narrow";
        private const string MobileClass = "mobile";
        private const float CompactBreakpoint = 1100f;
        private const float NarrowBreakpoint = 820f;

        private VisualElement _root;
        private Button _backButton;
        private Button _settingsButton;
        private Button _signOutButton;
        private bool _disposed;
        private float _lastWidth = -1f;

        public ProfilePanelView(VisualElement root)
        {
            ResolveRoot(root);
            if (_root == null)
            {
                Debug.LogWarning("[ProfilePanelView] Could not resolve profile-root inside the supplied element.");
                return;
            }

            _backButton = _root.Q<Button>("back-button");
            _settingsButton = _root.Q<Button>("settings-button");
            _signOutButton = _root.Q<Button>("sign-out-button");
            RegisterCallbacks();
            ApplyResponsiveClasses(_root.resolvedStyle.width);
        }

        public VisualElement Root => _root;
        public bool IsBound => _root != null && !_disposed;
        public event Action BackRequested;
        public event Action SettingsRequested;
        public event Action SignOutRequested;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            UnregisterCallbacks();
            BackRequested = null;
            SettingsRequested = null;
            SignOutRequested = null;
            _root = null;
            _backButton = null;
            _settingsButton = null;
            _signOutButton = null;
            _lastWidth = -1f;
        }

        private void ResolveRoot(VisualElement root) => _root = root?.name == RootName ? root : root?.Q<VisualElement>(RootName);
        private void RegisterCallbacks()
        {
            _backButton?.RegisterCallback<ClickEvent>(OnBackClicked);
            _settingsButton?.RegisterCallback<ClickEvent>(OnSettingsClicked);
            _signOutButton?.RegisterCallback<ClickEvent>(OnSignOutClicked);
            _root?.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void UnregisterCallbacks()
        {
            _backButton?.UnregisterCallback<ClickEvent>(OnBackClicked);
            _settingsButton?.UnregisterCallback<ClickEvent>(OnSettingsClicked);
            _signOutButton?.UnregisterCallback<ClickEvent>(OnSignOutClicked);
            _root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnBackClicked(ClickEvent evt) => BackRequested?.Invoke();
        private void OnSettingsClicked(ClickEvent evt) => SettingsRequested?.Invoke();
        private void OnSignOutClicked(ClickEvent evt) => SignOutRequested?.Invoke();
        private void OnGeometryChanged(GeometryChangedEvent evt) => ApplyResponsiveClasses(evt.newRect.width);

        private void ApplyResponsiveClasses(float width)
        {
            if (_root == null || float.IsNaN(width) || width <= 0f || Mathf.Approximately(width, _lastWidth)) return;
            _lastWidth = width;
            bool compact = width < CompactBreakpoint;
            bool narrow = width < NarrowBreakpoint;
            _root.EnableInClassList(CompactClass, compact);
            _root.EnableInClassList(NarrowClass, narrow);
            _root.EnableInClassList(MobileClass, narrow);
        }
    }
}
