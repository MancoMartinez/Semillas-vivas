using UnityEngine;
using UnityEngine.UI;

namespace SemillasVivas.Gameplay.Demo
{
    
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EmptyGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
