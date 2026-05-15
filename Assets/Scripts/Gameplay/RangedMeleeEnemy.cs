using UnityEngine;
using System.Collections;
using SemillasVivas.Gameplay.Demo;
using SemillasVivas.Gameplay.Boss;

public class RangedMeleeEnemy : MonoBehaviour
{
    
    public System.Action OnDied;

    public System.Action<int, int> OnHealthChanged;

    [Header("Ranges")]
    public float farRange = 10f;     
    public float mediumRange = 5f;   
    public float meleeRange = 1.5f;  

    [Header("Shooting")]
    public GameObject shootPrefab;   
    public Transform shootborn;      
    public float shootCooldown = 2f; 
    private float shootTimer;

    [Header("Melee")]
    public float meleeCooldown = 1.5f;
    private float meleeTimer;
    public int meleeDamage = 10;

    [Header("Movement")]
    public float walkSpeed = 3f;
    
    [Header("Health")]
    public int maxHealth = 50;
    private int currentHealth;
    private bool isDead = false;

    [Header("Phase 2")]
    public GameObject nextPhaseObject; 

    private Transform player;
    private Animator animator;

    private string currentState;
    private readonly string Anim_Idle = "Idle";
    private readonly string Anim_Walk = "Walk";
    private readonly string Anim_Attack1 = "Attack1";
    private readonly string Anim_Defeat = "Defeat";
    private BossFightController bossFightController;

    private bool isAttacking = false;
    private float attackAnimTimer = 0f;
    private float attackAnimDuration = 0.5f; 

    private bool _isSpawning = false;
    private const float SpawnDuration = 2f;

    void Start()
    {
        animator = GetComponent<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("RangedMeleeEnemy: No se encontró ningún objeto con el tag 'Player'.");
        }

        currentHealth = maxHealth;
        shootTimer = shootCooldown;
        meleeTimer = meleeCooldown;

        UpdateAttackDuration();
        bossFightController = FindFirstObjectByType<BossFightController>();

