using UnityEngine;
using UnityEngine.EventSystems;

namespace SemillasVivas.Gameplay.Demo
{
    public enum MobileButtonAction { Jump, Attack }

    public sealed class MobileButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler
    {
        private MobileButtonAction _action;

        public void Setup(MobileButtonAction action)
        {
            _action = action;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_action == MobileButtonAction.Jump)
            {
                MobileInputState.QueueJump();
                MobileInputState.JumpHeld = true; 
            }
            else
            {
                MobileInputState.QueueAttack();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_action == MobileButtonAction.Jump)
                MobileInputState.JumpHeld = false; 
        }
    }
}
