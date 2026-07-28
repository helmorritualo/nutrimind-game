using UnityEngine;
using UnityEngine.UI;

namespace NutriMind.Gameplay.Runtime
{
    /// <summary>
    /// Short world-space marker label above mission interactables/collectibles.
    /// Helps learners find placeholder objects during Areas 1–2.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class WorldSpaceInteractableLabel : MonoBehaviour
    {
        private static readonly Color AvailableColor = new Color(0.98f, 0.96f, 1f, 1f);
        private static readonly Color UnavailableColor = new Color(0.85f, 0.82f, 0.9f, 0.72f);
        private static readonly Color AvailableBackground = new Color(0.42f, 0.25f, 0.66f, 0.94f);
        private static readonly Color UnavailableBackground = new Color(0.28f, 0.24f, 0.34f, 0.7f);

        [SerializeField] private string _labelText = "Interact";
        [SerializeField] private float _topPadding = 0.55f;
        [SerializeField] private bool _facePlayerSpawn = true;
        [SerializeField] private bool _hideWhenUnavailable;
        [SerializeField] private Transform _faceToward;
        [SerializeField] private WorldInteractableBase _interactable;
        [SerializeField] private StoryFragmentCollectible _fragment;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Text _label;
        [SerializeField] private Image _background;

        public string LabelText => _labelText;

        public void Configure(
            string labelText,
            WorldInteractableBase interactable,
            StoryFragmentCollectible fragment,
            float topPadding,
            bool hideWhenUnavailable,
            Transform faceToward)
        {
            _labelText = labelText;
            _interactable = interactable;
            _fragment = fragment;
            _topPadding = Mathf.Max(0.2f, topPadding);
            _hideWhenUnavailable = hideWhenUnavailable;
            _faceToward = faceToward;
            _facePlayerSpawn = faceToward != null;
            EnsureBuilt();
            ApplyText();
            ApplyPlacement();
            RefreshVisibility();
        }

        public void SetLabelText(string labelText)
        {
            _labelText = labelText ?? string.Empty;
            ApplyText();
        }

        private void Awake()
        {
            EnsureBuilt();
            if (_interactable == null)
            {
                _interactable = GetComponentInParent<WorldInteractableBase>();
            }

            if (_fragment == null)
            {
                _fragment = GetComponentInParent<StoryFragmentCollectible>();
            }

            if (_faceToward == null)
            {
                _faceToward = FindPlayerSpawn();
            }

            ApplyText();
            ApplyPlacement();
            RefreshVisibility();
        }

        private void OnEnable()
        {
            ApplyPlacement();
            RefreshVisibility();
        }

        private void LateUpdate()
        {
            ApplyPlacement();
            RefreshVisibility();
        }

        private void ApplyPlacement()
        {
            Transform host = GetHostTransform();
            if (host == null)
            {
                return;
            }

            Vector3 top = GetHostTopWorldPosition(host);
            transform.position = top + Vector3.up * _topPadding;
            ApplyFacing();
        }

        private void ApplyFacing()
        {
            if (!_facePlayerSpawn)
            {
                return;
            }

            if (_faceToward == null)
            {
                _faceToward = FindPlayerSpawn();
            }

            if (_faceToward == null)
            {
                return;
            }

            // World-space Canvas is readable from the -Z side; aim +Z away from spawn
            // so the player at spawn looks at the front of the label immediately.
            Vector3 awayFromSpawn = transform.position - _faceToward.position;
            awayFromSpawn.y = 0f;
            if (awayFromSpawn.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(awayFromSpawn.normalized, Vector3.up);
        }

        private void EnsureBuilt()
        {
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(420f, 96f);
            }

            transform.localScale = Vector3.one * 0.012f;

            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                if (_canvas == null)
                {
                    _canvas = gameObject.AddComponent<Canvas>();
                }
            }

            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = 50;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.dynamicPixelsPerUnit = 10f;

            // Intentionally no GraphicRaycaster — labels must not block world interaction.

            if (_background == null)
            {
                Transform bgTransform = transform.Find("Background");
                GameObject bgGo = bgTransform != null
                    ? bgTransform.gameObject
                    : CreateUiChild("Background", transform);
                _background = bgGo.GetComponent<Image>();
                if (_background == null)
                {
                    _background = bgGo.AddComponent<Image>();
                }

                RectTransform bgRect = bgGo.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                _background.raycastTarget = false;
                _background.color = AvailableBackground;
            }

            if (_label == null)
            {
                Transform labelTransform = _background.transform.Find("Label");
                GameObject labelGo = labelTransform != null
                    ? labelTransform.gameObject
                    : CreateUiChild("Label", _background.transform);
                _label = labelGo.GetComponent<Text>();
                if (_label == null)
                {
                    _label = labelGo.AddComponent<Text>();
                }

                RectTransform labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(14f, 10f);
                labelRect.offsetMax = new Vector2(-14f, -10f);

                _label.alignment = TextAnchor.MiddleCenter;
                _label.fontSize = 44;
                _label.fontStyle = FontStyle.Bold;
                _label.color = AvailableColor;
                _label.horizontalOverflow = HorizontalWrapMode.Wrap;
                _label.verticalOverflow = VerticalWrapMode.Overflow;
                _label.raycastTarget = false;
                if (_label.font == null)
                {
                    _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_label.font == null)
                    {
                        _label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }
                }
            }

