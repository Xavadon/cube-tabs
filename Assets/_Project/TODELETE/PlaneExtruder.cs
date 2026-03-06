using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PlaneExtruder : MonoBehaviour
{
    [Tooltip("Толщина экструдирования по оси Y (вверх/вниз от плоскости Plane)")]
    public float thickness = 0.1f;

    [Tooltip("Исходный Plane (опционально — если не задан, берётся текущий объект)")]
    public MeshFilter sourceMeshFilter;

    [Tooltip("Готовый материал для экструдированного меша")]
    public Material material;
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlaneExtruder))]
public class PlaneExtruderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlaneExtruder extruder = (PlaneExtruder)target;

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Generate Mesh", GUILayout.Height(36)))
        {
            GenerateExtrudedMesh(extruder);
        }
    }

    private void GenerateExtrudedMesh(PlaneExtruder extruder)
    {
        MeshFilter targetFilter = extruder.GetComponent<MeshFilter>();
        MeshRenderer targetRenderer = extruder.GetComponent<MeshRenderer>();

        MeshFilter sourceFilter = extruder.sourceMeshFilter;
        if (sourceFilter == null)
        {
            sourceFilter = extruder.GetComponent<MeshFilter>();
        }

        Mesh sourceMesh = sourceFilter.sharedMesh;
        if (sourceMesh == null)
        {
            Debug.LogError("[PlaneExtruder] Исходный меш не найден.");
            return;
        }

        Vector3[] sourceVerts = sourceMesh.vertices;
        int[] sourceTris = sourceMesh.triangles;
        Vector2[] sourceUVs = sourceMesh.uv;

        int vertCount = sourceVerts.Length;
        float halfThickness = extruder.thickness * 0.5f;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // --- Верхняя поверхность (смещение +Y) ---
        for (int i = 0; i < vertCount; i++)
        {
            Vector3 v = sourceVerts[i];
            vertices.Add(new Vector3(v.x, v.y + halfThickness, v.z));
            uvs.Add(sourceUVs.Length > i ? sourceUVs[i] : Vector2.zero);
        }

        // Верхние треугольники (оригинальный порядок)
        for (int i = 0; i < sourceTris.Length; i++)
        {
            triangles.Add(sourceTris[i]);
        }

        // --- Нижняя поверхность (смещение -Y) ---
        int backOffset = vertCount;
        for (int i = 0; i < vertCount; i++)
        {
            Vector3 v = sourceVerts[i];
            vertices.Add(new Vector3(v.x, v.y - halfThickness, v.z));
            uvs.Add(sourceUVs.Length > i ? sourceUVs[i] : Vector2.zero);
        }

        // Нижние треугольники (обратный порядок нормалей)
        for (int i = 0; i < sourceTris.Length; i += 3)
        {
            triangles.Add(backOffset + sourceTris[i + 2]);
            triangles.Add(backOffset + sourceTris[i + 1]);
            triangles.Add(backOffset + sourceTris[i + 0]);
        }

        // --- Боковые поверхности (side faces) ---
        // Находим граничные рёбра (edge встречается только один раз в sourceTris)
        Dictionary<long, int[]> edgeMap = new Dictionary<long, int[]>();

        for (int i = 0; i < sourceTris.Length; i += 3)
        {
            int a = sourceTris[i];
            int b = sourceTris[i + 1];
            int c = sourceTris[i + 2];

            AddEdge(edgeMap, a, b);
            AddEdge(edgeMap, b, c);
            AddEdge(edgeMap, c, a);
        }

        // Для каждого граничного ребра строим боковой квад
        foreach (KeyValuePair<long, int[]> pair in edgeMap)
        {
            int[] edge = pair.Value;
            if (edge[2] != 1)
            {
                continue; // не граничное ребро
            }

            int v0Top = edge[0];
            int v1Top = edge[1];
            int v0Bot = backOffset + v0Top;
            int v1Bot = backOffset + v1Top;

            int sideBase = vertices.Count;

            // Верхняя вершина 0
            vertices.Add(vertices[v0Top]);
            uvs.Add(new Vector2(0f, 1f));

            // Верхняя вершина 1
            vertices.Add(vertices[v1Top]);
            uvs.Add(new Vector2(1f, 1f));

            // Нижняя вершина 1
            vertices.Add(vertices[v1Bot]);
            uvs.Add(new Vector2(1f, 0f));

            // Нижняя вершина 0
            vertices.Add(vertices[v0Bot]);
            uvs.Add(new Vector2(0f, 0f));

            // Квад из двух треугольников
            triangles.Add(sideBase + 0);
            triangles.Add(sideBase + 2);
            triangles.Add(sideBase + 1);

            triangles.Add(sideBase + 0);
            triangles.Add(sideBase + 3);
            triangles.Add(sideBase + 2);
        }

        // --- Собираем меш ---
        Mesh newMesh = new Mesh();
        newMesh.name = "ExtrudedMesh";

        if (vertices.Count > 65535)
        {
            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        newMesh.SetVertices(vertices);
        newMesh.SetTriangles(triangles, 0);
        newMesh.SetUVs(0, uvs);
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();

        targetFilter.sharedMesh = newMesh;

        // --- Материал ---
        if (extruder.material != null)
        {
            targetRenderer.sharedMaterial = extruder.material;
        }
        else
        {
            Debug.LogWarning("[PlaneExtruder] Материал не назначен — назначьте его в поле Material.");
        }

        EditorUtility.SetDirty(extruder.gameObject);
        Debug.Log("[PlaneExtruder] Меш успешно сгенерирован. Вершин: " + vertices.Count + ", Треугольников: " + (triangles.Count / 3));
    }

    private void AddEdge(Dictionary<long, int[]> edgeMap, int a, int b)
    {
        // Ключ — упорядоченная пара (min, max) для уникальности ребра
        long key = ((long)Mathf.Min(a, b) << 32) | (long)Mathf.Max(a, b);

        if (edgeMap.ContainsKey(key))
        {
            edgeMap[key][2]++;
        }
        else
        {
            // Храним: [v0, v1, count]
            edgeMap[key] = new int[] { a, b, 1 };
        }
    }
}
#endif