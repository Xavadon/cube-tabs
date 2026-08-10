using UnityEngine;
using CharacterEntity = _Project.Scripts.Gameplay.Character.Character;

namespace _Project.Scripts.Gameplay.Services.Scene
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class Portal : MonoBehaviour
    {
        [SerializeField] private bool _openOnStart = true;

        private bool _used;

        public bool IsOpen { get; private set; }

        protected virtual bool OneShot => true;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;

            // игрок ходит навмешем, без Rigidbody триггер между двумя статиками не сработает
            var body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            SetOpen(_openOnStart);
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
                renderer.enabled = open;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_used || !IsOpen)
                return;

            CharacterEntity player = other.GetComponentInParent<CharacterEntity>();

            if (player == null)
                return;

            _used = OneShot;
            Use(player);
        }

        protected abstract void Use(CharacterEntity player);
    }
}
