using System.Collections.Generic;
using UnityEngine;

namespace SemillasVivas.Gameplay.Demo
{
    public sealed class DemoEnemyPatrolPath : MonoBehaviour
    {
        [SerializeField] private Transform[] points;
        [SerializeField] private bool pingPong = true;
        [SerializeField] private float arriveDistance = 0.1f;

        private int _currentIndex;
        private int _direction = 1;

        public bool HasPoints => points != null && points.Length > 0;

        public Vector2 CurrentPoint => HasPoints ? points[_currentIndex].position : transform.position;

        private void Awake()
        {
            if (HasPoints)
            {
                SanitizePoints();
                return;
            }

            Transform root = transform.Find("PatrolPoints");

            if (root == null)
            {
                return;
            }

            List<Transform> discoveredPoints = new();

            for (int index = 0; index < root.childCount; index++)
            {
                Transform child = root.GetChild(index);

                if (child != null)
                {
                    discoveredPoints.Add(child);
                }
            }

            points = discoveredPoints.ToArray();
            SanitizePoints();
        }

        public void AdvanceIfReached(Vector2 currentPosition)
        {
            if (!HasPoints || Mathf.Abs(currentPosition.x - CurrentPoint.x) > arriveDistance || points.Length == 1)
            {
                return;
            }

            if (pingPong)
            {
                if (_currentIndex >= points.Length - 1)
                {
                    _direction = -1;
                }
                else if (_currentIndex <= 0)
                {
                    _direction = 1;
                }

                _currentIndex += _direction;
                return;
            }

            _currentIndex = (_currentIndex + 1) % points.Length;
        }

        private void SanitizePoints()
        {
            if (points == null || points.Length == 0)
            {
                return;
            }

            List<Transform> validPoints = new();

            for (int index = 0; index < points.Length; index++)
            {
                if (points[index] != null)
                {
                    validPoints.Add(points[index]);
                }
            }

            points = validPoints.ToArray();
            _currentIndex = Mathf.Clamp(_currentIndex, 0, Mathf.Max(0, points.Length - 1));
        }
    }
}
