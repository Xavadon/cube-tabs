using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.Editor
{
    public class CubePlacerTool : EditorWindow
    {
        private enum Axis { XZ, XY, ZY }

        private Axis _planeAxis = Axis.XZ;
        private float _planeOffset;
        private float _cubeSize = 1f;
        private bool _snapToGrid = true;
        private bool _isActive;
        private Transform _parentTransform;
        private GameObject _prefab;

        private bool _isDragging;
        private Vector3 _dragStart;
        private Vector3 _dragCurrent;

        [MenuItem("Tools/Cube Placer")]
        private static void Open()
        {
            GetWindow<CubePlacerTool>("Cube Placer");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            _isActive = false;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);

            _planeAxis = (Axis)EditorGUILayout.EnumPopup("Plane", _planeAxis);
            _planeOffset = EditorGUILayout.FloatField("Plane Offset", _planeOffset);
            _cubeSize = Mathf.Max(0.1f, EditorGUILayout.FloatField("Cube Size", _cubeSize));
            _snapToGrid = EditorGUILayout.Toggle("Snap to Grid", _snapToGrid);
            _prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", _prefab, typeof(GameObject), false);
            _parentTransform = (Transform)EditorGUILayout.ObjectField("Parent", _parentTransform, typeof(Transform), true);

            EditorGUILayout.Space(8);

            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = _isActive ? Color.red : Color.green;

            if (GUILayout.Button(_isActive ? "Stop Placing" : "Start Placing", GUILayout.Height(30)))
            {
                _isActive = !_isActive;
                _isDragging = false;
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = prevColor;

            if (_isActive)
            {
                EditorGUILayout.HelpBox(
                    "LMB click — place single\nLMB drag — rectangle fill\nShift+LMB — remove\nEsc — stop",
                    MessageType.Info);
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isActive)
                return;

            Event e = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlId);

            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                _isActive = false;
                _isDragging = false;
                e.Use();
                Repaint();
                return;
            }

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!RaycastPlane(ray, out Vector3 hitPoint))
                return;

            Vector3 snapped = _snapToGrid ? SnapToGrid(hitPoint) : hitPoint;

            if (_isDragging)
            {
                _dragCurrent = snapped;
                DrawRectPreview(_dragStart, _dragCurrent);

                if (e.type == EventType.MouseUp && e.button == 0)
                {
                    FillRect(_dragStart, _dragCurrent, e.shift);
                    _isDragging = false;
                    e.Use();
                }
            }
            else
            {
                DrawPreviewCube(snapped);

                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    _dragStart = snapped;
                    _dragCurrent = snapped;
                    _isDragging = true;
                    e.Use();
                }
            }

            sceneView.Repaint();
        }

        private void FillRect(Vector3 start, Vector3 end, bool remove)
        {
            GetGridRange(start, end, out Vector3 min, out Vector3 max);

            int axisA, axisB;
            GetPlaneAxes(out axisA, out axisB);

            Undo.SetCurrentGroupName(remove ? "Remove Cubes" : "Place Cubes");
            int undoGroup = Undo.GetCurrentGroup();

            for (float a = min[axisA]; a <= max[axisA] + 0.01f; a += _cubeSize)
            {
                for (float b = min[axisB]; b <= max[axisB] + 0.01f; b += _cubeSize)
                {
                    Vector3 pos = BuildPosition(a, b, axisA, axisB);

                    if (remove)
                        RemoveCubeAt(pos);
                    else
                        PlaceCubeAt(pos);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        private void GetGridRange(Vector3 start, Vector3 end, out Vector3 min, out Vector3 max)
        {
            min = Vector3.Min(start, end);
            max = Vector3.Max(start, end);
        }

        private void GetPlaneAxes(out int axisA, out int axisB)
        {
            switch (_planeAxis)
            {
                case Axis.XY: axisA = 0; axisB = 1; break;
                case Axis.ZY: axisA = 2; axisB = 1; break;
                default:      axisA = 0; axisB = 2; break;
            }
        }

        private Vector3 BuildPosition(float a, float b, int axisA, int axisB)
        {
            float half = _cubeSize * 0.5f;
            Vector3 pos = Vector3.zero;
            pos[axisA] = a;
            pos[axisB] = b;

            int fixedAxis = _planeAxis switch
            {
                Axis.XZ => 1,
                Axis.XY => 2,
                Axis.ZY => 0,
                _       => 1
            };
            pos[fixedAxis] = _planeOffset + half;

            return pos;
        }

        private void DrawRectPreview(Vector3 start, Vector3 end)
        {
            GetGridRange(start, end, out Vector3 min, out Vector3 max);
            GetPlaneAxes(out int axisA, out int axisB);

            Handles.color = new Color(0f, 1f, 0.5f, 0.15f);
            Color wireColor = new Color(0f, 1f, 0.5f, 0.4f);

            for (float a = min[axisA]; a <= max[axisA] + 0.01f; a += _cubeSize)
            {
                for (float b = min[axisB]; b <= max[axisB] + 0.01f; b += _cubeSize)
                {
                    Vector3 pos = BuildPosition(a, b, axisA, axisB);
                    Handles.CubeHandleCap(0, pos, Quaternion.identity, _cubeSize, EventType.Repaint);
                    Handles.color = wireColor;
                    Handles.DrawWireCube(pos, Vector3.one * _cubeSize);
                    Handles.color = new Color(0f, 1f, 0.5f, 0.15f);
                }
            }
        }

        private bool RaycastPlane(Ray ray, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;

            Vector3 planeNormal = _planeAxis switch
            {
                Axis.XZ => Vector3.up,
                Axis.XY => Vector3.forward,
                Axis.ZY => Vector3.right,
                _       => Vector3.up
            };

            var plane = new Plane(planeNormal, planeNormal * _planeOffset);

            if (!plane.Raycast(ray, out float distance))
                return false;

            hitPoint = ray.GetPoint(distance);
            return true;
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            float half = _cubeSize * 0.5f;
            float x = Mathf.Round((position.x - half) / _cubeSize) * _cubeSize + half;
            float y = Mathf.Round((position.y - half) / _cubeSize) * _cubeSize + half;
            float z = Mathf.Round((position.z - half) / _cubeSize) * _cubeSize + half;

            return _planeAxis switch
            {
                Axis.XZ => new Vector3(x, _planeOffset + half, z),
                Axis.XY => new Vector3(x, y, _planeOffset + half),
                Axis.ZY => new Vector3(_planeOffset + half, y, z),
                _       => new Vector3(x, _planeOffset + half, z)
            };
        }

        private void DrawPreviewCube(Vector3 position)
        {
            Handles.color = new Color(0f, 1f, 0.5f, 0.3f);
            Handles.DrawWireCube(position, Vector3.one * _cubeSize);

            Handles.color = new Color(0f, 1f, 0.5f, 0.1f);
            Handles.CubeHandleCap(0, position, Quaternion.identity, _cubeSize, EventType.Repaint);
        }

        private void PlaceCubeAt(Vector3 position)
        {
            if (HasCubeAt(position))
                return;

            GameObject go;

            if (_prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(_prefab);
                go.transform.position = position;
                go.transform.localScale = Vector3.one * _cubeSize;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Cube";
                go.transform.position = position;
                go.transform.localScale = Vector3.one * _cubeSize;
            }

            if (_parentTransform != null)
                go.transform.SetParent(_parentTransform);

            Undo.RegisterCreatedObjectUndo(go, "Place Cube");
        }

        private void RemoveCubeAt(Vector3 position)
        {
            float threshold = _cubeSize * 0.4f;

            if (_parentTransform != null)
            {
                for (int i = _parentTransform.childCount - 1; i >= 0; i--)
                {
                    Transform child = _parentTransform.GetChild(i);
                    if (Vector3.Distance(child.position, position) < threshold)
                    {
                        Undo.DestroyObjectImmediate(child.gameObject);
                        return;
                    }
                }
            }
            else
            {
                var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                foreach (var r in renderers)
                {
                    if (Vector3.Distance(r.transform.position, position) < threshold)
                    {
                        Undo.DestroyObjectImmediate(r.gameObject);
                        return;
                    }
                }
            }
        }

        private bool HasCubeAt(Vector3 position)
        {
            float threshold = _cubeSize * 0.4f;

            if (_parentTransform != null)
            {
                for (int i = 0; i < _parentTransform.childCount; i++)
                {
                    if (Vector3.Distance(_parentTransform.GetChild(i).position, position) < threshold)
                        return true;
                }

                return false;
            }

            var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                if (Vector3.Distance(r.transform.position, position) < threshold)
                    return true;
            }

            return false;
        }
    }
}
