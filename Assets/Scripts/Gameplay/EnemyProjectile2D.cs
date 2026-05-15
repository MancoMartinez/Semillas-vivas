using SemillasVivas.Gameplay.Demo;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class EnemyProjectile2D : MonoBehaviour
{
    [Header("Daño y vida útil")]
    [Tooltip("Daño que hace al player al impactar.")]
    [SerializeField] private int   damage   = 1;
    [Tooltip("Segundos antes de auto-destruirse si no impacta nada.")]
    [SerializeField] private float lifetime = 5f;

    private void Start()
    {
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        
        DemoPlayerHealth playerHealth = other.GetComponent<DemoPlayerHealth>()
                                     ?? other.GetComponentInParent<DemoPlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (!other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