        StartCoroutine(SpawnIntroRoutine());
    }

    private void UpdateAttackDuration()
    {
        if (animator == null) return;
        RuntimeAnimatorController ac = animator.runtimeAnimatorController;
        if (ac != null)
        {
            foreach (AnimationClip clip in ac.animationClips)
            {
                if (clip.name == Anim_Attack1)
                {
                    attackAnimDuration = clip.length;
                    return;
                }
            }
        }
    }

    void Update()
    {
        if (isDead || _isSpawning || player == null) return;

        float effectiveShootCooldown = shootCooldown * GetAttackCooldownMultiplier();
        float effectiveMeleeCooldown = meleeCooldown * GetAttackCooldownMultiplier();
        if (shootTimer < effectiveShootCooldown) shootTimer += Time.deltaTime;
        if (meleeTimer < effectiveMeleeCooldown) meleeTimer += Time.deltaTime;

        if (isAttacking)
        {
            attackAnimTimer -= Time.deltaTime;
            if (attackAnimTimer <= 0)
            {
                isAttacking = false; 
            }
            else
            {
                return; 
            }
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= farRange && distanceToPlayer > meleeRange)
        {
            FacePlayer();
        }

        if (distanceToPlayer > farRange)
        {
            
            ChangeAnimationState(Anim_Idle);
        }
        else if (distanceToPlayer <= farRange && distanceToPlayer > mediumRange)
        {
            
            if (shootTimer >= effectiveShootCooldown)
            {
                PerformShoot();
            }
            else
            {
                ChangeAnimationState(Anim_Idle); 
            }
        }
        else if (distanceToPlayer <= mediumRange && distanceToPlayer > meleeRange)
        {
            
            ChangeAnimationState(Anim_Walk);
            MoveTowardsPlayer();
        }
        else if (distanceToPlayer <= meleeRange)
        {
            
            if (meleeTimer >= effectiveMeleeCooldown)
            {
                PerformMeleeAttack();
            }
            else
            {
                ChangeAnimationState(Anim_Idle); 
            }
        }
    }

    private void MoveTowardsPlayer()
    {
        transform.position = Vector2.MoveTowards(transform.position, player.position, walkSpeed * Time.deltaTime);
    }

    private void FacePlayer()
    {
        if (player.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (player.position.x < transform.position.x)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    private void PerformShoot()
    {
        shootTimer = 0f;
        isAttacking = true;
        attackAnimTimer = attackAnimDuration; 
        ChangeAnimationState(Anim_Attack1);
        
        if (shootPrefab == null)
        {
            Debug.LogError("RangedMeleeEnemy: 'Shoot Prefab' no está asignado en el Inspector.");
            return;
        }
        
        if (shootborn == null)
        {
            Debug.LogError("RangedMeleeEnemy: 'Shootborn' no está asignado en el Inspector. Usando el transform del enemigo.");
            shootborn = transform;
        }

        Debug.Log("RangedMeleeEnemy: Instanciando disparo...");
        GameObject proj = Instantiate(shootPrefab, shootborn.position, shootborn.rotation);
        EnemyShootProjectile script = proj.GetComponent<EnemyShootProjectile>();
        if (script != null)
        {
            script.SetTarget(player);
        }
        else
        {
            Debug.LogWarning("RangedMeleeEnemy: El prefab instanciado no tiene el script 'EnemyShootProjectile'.");
        }
    }

    private void PerformMeleeAttack()
    {
        meleeTimer = 0f;
        isAttacking = true;
        attackAnimTimer = attackAnimDuration;
        ChangeAnimationState(Anim_Attack1);

        if (player == null) return;

        DemoPlayerHealth playerHealth = player.GetComponent<DemoPlayerHealth>()
                                     ?? player.GetComponentInParent<DemoPlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.TakeDamage(1);

        float dir = Mathf.Sign(player.position.x - transform.position.x);
        playerHealth.ApplyKnockback(new Vector2(dir * 7f, 4f));
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        SpriteRenderer sr = GetComponent<SpriteRenderer>()
                         ?? GetComponentInChildren<SpriteRenderer>();
        if (sr != null) StartCoroutine(FlashDamageRoutine(sr));

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashDamageRoutine(SpriteRenderer sr)
    {
        Color original = sr.color;
        sr.color = new Color(1f, 0.25f, 0.25f, 1f);
        yield return new WaitForSeconds(0.1f);
        if (sr != null) sr.color = original;
    }

    private void Die()
    {
        isDead = true;
        ChangeAnimationState(Anim_Defeat);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        OnDied?.Invoke();

        StartCoroutine(ActivatePhase2AndDie());
    }

    private IEnumerator ActivatePhase2AndDie()
    {
        
        yield return new WaitForSeconds(3f);

        if (nextPhaseObject != null)
        {
            nextPhaseObject.SetActive(true);
        }

        Destroy(gameObject);
    }

    private void ChangeAnimationState(string newState)
    {
        if (currentState == newState) return;
        
        if (animator != null)
        {
            animator.Play(newState);
            currentState = newState;
        }
    }

    private float GetAttackCooldownMultiplier()
    {
        return bossFightController != null && bossFightController.AreAttacksSlowed() ? 1.75f : 1f;
    }

    private IEnumerator SpawnIntroRoutine()
    {
        _isSpawning = true;
        SpriteRenderer sr = GetComponent<SpriteRenderer>()
                         ?? GetComponentInChildren<SpriteRenderer>();
        Color baseColor = sr != null ? sr.color : Color.white;
        float elapsed = 0f;

        while (elapsed < SpawnDuration)
        {
            if (sr != null)
            {
                float alpha = (Mathf.Sin(elapsed * 14f) + 1f) * 0.4f + 0.2f;
                sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (sr != null)
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);

        _isSpawning = false;
    }
}
