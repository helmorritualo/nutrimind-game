#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading;
using NutriMind.Core.Bootstrap;
using NutriMind.Core.Data;
using NutriMind.Core.Utilities;
using UnityEngine;

namespace NutriMind.App.Composition
{
    /// <summary>
    /// Hidden development-only IMGUI overlay for Mock controls.
    /// Open via the small DEV button or a deliberate triple-tap gesture zone.
    /// Never displays PIN or bearer token.
    /// </summary>
    public sealed class DevelopmentMockMenu : MonoBehaviour
    {
        private bool _visible;
        private bool _confirmDatabaseReset;
        private bool _confirmFullReset;
        private Vector2 _scroll;
        private string _status;
        private int _gestureTaps;
        private float _gestureWindowEnd;

        public void ToggleVisible()
        {
            _visible = !_visible;
            _confirmDatabaseReset = false;
            _confirmFullReset = false;
        }

        private void OnGUI()
        {
            // Prefer IMGUI events — project uses Input System only (activeInputHandler=1),
            // so UnityEngine.Input.GetKeyDown throws and can disrupt login UI.
            if (Event.current != null
                && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.F8)
            {
                ToggleVisible();
                Event.current.Use();
            }

            if (!AppLifetime.HasInstance)
            {
                return;
            }

            DevelopmentMockRuntimeController controller =
                GetComponent<DevelopmentMockRuntimeController>();
            if (controller == null)
            {
                return;
            }

            DrawDevButton();
            HandleCornerGesture();

            if (!_visible)
            {
                return;
            }

            const float width = 420f;
            const float height = 460f;
            Rect area = new Rect(12f, 48f, width, height);
            GUI.Box(area, "NutriMind DEV Mock Controls");

            GUILayout.BeginArea(new Rect(area.x + 10f, area.y + 28f, area.width - 20f, area.height - 36f));
            _scroll = GUILayout.BeginScrollView(_scroll);

            GUILayout.Label("Scenario");
            MockApiScenario current = controller.Scenario;
            foreach (MockApiScenario scenario in Enum.GetValues(typeof(MockApiScenario)))
            {
                bool selected = scenario == current;
                if (GUILayout.Toggle(selected, scenario.ToString()) && !selected)
                {
                    controller.Scenario = scenario;
                }
            }

            GUILayout.Space(8f);
            bool online = GUILayout.Toggle(controller.IsOnline, "Online");
            if (online != controller.IsOnline)
            {
                controller.IsOnline = online;
            }

            GUILayout.Space(8f);
            GUILayout.Label("Database path");
            GUILayout.TextArea(controller.DatabasePath ?? string.Empty, GUILayout.MinHeight(40f));
            GUILayout.Label("Pending outbox count: " + controller.GetOutboxCount());

            GUILayout.Label("Cache keys");
            foreach (string key in controller.GetKnownCacheKeys())
            {
                GUILayout.Label(" • " + key);
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("Reset mock server state"))
            {
                // Mock-server reset does not cancel lifetime; LifetimeToken is safe here.
                TaskUtilities.ForgetSafely(
                    controller.ResetMockServerAsync(AppLifetime.Instance.LifetimeToken),
                    AppLifetime.Instance.LifetimeToken,
                    "ResetMockServer");
                _status = "Mock server reset requested.";
            }

            if (!_confirmDatabaseReset)
            {
                if (GUILayout.Button("Reset local database…"))
                {
                    _confirmDatabaseReset = true;
                }
            }
            else
            {
                GUILayout.Label("Confirm delete nutrimind.db?");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Confirm DB reset"))
                {
                    _confirmDatabaseReset = false;
                    // Destructive resets cancel LifetimeToken — never pass it as the caller token.
                    TaskUtilities.ForgetSafely(
                        controller.ResetLocalDatabaseAsync(CancellationToken.None),
                        CancellationToken.None,
                        "ResetLocalDatabase");
                    _status = "Local database reset requested.";
                    _visible = false;
                }

                if (GUILayout.Button("Cancel"))
                {
                    _confirmDatabaseReset = false;
                }

                GUILayout.EndHorizontal();
            }

            if (!_confirmFullReset)
            {
                if (GUILayout.Button("Full installation reset…"))
                {
                    _confirmFullReset = true;
                }
            }
            else
            {
                GUILayout.Label("Confirm full installation reset?");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Confirm full reset"))
                {
                    _confirmFullReset = false;
                    TaskUtilities.ForgetSafely(
                        controller.FullInstallationResetAsync(CancellationToken.None),
                        CancellationToken.None,
                        "FullInstallationReset");
                    _status = "Full installation reset requested.";
                    _visible = false;
                }

                if (GUILayout.Button("Cancel"))
                {
                    _confirmFullReset = false;
                }

                GUILayout.EndHorizontal();
            }

            if (!string.IsNullOrEmpty(_status))
            {
                GUILayout.Space(8f);
                GUILayout.Label(_status);
            }

            if (GUILayout.Button("Close"))
            {
                _visible = false;
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawDevButton()
        {
            if (GUI.Button(new Rect(8f, 8f, 56f, 28f), "DEV"))
            {
                ToggleVisible();
            }
        }

        private void HandleCornerGesture()
        {
            // Deliberate hidden gesture: three quick taps in the top-right corner.
            if (Event.current == null || Event.current.type != EventType.MouseDown)
            {
                return;
            }

            Rect corner = new Rect(Screen.width - 64f, 0f, 64f, 64f);
            if (!corner.Contains(Event.current.mousePosition))
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now > _gestureWindowEnd)
            {
                _gestureTaps = 0;
            }

            _gestureWindowEnd = now + 1.25f;
            _gestureTaps++;
            if (_gestureTaps >= 3)
            {
                _gestureTaps = 0;
                ToggleVisible();
                Event.current.Use();
            }
        }
    }
}
#endif
