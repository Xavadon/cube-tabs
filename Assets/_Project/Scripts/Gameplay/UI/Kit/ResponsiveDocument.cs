using _Project.Scripts.Architecture.Utils;
using GamePush;
using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Kit
{
    [RequireComponent(typeof(UIDocument))]
    public class ResponsiveDocument : MonoBehaviour
    {
        private const float MinUnitSize = 0.5f;
        private const float ScaleEpsilon = 0.001f;
        private const string CompactClass = "compact";
        private const string TouchClass = "touch";

        [SerializeField]
        private UIDocument _document;

        private PanelSettings _panelSettings;
        private VisualElement _root;
        private int _screenWidth;
        private int _screenHeight;
        private Rect _safeArea;
        private float _scale = 1f;

        private void Start()
        {
            _panelSettings = _document.panelSettings;
            _root = _document.rootVisualElement;
            _root.style.position = Position.Absolute;
            _root.EnableInClassList(TouchClass, GP_Device.IsMobile());
            Apply();
        }

        private void Update()
        {
            if (IsChanged())
            {
                Apply();
            }
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            if (_panelSettings != null)
            {
                _panelSettings.scale = 1f;
            }
        }
#endif

        private bool IsChanged()
        {
            if (_root == null)
            {
                return false;
            }

            return Screen.width != _screenWidth
                || Screen.height != _screenHeight
                || Screen.safeArea != _safeArea
                || !Mathf.Approximately(_panelSettings.scale, _scale);
        }

        private void Apply()
        {
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
            _safeArea = Screen.safeArea;

            if (_screenWidth <= 0 || _screenHeight <= 0)
            {
                return;
            }

            Vector2 reference = _panelSettings.referenceResolution;
            float fitScale = Mathf.Min(_screenWidth / reference.x, _screenHeight / reference.y);
            float unitSize = fitScale / ScreenDensity.GetPixelRatio();

            _scale = 1f;
            if (unitSize < MinUnitSize)
            {
                _scale = MinUnitSize / unitSize;
            }

            _panelSettings.scale = _scale;
            _root.EnableInClassList(CompactClass, _scale > 1f + ScaleEpsilon);
            ApplySafeArea(fitScale * _scale);
        }

        private void ApplySafeArea(float pixelsPerUnit)
        {
            _root.style.left = _safeArea.xMin / pixelsPerUnit;
            _root.style.right = (_screenWidth - _safeArea.xMax) / pixelsPerUnit;
            _root.style.top = (_screenHeight - _safeArea.yMax) / pixelsPerUnit;
            _root.style.bottom = _safeArea.yMin / pixelsPerUnit;
        }
    }
}
