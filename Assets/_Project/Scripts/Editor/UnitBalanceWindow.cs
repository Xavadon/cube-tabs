using System;
using System.Collections.Generic;
using System.Reflection;
using _Project.Scripts.Gameplay.Character.Data;
using _Project.Scripts.Gameplay.Character.Data.AiBrain;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.Editor
{
    public class UnitBalanceWindow : EditorWindow
    {
        private enum SortColumn
        {
            Folder,
            Name,
            Tier,
            HP,
            Damage,
            DamageType,
            PhysRes,
            MagRes,
            FireRes,
            FaithRes,
            Speed,
            Cooldown,
            DPS,
            EvolCost,
            KillReward
        }

        private struct Row
        {
            public CharacterData Data;
            public int TierIndex;
            public string ParentFolder;  // "Enemy Army", "Player Army"
            public string ChildFolder;   // "Dark", "Archer"
            public string Id;
            public float HP;
            public float Damage;
            public string DamageType;
            public float PhysRes;
            public float MagRes;
            public float FireRes;
            public float FaithRes;
            public float Speed;
            public float Cooldown;
            public float DPS;
            public int EvolCost;
            public int KillReward;
        }

        private static readonly ColumnDef[] Columns =
        {
            new("Unit",   SortColumn.Name,       130, false),
            new("T",      SortColumn.Tier,        28, false),
            new("HP",     SortColumn.HP,          60, true),
            new("Dmg",    SortColumn.Damage,      55, true),
            new("Type",   SortColumn.DamageType,  65, false),
            new("Phys",   SortColumn.PhysRes,     42, true),
            new("Mag",    SortColumn.MagRes,      42, true),
            new("Fire",   SortColumn.FireRes,     42, true),
            new("Faith",  SortColumn.FaithRes,    42, true),
            new("Spd",    SortColumn.Speed,       45, true),
            new("CD",     SortColumn.Cooldown,    45, false),
            new("DPS",    SortColumn.DPS,         55, false),
            new("Evol$",  SortColumn.EvolCost,    55, true),
            new("Kill$",  SortColumn.KillReward,  50, true),
        };

        private const float RowHeight = 20f;
        private const float Padding = 4f;

        private readonly List<Row> _rows = new();
        private Vector2 _scroll;
        private SortColumn _sortColumn = SortColumn.Folder;
        private bool _sortAscending = true;
        private string _searchFilter = "";

        [MenuItem("Tools/Unit Balance Table")]
        public static void Open()
        {
            GetWindow<UnitBalanceWindow>("Unit Balance");
        }

        private void OnEnable() => Rebuild();
        private void OnFocus() => Rebuild();

        private void Rebuild()
        {
            _rows.Clear();

            string[] guids = AssetDatabase.FindAssets("t:CharacterData");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);

                if (data == null || data.Tiers == null)
                    continue;

                var (parentFolder, childFolder) = ExtractFolderHierarchy(path);

                for (int t = 0; t < data.Tiers.Length; t++)
                {
                    var tier = data.Tiers[t];
                    if (tier?.Stats == null)
                        continue;

                    float cooldown = GetAttackCooldown(tier.BrainData);
                    float dps = cooldown > 0f ? tier.Stats.Damage / cooldown : 0f;

                    _rows.Add(new Row
                    {
                        Data = data,
                        TierIndex = t,
                        ParentFolder = parentFolder,
                        ChildFolder = childFolder,
                        Id = data.Id,
                        HP = tier.Stats.Health,
                        Damage = tier.Stats.Damage,
                        DamageType = tier.Stats.DamageType.ToString(),
                        PhysRes = tier.Stats.PhysicalResist,
                        MagRes = tier.Stats.MagicResist,
                        FireRes = tier.Stats.FireResist,
                        FaithRes = tier.Stats.FaithResist,
                        Speed = tier.MoveSpeed,
                        Cooldown = cooldown,
                        DPS = dps,
                        EvolCost = tier.EvolutionCost,
                        KillReward = tier.KillReward
                    });
                }
            }

            SortRows();
            Repaint();
        }

        private static float GetAttackCooldown(BrainDataBase brain)
        {
            if (brain == null)
                return 0f;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo prop = brain.GetType().GetProperty("AttackCooldown", flags);

            if (prop != null && prop.PropertyType == typeof(float))
                return (float)prop.GetValue(brain);

            return 0f;
        }

        private static (string parent, string child) ExtractFolderHierarchy(string assetPath)
        {
            // Expected: .../Characters/ParentFolder/ChildFolder/asset.asset
            // or: .../Characters/ParentFolder/asset.asset
            const string marker = "/Characters/";
            int markerIdx = assetPath.IndexOf(marker, StringComparison.Ordinal);
            if (markerIdx < 0)
                return ("", "");

            string relativePath = assetPath.Substring(markerIdx + marker.Length);
            string[] parts = relativePath.Split('/');

            // parts[0] = ParentFolder, parts[1] = ChildFolder or asset.asset
            string parent = parts.Length > 0 ? parts[0] : "";
            string child = parts.Length > 2 ? parts[1] : ""; // only if there's a subfolder

            return (parent, child);
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawHeader();
            DrawTable();
        }

        private void DrawToolbar()
        {
            Rect toolbar = EditorGUILayout.GetControlRect(false, EditorStyles.toolbar.fixedHeight);
            GUI.Box(toolbar, GUIContent.none, EditorStyles.toolbar);

            var refreshRect = new Rect(toolbar.x + 2, toolbar.y, 60, toolbar.height);
            if (GUI.Button(refreshRect, "Refresh", EditorStyles.toolbarButton))
                Rebuild();

            var searchLabelRect = new Rect(refreshRect.xMax + 8, toolbar.y, 45, toolbar.height);
            GUI.Label(searchLabelRect, "Search:");

            var searchRect = new Rect(searchLabelRect.xMax + 2, toolbar.y + 2, 200, toolbar.height - 4);
            _searchFilter = EditorGUI.TextField(searchRect, _searchFilter, EditorStyles.toolbarSearchField);
        }

        private void DrawHeader()
        {
            Rect header = EditorGUILayout.GetControlRect(false, EditorStyles.toolbar.fixedHeight);
            GUI.Box(header, GUIContent.none, EditorStyles.toolbar);

            float x = header.x + Padding;

            foreach (var col in Columns)
            {
                var rect = new Rect(x, header.y, col.Width, header.height);

                string label = _sortColumn == col.Sort
                    ? col.Label + (_sortAscending ? " \u25B2" : " \u25BC")
                    : col.Label;

                if (GUI.Button(rect, label, EditorStyles.toolbarButton))
                {
                    if (_sortColumn == col.Sort)
                        _sortAscending = !_sortAscending;
                    else
                    {
                        _sortColumn = col.Sort;
                        _sortAscending = true;
                    }

                    SortRows();
                }

                x += col.Width;
            }
        }

        private void DrawTable()
        {
            bool hasFilter = !string.IsNullOrEmpty(_searchFilter);
            int visibleCount = 0;
            string prevParent = null;
            string prevChild = null;

            // Count visible rows + headers
            for (int i = 0; i < _rows.Count; i++)
            {
                if (hasFilter && _rows[i].Id.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (_rows[i].ParentFolder != prevParent)
                    visibleCount++; // parent header

                if (_rows[i].ChildFolder != prevChild && !string.IsNullOrEmpty(_rows[i].ChildFolder))
                    visibleCount++; // child header

                visibleCount++;
                prevParent = _rows[i].ParentFolder;
                prevChild = _rows[i].ChildFolder;
            }

            float totalHeight = visibleCount * RowHeight;
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            Rect viewRect = GUILayoutUtility.GetRect(position.width - 20, totalHeight);
            bool needsRebuild = false;
            int drawIndex = 0;
            prevParent = null;
            prevChild = null;

            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];

                if (hasFilter && row.Id.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                // Draw parent folder header
                if (row.ParentFolder != prevParent)
                {
                    var headerRect = new Rect(viewRect.x, viewRect.y + drawIndex * RowHeight, viewRect.width, RowHeight);
                    EditorGUI.LabelField(headerRect, row.ParentFolder, Styles.ParentHeader);
                    drawIndex++;
                    prevChild = null; // reset child when parent changes
                }

                // Draw child folder header
                if (row.ChildFolder != prevChild && !string.IsNullOrEmpty(row.ChildFolder))
                {
                    var headerRect = new Rect(viewRect.x + 20, viewRect.y + drawIndex * RowHeight, viewRect.width - 20, RowHeight);
                    EditorGUI.LabelField(headerRect, row.ChildFolder, Styles.ChildHeader);
                    drawIndex++;
                }

                prevParent = row.ParentFolder;
                prevChild = row.ChildFolder;

                var rowRect = new Rect(viewRect.x, viewRect.y + drawIndex * RowHeight, viewRect.width, RowHeight);

                if (Event.current.type == EventType.Repaint)
                {
                    GUIStyle bg = drawIndex % 2 == 0 ? Styles.EvenRow : Styles.OddRow;
                    bg.Draw(rowRect, false, false, false, false);
                }

                float x = rowRect.x + Padding;
                int colIdx = 0;

                // Unit name (clickable)
                var nameRect = CellRect(ref x, colIdx++, rowRect.y);
                if (GUI.Button(nameRect, row.Id, Styles.Link))
                {
                    EditorGUIUtility.PingObject(row.Data);
                    Selection.activeObject = row.Data;
                }

                // Tier (readonly)
                var tierRect = CellRect(ref x, colIdx++, rowRect.y);
                EditorGUI.LabelField(tierRect, (row.TierIndex + 1).ToString(), Styles.CenterLabel);

                // Editable fields via SerializedProperty
                var so = new SerializedObject(row.Data);
                SerializedProperty tierProp = so.FindProperty("<Tiers>k__BackingField")
                    .GetArrayElementAtIndex(row.TierIndex);
                SerializedProperty statsProp = tierProp.FindPropertyRelative("<Stats>k__BackingField");

                EditorGUI.BeginChangeCheck();

                // HP
                DrawCellProperty(ref x, ref colIdx, rowRect.y, statsProp, "<Health>k__BackingField");
                // Damage
                DrawCellProperty(ref x, ref colIdx, rowRect.y, statsProp, "<Damage>k__BackingField");

                // DamageType (readonly)
                var typeRect = CellRect(ref x, colIdx++, rowRect.y);
                EditorGUI.LabelField(typeRect, row.DamageType, Styles.CenterLabel);

                // Resists
                DrawCellProperty(ref x, ref colIdx, rowRect.y, statsProp, "<PhysicalResist>k__BackingField");
                DrawCellProperty(ref x, ref colIdx, rowRect.y, statsProp, "<MagicResist>k__BackingField");
                DrawCellProperty(ref x, ref colIdx, rowRect.y, statsProp, "<FireResist>k__BackingField");
                DrawCellProperty(ref x, ref colIdx, rowRect.y, statsProp, "<FaithResist>k__BackingField");

                // Speed
                DrawCellProperty(ref x, ref colIdx, rowRect.y, tierProp, "<MoveSpeed>k__BackingField");

                // Cooldown (readonly)
                var cdRect = CellRect(ref x, colIdx++, rowRect.y);
                EditorGUI.LabelField(cdRect, row.Cooldown > 0 ? row.Cooldown.ToString("0.##") : "-", Styles.CenterLabel);

                // DPS (computed, readonly)
                var dpsRect = CellRect(ref x, colIdx++, rowRect.y);
                EditorGUI.LabelField(dpsRect, row.DPS > 0 ? row.DPS.ToString("0.#") : "-", Styles.BoldCenter);

                // EvolCost
                DrawCellProperty(ref x, ref colIdx, rowRect.y, tierProp, "<EvolutionCost>k__BackingField");
                // KillReward
                DrawCellProperty(ref x, ref colIdx, rowRect.y, tierProp, "<KillReward>k__BackingField");

                if (EditorGUI.EndChangeCheck())
                {
                    so.ApplyModifiedProperties();
                    needsRebuild = true;
                }

                drawIndex++;
            }

            EditorGUILayout.EndScrollView();

            if (needsRebuild)
                EditorApplication.delayCall += Rebuild;
        }

        private static Rect CellRect(ref float x, int colIdx, float y)
        {
            float w = Columns[colIdx].Width;
            var rect = new Rect(x + 2, y + 1, w - 4, RowHeight - 2);
            x += w;
            return rect;
        }

        private static void DrawCellProperty(ref float x, ref int colIdx, float y,
            SerializedProperty parent, string relativePath)
        {
            var rect = CellRect(ref x, colIdx++, y);
            SerializedProperty prop = parent.FindPropertyRelative(relativePath);

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:
                    prop.floatValue = EditorGUI.FloatField(rect, prop.floatValue);
                    break;
                case SerializedPropertyType.Integer:
                    prop.intValue = EditorGUI.IntField(rect, prop.intValue);
                    break;
            }
        }

        private void SortRows()
        {
            _rows.Sort((a, b) =>
            {
                // 1. Parent folder (Enemy Army, Player Army, etc.)
                int cmp = string.Compare(a.ParentFolder, b.ParentFolder, StringComparison.Ordinal);
                if (cmp != 0) return cmp;

                // 2. Child folder (Dark, Archer, etc.)
                cmp = string.Compare(a.ChildFolder, b.ChildFolder, StringComparison.Ordinal);
                if (cmp != 0) return cmp;

                // 3. Unit name
                cmp = string.Compare(a.Id, b.Id, StringComparison.Ordinal);
                if (cmp != 0) return cmp;

                // 4. Tier 1→2→3
                return a.TierIndex.CompareTo(b.TierIndex);
            });
        }

        private readonly struct ColumnDef
        {
            public readonly string Label;
            public readonly SortColumn Sort;
            public readonly float Width;
            public readonly bool Editable;

            public ColumnDef(string label, SortColumn sort, float width, bool editable)
            {
                Label = label;
                Sort = sort;
                Width = width;
                Editable = editable;
            }
        }

        private static class Styles
        {
            private static GUIStyle _evenRow;
            private static GUIStyle _oddRow;
            private static GUIStyle _link;
            private static GUIStyle _centerLabel;
            private static GUIStyle _boldCenter;
            private static GUIStyle _parentHeader;
            private static GUIStyle _childHeader;

            public static GUIStyle EvenRow =>
                _evenRow ??= new GUIStyle("CN EntryBackEven");

            public static GUIStyle OddRow =>
                _oddRow ??= new GUIStyle("CN EntryBackOdd");

            public static GUIStyle Link
            {
                get
                {
                    if (_link == null)
                    {
                        _link = new GUIStyle(EditorStyles.label)
                        {
                            normal = { textColor = new Color(0.4f, 0.8f, 1f) },
                            hover = { textColor = new Color(0.6f, 0.9f, 1f) },
                            alignment = TextAnchor.MiddleLeft
                        };
                    }

                    return _link;
                }
            }

            public static GUIStyle CenterLabel
            {
                get
                {
                    if (_centerLabel == null)
                    {
                        _centerLabel = new GUIStyle(EditorStyles.label)
                        {
                            alignment = TextAnchor.MiddleCenter
                        };
                    }

                    return _centerLabel;
                }
            }

            public static GUIStyle BoldCenter
            {
                get
                {
                    if (_boldCenter == null)
                    {
                        _boldCenter = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleCenter
                        };
                    }

                    return _boldCenter;
                }
            }

            public static GUIStyle ParentHeader
            {
                get
                {
                    if (_parentHeader == null)
                    {
                        _parentHeader = new GUIStyle(EditorStyles.boldLabel)
                        {
                            fontSize = 14,
                            alignment = TextAnchor.MiddleLeft,
                            normal = { textColor = new Color(1f, 0.8f, 0.2f) }
                        };
                    }

                    return _parentHeader;
                }
            }

            public static GUIStyle ChildHeader
            {
                get
                {
                    if (_childHeader == null)
                    {
                        _childHeader = new GUIStyle(EditorStyles.boldLabel)
                        {
                            fontSize = 12,
                            alignment = TextAnchor.MiddleLeft,
                            normal = { textColor = new Color(0.7f, 0.9f, 1f) }
                        };
                    }

                    return _childHeader;
                }
            }
        }
    }
}
