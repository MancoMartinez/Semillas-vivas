using UnityEngine;
using UnityEngine.EventSystems;

namespace SemillasVivas.Gameplay.Demo
{
    
    public sealed class MobileJoystick : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Visual (dejar vacío para joystick invisible)")]
        [SerializeField] private RectTransform background; 
        [SerializeField] private RectTransform handle;     

        [Header("Configuración")]
        [Tooltip("Pixels de movimiento mínimo antes de registrar input (evita drift).")]
        [SerializeField] private float deadZone = 12f;
        [Tooltip("Pixels de movimiento que corresponden a input completo (-1 / +1). " +
                 "Se ignora si Background está asignado (se usa su radio).")]
        [SerializeField] private float fallbackMaxRadius = 90f;

        private Canvas  _canvas;
        private float   _scaleFactor = 1f;
        private float   _radiusInCanvasUnits;
        private Vector2 _anchorScreenPos;
        private bool    _touching;

        private void Awake()
        {
            _canvas      = GetComponentInParent<Canvas>();
            _scaleFactor = _canvas != null ? _canvas.scaleFactor : 1f;

            _radiusInCanvasUnits = background != null
                ? background.sizeDelta.x * 0.5f
                : fallbackMaxRadius / _scaleFactor;

            ResetHandle();
        }

        private void OnEnable()
        {
            
            if (_canvas != null) _scaleFactor = _canvas.scaleFactor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _anchorScreenPos = GetBackgroundScreenCenter();
            _touching        = true;
            UpdateInput(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_touching) return;
            UpdateInput(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _touching                   = false;
            MobileInputState.Horizontal = 0f;
            MobileInputState.Vertical   = 0f;
            ResetHandle();
        }

        private void UpdateInput(Vector2 screenPos)
        {
            
            Vector2 screenDelta = screenPos - _anchorScreenPos;

            Vector2 canvasDelta = screenDelta / _scaleFactor;

            float dx = canvasDelta.x;
            float dy = canvasDelta.y;
            MobileInputState.Horizontal = Mathf.Abs(dx) < (deadZone / _scaleFactor)
                ? 0f
                : Mathf.Clamp(dx / _radiusInCanvasUnits, -1f, 1f);
            MobileInputState.Vertical = Mathf.Abs(dy) < (deadZone / _scaleFactor)
                ? 0f
                : Mathf.Clamp(dy / _radiusInCanvasUnits, -1f, 1f);

            if (handle != null)
            {
                Vector2 clamped = canvasDelta;
                if (clamped.magnitude > _radiusInCanvasUnits)
                    clamped = clamped.normalized * _radiusInCanvasUnits;

                handle.anchoredPosition = clamped;
            }
        }

        private void ResetHandle()
        {
            if (handle != null)
                handle.anchoredPosition = Vector2.zero;
        }

        private Vector2 GetBackgroundScreenCenter()
        {
            RectTransform rt = background != null
                ? background
                : transform as RectTransform;

            if (rt == null) return Vector2.zero;

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;

            if (_canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return new Vector2(worldCenter.x, worldCenter.y);

            return RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, worldCenter);
        }
    }
}
