using UnityEngine;
using SemillasVivas.Gameplay.Demo;

public class EnemyShootProjectile : MonoBehaviour
{
    public float speed = 5f;
    public int damage = 10;
    public float lifetime = 5f;

    private Vector2 moveDirection;

    void Start()
    {
        
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;

        Destroy(gameObject, lifetime);
    }

    public void SetTarget(Transform target)
    {
        if (target != null)
        {
            
            moveDirection = new Vector2(target.position.x - transform.position.x, target.position.y - transform.position.y).normalized;
            
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
    }

    void Update()
    {
        
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            
            DemoPlayerHealth health = other.GetComponent<DemoPlayerHealth>()
                                   ?? other.GetComponentInParent<DemoPlayerHealth>();
            if (health != null)
            {
                
                health.TakeDamage(1);
            }

            Destroy(gameObject);
        }
        else if (!other.CompareTag("Enemy") && !other.isTrigger)
        {
            Destroy(gameObject);
        }
    }
}
