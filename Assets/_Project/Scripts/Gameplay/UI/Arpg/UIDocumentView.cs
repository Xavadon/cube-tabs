using UnityEngine;
using UnityEngine.UIElements;

namespace _Project.Scripts.Gameplay.UI.Arpg
{
    [RequireComponent(typeof(UIDocument))]
    public abstract class UIDocumentView : MonoBehaviour
    {
        protected VisualElement Root { get; private set; }

        private UIDocument _document;
        private bool _bound;

        protected void StartWiring()
        {
            _document = GetComponent<UIDocument>();
            _bound = true;

            Rewire();
        }

        protected virtual void Update()
        {
            if (!_bound)
                return;

            VisualElement current = _document.rootVisualElement;

            if (current == null || current == Root)
                return;

            Rewire();
        }

        private void Rewire()
        {
            Root = _document.rootVisualElement;
            Wire();
        }

        protected abstract void Wire();
    }
}
