using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Project.Scripts.Gameplay.Character.Baking
{
    public class CharacterMeshBuilder
    {
        private const string SurfaceProperty = "_Surface";
        private const string BlendProperty = "_Blend";
        private const string CullProperty = "_Cull";
        private const string CutoffProperty = "_Cutoff";
        private const string ZWriteProperty = "_ZWrite";
        private const string BaseMapProperty = "_BaseMap";
        private const string BaseColorProperty = "_BaseColor";
        private const string AlphaClipKeyword = "_ALPHATEST_ON";
        private const float MergedCutoff = 0.5f;
        private const float BoundsPadding = 0.5f;

        private readonly TextureAtlasBuilder _atlas = new();
        private readonly List<MaterialGroup> _groups = new();
        private readonly List<MaterialGroup> _submeshOrder = new();
        private readonly List<Section> _sections = new();
        private readonly List<Material> _rendererMaterials = new();
        private readonly List<Matrix4x4> _toRoot = new();
        private readonly List<Vector3> _sourceVertices = new();
        private readonly List<Vector3> _sourceNormals = new();
        private readonly List<Vector2> _sourceUvs = new();
        private readonly List<int> _sourceIndices = new();
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Vector2> _uvs = new();
        private readonly List<BoneWeight> _boneWeights = new();
        private readonly Dictionary<int, int> _remap = new();
        private bool _missingNormals;

        public BakedCharacterMesh Build(string name, Transform root, List<MeshRenderer> renderers)
        {
            Clear();

            if (!CollectSections(renderers))
            {
                return null;
            }

            Matrix4x4[] bindposes = CreateBindposes(root, renderers);
            Texture2D atlas = _atlas.Build($"{name} Atlas");

            AppendSections();
            OrderSubmeshes();

            Mesh mesh = CreateMesh(name, bindposes);
            Material[] materials = CreateMaterials(name, atlas);

            Bounds bounds = mesh.bounds;
            bounds.Expand(bounds.size.magnitude * BoundsPadding);

            return new BakedCharacterMesh(mesh, materials, atlas, bounds, renderers.Count);
        }

        private void Clear()
        {
            _atlas.Clear();
            _groups.Clear();
            _submeshOrder.Clear();
            _sections.Clear();
            _toRoot.Clear();
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _boneWeights.Clear();
            _missingNormals = false;
        }

        private bool CollectSections(List<MeshRenderer> renderers)
        {
            for (int bone = 0; bone < renderers.Count; bone++)
            {
                MeshRenderer renderer = renderers[bone];

                if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                {
                    Debug.LogError($"[CharacterMeshBaker] '{renderer.name}' has no mesh");
                    return false;
                }

                Mesh mesh = filter.sharedMesh;

                if (!mesh.isReadable)
                {
                    Debug.LogError($"[CharacterMeshBaker] Mesh '{mesh.name}' needs Read/Write enabled");
                    return false;
                }

                renderer.GetSharedMaterials(_rendererMaterials);
                int subMeshCount = Mathf.Min(mesh.subMeshCount, _rendererMaterials.Count);

                for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                {
                    if (!TryAddSection(bone, mesh, subMesh, _rendererMaterials[subMesh]))
                    {
                        return false;
                    }
                }
            }

            return _sections.Count > 0;
        }

        private bool TryAddSection(int bone, Mesh mesh, int subMesh, Material material)
        {
            if (material == null)
            {
                return true;
            }

            if (mesh.GetTopology(subMesh) != MeshTopology.Triangles)
            {
                Debug.LogError($"[CharacterMeshBaker] Mesh '{mesh.name}' submesh {subMesh} is not triangles");
                return false;
            }

            var texture = material.GetTexture(BaseMapProperty) as Texture2D;

            if (texture != null && !texture.isReadable)
            {
                Debug.LogError($"[CharacterMeshBaker] Texture '{texture.name}' needs Read/Write enabled");
                return false;
            }

            AtlasAlphaMode mode = GetAlphaMode(material);
            int group = GetGroup(material, mode);
            int tile = _atlas.AddTile(texture, material.GetColor(BaseColorProperty), mode,
                GetFloat(material, CutoffProperty, MergedCutoff));

            _sections.Add(new Section(bone, mesh, subMesh, group, tile));
            return true;
        }

        private int GetGroup(Material material, AtlasAlphaMode mode)
        {
            bool transparent = mode == AtlasAlphaMode.Transparent;
            float blend = GetFloat(material, BlendProperty, 0f);

            for (int i = 0; i < _groups.Count; i++)
            {
                if (_groups[i].Matches(material.shader, transparent, blend))
                {
                    _groups[i].Add(material, mode);
                    return i;
                }
            }

            var group = new MaterialGroup(material.shader, transparent, blend);
            group.Add(material, mode);
            _groups.Add(group);
            return _groups.Count - 1;
        }

        private Matrix4x4[] CreateBindposes(Transform root, List<MeshRenderer> renderers)
        {
            Matrix4x4 rootWorldToLocal = root.worldToLocalMatrix;
            Matrix4x4 rootLocalToWorld = root.localToWorldMatrix;
            var bindposes = new Matrix4x4[renderers.Count];

            for (int bone = 0; bone < renderers.Count; bone++)
            {
                Transform boneTransform = renderers[bone].transform;
                bindposes[bone] = boneTransform.worldToLocalMatrix * rootLocalToWorld;
                _toRoot.Add(rootWorldToLocal * boneTransform.localToWorldMatrix);
            }

            return bindposes;
        }

        private void AppendSections()
        {
            Mesh loadedMesh = null;

            foreach (Section section in _sections)
            {
                if (section.Mesh != loadedMesh)
                {
                    section.Mesh.GetVertices(_sourceVertices);
                    section.Mesh.GetNormals(_sourceNormals);
                    section.Mesh.GetUVs(0, _sourceUvs);
                    loadedMesh = section.Mesh;
                }

                Matrix4x4 toRoot = _toRoot[section.Bone];
                Matrix4x4 normalMatrix = toRoot.inverse.transpose;
                List<int> indices = _groups[section.Group].Indices;

                section.Mesh.GetIndices(_sourceIndices, section.SubMesh);
                _remap.Clear();

                foreach (int index in _sourceIndices)
                {
                    if (!_remap.TryGetValue(index, out int mapped))
                    {
                        mapped = AppendVertex(section, index, toRoot, normalMatrix);
                        _remap.Add(index, mapped);
                    }

                    indices.Add(mapped);
                }
            }
        }

        private int AppendVertex(Section section, int index, Matrix4x4 toRoot, Matrix4x4 normalMatrix)
        {
            _vertices.Add(toRoot.MultiplyPoint3x4(_sourceVertices[index]));

            if (index < _sourceNormals.Count)
            {
                _normals.Add(normalMatrix.MultiplyVector(_sourceNormals[index]).normalized);
            }
            else
            {
                _normals.Add(Vector3.zero);
                _missingNormals = true;
            }

            Vector2 uv = Vector2.zero;

            if (index < _sourceUvs.Count)
            {
                uv = _sourceUvs[index];
            }

            _uvs.Add(_atlas.RemapUv(section.Tile, uv));
            _boneWeights.Add(new BoneWeight { boneIndex0 = section.Bone, weight0 = 1f });
            return _vertices.Count - 1;
        }

        private void OrderSubmeshes()
        {
            foreach (MaterialGroup group in _groups)
            {
                if (!group.Transparent)
                {
                    _submeshOrder.Add(group);
                }
            }

            foreach (MaterialGroup group in _groups)
            {
                if (group.Transparent)
                {
                    _submeshOrder.Add(group);
                }
            }
        }

        private Mesh CreateMesh(string name, Matrix4x4[] bindposes)
        {
            var mesh = new Mesh { name = $"{name} Baked" };

            if (_vertices.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.boneWeights = _boneWeights.ToArray();
            mesh.bindposes = bindposes;
            mesh.subMeshCount = _submeshOrder.Count;

            for (int i = 0; i < _submeshOrder.Count; i++)
            {
                mesh.SetTriangles(_submeshOrder[i].Indices, i);
            }

            if (_missingNormals)
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        private Material[] CreateMaterials(string name, Texture2D atlas)
        {
            var materials = new Material[_submeshOrder.Count];

            for (int i = 0; i < _submeshOrder.Count; i++)
            {
                MaterialGroup group = _submeshOrder[i];
                var material = new Material(group.Template) { name = $"{name} Baked {i}" };

                material.SetTexture(BaseMapProperty, atlas);
                material.SetColor(BaseColorProperty, Color.white);

                if (group.Transparent)
                {
                    material.SetFloat(ZWriteProperty, 1f);
                }
                else if (group.HasCutout)
                {
                    material.SetFloat(CutoffProperty, MergedCutoff);
                }

                if (group.CullOff)
                {
                    material.SetFloat(CullProperty, (float)CullMode.Off);
                }

                materials[i] = material;
            }

            return materials;
        }

        private static AtlasAlphaMode GetAlphaMode(Material material)
        {
            if (GetFloat(material, SurfaceProperty, 0f) > 0.5f)
            {
                return AtlasAlphaMode.Transparent;
            }

            if (material.IsKeywordEnabled(AlphaClipKeyword))
            {
                return AtlasAlphaMode.Cutout;
            }

            return AtlasAlphaMode.Opaque;
        }

        private static float GetFloat(Material material, string property, float fallback)
        {
            if (material.HasProperty(property))
            {
                return material.GetFloat(property);
            }

            return fallback;
        }

        private readonly struct Section
        {
            public readonly int Bone;
            public readonly Mesh Mesh;
            public readonly int SubMesh;
            public readonly int Group;
            public readonly int Tile;

            public Section(int bone, Mesh mesh, int subMesh, int group, int tile)
            {
                Bone = bone;
                Mesh = mesh;
                SubMesh = subMesh;
                Group = group;
                Tile = tile;
            }
        }

        private class MaterialGroup
        {
            public readonly List<int> Indices = new();
            public readonly bool Transparent;

            private readonly Shader _shader;
            private readonly float _blend;

            public Material Template { get; private set; }
            public bool HasCutout { get; private set; }
            public bool CullOff { get; private set; }

            public MaterialGroup(Shader shader, bool transparent, float blend)
            {
                _shader = shader;
                _blend = blend;
                Transparent = transparent;
            }

            public bool Matches(Shader shader, bool transparent, float blend)
            {
                return _shader == shader && Transparent == transparent && Mathf.Approximately(_blend, blend);
            }

            public void Add(Material material, AtlasAlphaMode mode)
            {
                if (Template == null)
                {
                    Template = material;
                }

                if (mode == AtlasAlphaMode.Cutout && !HasCutout)
                {
                    HasCutout = true;
                    Template = material;
                }

                if (GetFloat(material, CullProperty, (float)CullMode.Back) < 0.5f)
                {
                    CullOff = true;
                }
            }
        }
    }
}
