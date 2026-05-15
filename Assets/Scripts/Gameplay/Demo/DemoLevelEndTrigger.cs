using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    
    public sealed class DemoLevelEndTrigger : MonoBehaviour
    {
        private DemoLevelFlowController _flowController;

        public void Setup(DemoLevelFlowController flowController)
        {
            _flowController = flowController;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_flowController == null)
            {
                return;
            }

            bool isPlayer = other.GetComponent<DemoPlayerHealth>() != null
                            || other.GetComponentInParent<DemoPlayerHealth>() != null;

            if (!isPlayer)
            {
                return;
            }

            _flowController.CompleteLevel();
        }
    }
}
