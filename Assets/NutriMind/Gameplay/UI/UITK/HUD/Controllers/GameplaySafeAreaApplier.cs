using UnityEngine;
using UnityEngine.UIElements;

namespace NutriMind.Gameplay.UI
{
    /// <summary>
    /// Converts <see cref="Screen.safeArea"/> insets into panel-space padding on a target element.
    /// </summary>
    internal sealed class GameplaySafeAreaApplier
    {
        private readonly VisualElement _safeAreaElement;
        private readonly VisualElement _scaleReferenceElement;
        private float _lastLeft = float.NaN;
        private float _lastRight = float.NaN;
        private float _lastTop = float.NaN;
        private float _lastBottom = float.NaN;

        public GameplaySafeAreaApplier(VisualElement safeAreaElement, VisualElement scaleReferenceElement)
        {
            _safeAreaElement = safeAreaElement;
            _scaleReferenceElement = scaleReferenceElement;
        }

        public void ApplyIfChanged()
        {
            if (_safeAreaElement == null || _scaleReferenceElement == null)
            {
                return;
            }

            float panelWidth = _scaleReferenceElement.resolvedStyle.width;
            float panelHeight = _scaleReferenceElement.resolvedStyle.height;
            if (panelWidth <= 0f || panelHeight <= 0f)
            {
                return;
            }

            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            float scaleX = panelWidth / screenWidth;
            float scaleY = panelHeight / screenHeight;

            float left = safeArea.xMin * scaleX;
            float right = (screenWidth - safeArea.xMax) * scaleX;
            float top = (screenHeight - safeArea.yMax) * scaleY;
            float bottom = safeArea.yMin * scaleY;

            if (Mathf.Approximately(left, _lastLeft)
                && Mathf.Approximately(right, _lastRight)
                && Mathf.Approximately(top, _lastTop)
                && Mathf.Approximately(bottom, _lastBottom))
            {
                return;
            }

            _lastLeft = left;
            _lastRight = right;
            _lastTop = top;
            _lastBottom = bottom;

            _safeAreaElement.style.paddingLeft = left;
            _safeAreaElement.style.paddingRight = right;
            _safeAreaElement.style.paddingTop = top;
            _safeAreaElement.style.paddingBottom = bottom;
        }

        public void ResetCache()
        {
            _lastLeft = float.NaN;
            _lastRight = float.NaN;
            _lastTop = float.NaN;
            _lastBottom = float.NaN;
        }
    }
}
