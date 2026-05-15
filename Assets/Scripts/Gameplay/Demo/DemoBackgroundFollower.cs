using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    [RequireComponent(typeof(Renderer))]
    public sealed class DemoBackgroundFollower : MonoBehaviour
    {
        [SerializeField] private bool followCameraX = true;
        [SerializeField] private bool followCameraY = true;

        [SerializeField] private float parallaxX = 1f;
        [SerializeField] private float parallaxY = 1f;

        [SerializeField] private float repeatScaleX = 8f;
        [SerializeField] private float repeatScaleY = 1f;

        private Camera _mainCamera;
        private Renderer _renderer;
        private Vector3 _initialPosition;
        private Vector3 _initialCameraPosition;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _initialPosition = transform.position;
            _mainCamera = Camera.main;

            if (_mainCamera != null)
            {
                _initialCameraPosition = _mainCamera.transform.position;
            }

            ApplyTiling();
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;

                if (_mainCamera == null)
                {
                    return;
                }

                _initialCameraPosition = _mainCamera.transform.position;
            }

            Vector3 camDelta = _mainCamera.transform.position - _initialCameraPosition;

            Vector3 nextPosition = _initialPosition;

            if (followCameraX)
            {
                nextPosition.x += camDelta.x * parallaxX;
            }

            if (followCameraY)
            {
                
                nextPosition.y += camDelta.y * parallaxY;
            }

            nextPosition.z = transform.position.z;
            transform.position = nextPosition;
        }

        private void ApplyTiling()
        {
            if (_renderer == null || _renderer.sharedMaterial == null)
            {
                return;
            }

            Material materialInstance = _renderer.material;
            Vector2 scale = new(repeatScaleX, repeatScaleY);

            if (materialInstance.HasProperty("_BaseMap"))
            {
                materialInstance.SetTextureScale("_BaseMap", scale);
                return;
            }

            materialInstance.mainTextureScale = scale;
        }
    }
}
