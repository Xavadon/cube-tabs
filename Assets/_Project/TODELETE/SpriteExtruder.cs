using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SpriteExtruder : MonoBehaviour
{
    [Tooltip("Спрайт-источник. В Import Settings: Mesh Type = Full Rect или Tight, Read/Write = On, Generate Physics Shape = On")]
    public Sprite sourceSprite;

    [Tooltip("Толщина объёма по оси Z")]
    public float thickness = 0.2f;

    [Tooltip("Материал для передней и задней поверхности")]
    public Material frontMaterial;

    [Tooltip("Материал для боковых граней (если null — используется frontMaterial)")]
    public Material sideMaterial;
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpriteExtruder))]
public class SpriteExtruderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space(8);

        SpriteExtruder extruder = (SpriteExtruder)target;

        if (extruder.sourceSprite != null)
        {
            int shapeCount = extruder.sourceSprite.GetPhysicsShapeCount();
            EditorGUILayout.HelpBox(
                "Physics shapes найдено: " + shapeCount +
                (shapeCount == 0 ? "\nВключи Generate Physics Shape в Import Settings спрайта!" : " ✓"),
                shapeCount == 0 ? MessageType.Warning : MessageType.Info
            );
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Generate 3D Mesh", GUILayout.Height(40)))
        {
            GenerateMesh(extruder);
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  MAIN
    // ─────────────────────────────────────────────────────────────
    private void GenerateMesh(SpriteExtruder extruder)
    {
        if (extruder.sourceSprite == null)
        {
            Debug.LogError("[SpriteExtruder] Спрайт не назначен.");
            return;
        }

        int shapeCount = extruder.sourceSprite.GetPhysicsShapeCount();
        if (shapeCount == 0)
        {
            Debug.LogError("[SpriteExtruder] Physics Shape не найден. Включи Generate Physics Shape в Import Settings.");
            return;
        }

        float halfZ = extruder.thickness * 0.5f;

        List<Vector3> allVerts = new List<Vector3>();
        List<int> frontBackTris = new List<int>();
        List<int> sideTris = new List<int>();
        List<Vector2> allUVs = new List<Vector2>();

        // Bounding box спрайта для нормализации UV
        Bounds spriteBounds = extruder.sourceSprite.bounds;
        float boundsW = spriteBounds.size.x;
        float boundsH = spriteBounds.size.y;
        Vector2 boundsMin = new Vector2(spriteBounds.min.x, spriteBounds.min.y);

        for (int s = 0; s < shapeCount; s++)
        {
            List<Vector2> shape = new List<Vector2>();
            extruder.sourceSprite.GetPhysicsShape(s, shape);

            if (shape.Count < 3)
            {
                continue;
            }

            // Гарантируем CCW
            if (!IsCounterClockwise(shape))
            {
                shape.Reverse();
            }

            // Триангулируем
            List<int> polyTris = EarClip(shape);
            if (polyTris.Count == 0)
            {
                Debug.LogWarning("[SpriteExtruder] Shape " + s + ": триангуляция не удалась, пропускаем.");
                continue;
            }

            int n = shape.Count;
            int baseOffset = allVerts.Count;

            // ── FRONT (z = -halfZ, смотрит вперёд) ──
            int frontBase = baseOffset;
            for (int i = 0; i < n; i++)
            {
                allVerts.Add(new Vector3(shape[i].x, shape[i].y, -halfZ));
                allUVs.Add(WorldToUV(shape[i], boundsMin, boundsW, boundsH));
            }
            for (int i = 0; i < polyTris.Count; i += 3)
            {
                frontBackTris.Add(frontBase + polyTris[i]);
                frontBackTris.Add(frontBase + polyTris[i + 1]);
                frontBackTris.Add(frontBase + polyTris[i + 2]);
            }

            // ── BACK (z = +halfZ, нормали обратные) ──
            int backBase = allVerts.Count;
            for (int i = 0; i < n; i++)
            {
                allVerts.Add(new Vector3(shape[i].x, shape[i].y, halfZ));
                allUVs.Add(WorldToUV(shape[i], boundsMin, boundsW, boundsH));
            }
            for (int i = 0; i < polyTris.Count; i += 3)
            {
                frontBackTris.Add(backBase + polyTris[i + 2]);
                frontBackTris.Add(backBase + polyTris[i + 1]);
                frontBackTris.Add(backBase + polyTris[i + 0]);
            }

            // ── SIDES ──
            float totalLen = 0f;
            float[] segLengths = new float[n];
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                segLengths[i] = Vector2.Distance(shape[i], shape[next]);
                totalLen += segLengths[i];
            }

            float uCur = 0f;
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                float uNext = uCur + (totalLen > 0f ? segLengths[i] / totalLen : 0f);

                int sideBase = allVerts.Count;

                allVerts.Add(new Vector3(shape[i].x,    shape[i].y,    -halfZ)); allUVs.Add(new Vector2(uCur,  1f));
                allVerts.Add(new Vector3(shape[next].x, shape[next].y, -halfZ)); allUVs.Add(new Vector2(uNext, 1f));
                allVerts.Add(new Vector3(shape[next].x, shape[next].y,  halfZ)); allUVs.Add(new Vector2(uNext, 0f));
                allVerts.Add(new Vector3(shape[i].x,    shape[i].y,     halfZ)); allUVs.Add(new Vector2(uCur,  0f));

                sideTris.Add(sideBase + 0);
                sideTris.Add(sideBase + 1);
                sideTris.Add(sideBase + 2);

                sideTris.Add(sideBase + 0);
                sideTris.Add(sideBase + 2);
                sideTris.Add(sideBase + 3);

                uCur = uNext;
            }
        }

        if (allVerts.Count == 0)
        {
            Debug.LogError("[SpriteExtruder] Меш пустой — нет валидных шейпов.");
            return;
        }

        Mesh mesh = new Mesh();
        mesh.name = "SpriteExtrudedMesh";

        if (allVerts.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(allVerts);
        mesh.SetUVs(0, allUVs);
        mesh.subMeshCount = 2;
        mesh.SetTriangles(frontBackTris, 0);
        mesh.SetTriangles(sideTris, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter mf = extruder.GetComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = extruder.GetComponent<MeshRenderer>();
        Material side = extruder.sideMaterial != null ? extruder.sideMaterial : extruder.frontMaterial;
        mr.sharedMaterials = new Material[] { extruder.frontMaterial, side };

        EditorUtility.SetDirty(extruder.gameObject);
        Debug.Log("[SpriteExtruder] Готово. Вершин: " + allVerts.Count +
                  ", front/back тр: " + (frontBackTris.Count / 3) +
                  ", side тр: " + (sideTris.Count / 3));
    }

    // ─────────────────────────────────────────────────────────────
    //  UV HELPERS
    // ─────────────────────────────────────────────────────────────
    private Vector2 WorldToUV(Vector2 worldPos, Vector2 boundsMin, float boundsW, float boundsH)
    {
        float u = boundsW > 0f ? (worldPos.x - boundsMin.x) / boundsW : 0f;
        float v = boundsH > 0f ? (worldPos.y - boundsMin.y) / boundsH : 0f;
        return new Vector2(u, v);
    }

    // ─────────────────────────────────────────────────────────────
    //  EAR CLIPPING (robust, handles concave polygons)
    // ─────────────────────────────────────────────────────────────
    private List<int> EarClip(List<Vector2> polygon)
    {
        List<int> result = new List<int>();
        List<int> idx = new List<int>();
        for (int i = 0; i < polygon.Count; i++) { idx.Add(i); }

        int limit = polygon.Count * polygon.Count * 2 + 100;
        int iter = 0;

        while (idx.Count > 3 && iter++ < limit)
        {
            bool found = false;
            for (int i = 0; i < idx.Count; i++)
            {
                int pi = (i - 1 + idx.Count) % idx.Count;
                int ni = (i + 1) % idx.Count;

                Vector2 A = polygon[idx[pi]];
                Vector2 B = polygon[idx[i]];
                Vector2 C = polygon[idx[ni]];

                if (Cross(A, B, C) <= 0f) { continue; } // не выпуклое ухо

                bool blocked = false;
                for (int j = 0; j < idx.Count; j++)
                {
                    if (j == pi || j == i || j == ni) { continue; }
                    if (PointInTriangle(polygon[idx[j]], A, B, C))
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    result.Add(idx[pi]);
                    result.Add(idx[i]);
                    result.Add(idx[ni]);
                    idx.RemoveAt(i);
                    found = true;
                    break;
                }
            }

            if (!found) { break; } // вырожденный полигон — прерываем
        }

        if (idx.Count == 3)
        {
            result.Add(idx[0]);
            result.Add(idx[1]);
            result.Add(idx[2]);
        }

        return result;
    }

    private float Cross(Vector2 o, Vector2 a, Vector2 b)
    {
        return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
    }

    private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(a, b, p);
        float d2 = Cross(b, c, p);
        float d3 = Cross(c, a, p);
        bool hasNeg = (d1 < 0f) || (d2 < 0f) || (d3 < 0f);
        bool hasPos = (d1 > 0f) || (d2 > 0f) || (d3 > 0f);
        return !(hasNeg && hasPos);
    }

    private bool IsCounterClockwise(List<Vector2> poly)
    {
        float sum = 0f;
        for (int i = 0; i < poly.Count; i++)
        {
            Vector2 cur = poly[i];
            Vector2 nxt = poly[(i + 1) % poly.Count];
            sum += (nxt.x - cur.x) * (nxt.y + cur.y);
        }
        return sum < 0f;
    }
}
#endif