            Camera main = Camera.main;
            if (_canvas != null && main != null)
            {
                _canvas.worldCamera = main;
            }
        }

        private static GameObject CreateUiChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private void ApplyText()
        {
            if (_label != null)
            {
                _label.text = _labelText ?? string.Empty;
            }
        }

        private void RefreshVisibility()
        {
            bool available = IsCurrentlyAvailable();
            bool shouldShow = available || !_hideWhenUnavailable;

            if (_fragment != null)
            {
                shouldShow = _fragment.IsRevealed && !_fragment.IsCollected;
                available = shouldShow;
            }

            if (_canvas != null && _canvas.enabled != shouldShow)
            {
                _canvas.enabled = shouldShow;
            }

            if (!shouldShow)
            {
                return;
            }

            if (_label != null)
            {
                _label.color = available ? AvailableColor : UnavailableColor;
            }

            if (_background != null)
            {
                _background.color = available ? AvailableBackground : UnavailableBackground;
            }
        }

        private bool IsCurrentlyAvailable()
        {
            if (_fragment != null)
            {
                return _fragment.IsRevealed && !_fragment.IsCollected;
            }

            if (_interactable != null)
            {
                return _interactable.CanInteract;
            }

            return true;
        }

        private Transform GetHostTransform()
        {
            if (_interactable != null)
            {
                return _interactable.transform;
            }

            if (_fragment != null)
            {
                return _fragment.transform;
            }

            return transform.parent != null ? transform.parent : transform;
        }

        private static Vector3 GetHostTopWorldPosition(Transform host)
        {
            // Prefer the host's own collider/renderer so nested clue/board children
            // do not pull the label toward a sibling visual.
            if (TryGetOwnBounds(host, out Bounds ownBounds))
            {
                return new Vector3(host.position.x, ownBounds.max.y, host.position.z);
            }

            bool hasBounds = false;
            Bounds bounds = new Bounds(host.position, Vector3.zero);

            Renderer[] renderers = host.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!ShouldUseChildRenderer(host, renderer))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                Collider[] colliders = host.GetComponentsInChildren<Collider>();
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider collider = colliders[i];
                    if (!ShouldUseChildCollider(host, collider))
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = collider.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(collider.bounds);
                    }
                }
            }

            if (hasBounds)
            {
                return new Vector3(host.position.x, bounds.max.y, host.position.z);
            }

            return host.position + Vector3.up * 1.8f;
        }

        private static bool TryGetOwnBounds(Transform host, out Bounds bounds)
        {
            Collider ownCollider = host.GetComponent<Collider>();
            if (ownCollider != null && ownCollider.enabled)
            {
                bounds = ownCollider.bounds;
                return true;
            }

            Renderer ownRenderer = host.GetComponent<Renderer>();
            if (ownRenderer != null && ownRenderer.enabled && ownRenderer.GetComponentInParent<Canvas>() == null)
            {
                bounds = ownRenderer.bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        private static bool ShouldUseChildRenderer(Transform host, Renderer renderer)
        {
            if (renderer == null || !renderer.enabled)
            {
                return false;
            }

            if (renderer.GetComponentInParent<Canvas>() != null)
            {
                return false;
            }

            return IsOwnedByHostOnly(host, renderer.transform);
        }

        private static bool ShouldUseChildCollider(Transform host, Collider collider)
        {
            if (collider == null || !collider.enabled)
            {
                return false;
            }

            return IsOwnedByHostOnly(host, collider.transform);
        }

        private static bool IsOwnedByHostOnly(Transform host, Transform candidate)
        {
            Transform current = candidate;
            while (current != null && current != host)
            {
                if (current != host
                    && (current.GetComponent<WorldInteractableBase>() != null
                        || current.GetComponent<StoryFragmentCollectible>() != null
                        || current.GetComponent<WorldSpaceInteractableLabel>() != null))
                {
                    return false;
                }

                current = current.parent;
            }

            return current == host;
        }

        private static Transform FindPlayerSpawn()
        {
            MissionSceneBindings bindings = Object.FindFirstObjectByType<MissionSceneBindings>();
            if (bindings != null && bindings.PlayerSpawn != null)
            {
                return bindings.PlayerSpawn;
            }

            GameObject spawn = GameObject.Find("PlayerSpawnPoint");
            if (spawn != null)
            {
                return spawn.transform;
            }

            spawn = GameObject.Find("PlayerEntry_A01");
            return spawn != null ? spawn.transform : null;
        }
    }
}
