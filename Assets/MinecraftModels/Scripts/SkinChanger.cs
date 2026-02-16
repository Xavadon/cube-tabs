using NaughtyAttributes;
using UnityEngine;

namespace MinecraftModels.Scripts
{
    public class SkinChanger : MonoBehaviour
    {
        [SerializeField]
        private MeshRenderer[] _meshRenderers;
        
        [SerializeField]
        private Material _material;

        public void ChangeSkin(Material material)
        {
            foreach (var renderer in _meshRenderers)
            {
                renderer.material = _material;
            }
        }
        
        [Button]
        private void ChangeSkin()
        {
            ChangeSkin(_material);
        }
    }
}
