using UnityEngine;

namespace _Project.Scripts.Gameplay.Character.Baking
{
    public class BakedCharacterMesh
    {
        private readonly Texture2D _atlas;

        public Mesh Mesh { get; }
        public Material[] Materials { get; }
        public Bounds Bounds { get; }
        public int BoneCount { get; }

        public BakedCharacterMesh(Mesh mesh, Material[] materials, Texture2D atlas, Bounds bounds, int boneCount)
        {
            Mesh = mesh;
            Materials = materials;
            Bounds = bounds;
            BoneCount = boneCount;
            _atlas = atlas;
        }

        public void Release()
        {
            Object.Destroy(Mesh);
            Object.Destroy(_atlas);

            foreach (Material material in Materials)
            {
                Object.Destroy(material);
            }
        }
    }
}
