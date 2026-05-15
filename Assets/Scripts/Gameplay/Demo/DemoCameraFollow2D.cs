using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoCameraFollow2D : MonoBehaviour
    {
        [SerializeField] private string targetName = "PersonajeFinal";
        [SerializeField] private Vector3 offset = new(0f, 1.25f, -10f);
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private bool followY = true;
        [SerializeField] private float lookDownDistance = 2.5f;
        [SerializeField] private float lookDownThreshold = -0.35f;

        [Header("Boundaries")]
        [Tooltip("Prevent the camera from scrolling left of its scene-placed starting position. " +
                 "Eliminates empty-space reveal at the beginning of each level.")]
        [SerializeField] private bool clampLeft = true;

        private Transform _target;
        private Vector3 _velocity;
        private float _minX;
        private float _baseY;
        private bool _snappedOnFirstFrame;

        private void Awake()
        {
            
            _minX = transform.position.x;
            _baseY = transform.position.y;
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                GameObject targetObject =
                    GameObject.Find(targetName) ?? GameObject.Find("Character");

                if (targetObject == null)
                {
                    return;
                }

                _target = targetObject.transform;
            }

            Vector3 desiredPosition = _target.position + offset;
            float lookDownOffset = GetLookDownOffset();

            if (!followY)
            {
                desiredPosition.y = _baseY + lookDownOffset;
            }
            else
            {
                desiredPosition.y += lookDownOffset;
            }

            if (clampLeft)
            {
                desiredPosition.x = Mathf.Max(_minX, desiredPosition.x);
            }

            if (!_snappedOnFirstFrame)
            {
                _snappedOnFirstFrame = true;
                transform.position = desiredPosition;
                _velocity = Vector3.zero;
                return;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref _velocity, smoothTime);
        }

        private float GetLookDownOffset()
        {
            float verticalInput = MobileInputState.Vertical;

            if (Mathf.Abs(verticalInput) < 0.01f && UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.sKey.isPressed ||
                    UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed)
                {
                    verticalInput = -1f;
                }
            }

            return verticalInput <= lookDownThreshold ? -lookDownDistance : 0f;
        }
    }
}
