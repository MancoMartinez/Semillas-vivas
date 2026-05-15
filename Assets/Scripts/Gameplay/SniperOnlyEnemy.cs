using System.Collections;
using UnityEngine;
using SemillasVivas.Gameplay.Boss;

public class SniperOnlyEnemy : MonoBehaviour
{
    [Header("Sniper Settings")]
    public float shootRange = 15f;   
    public GameObject shootPrefab;
    public Transform shootborn;
    public float shootCooldown = 2f;

    [Header("Health")]
    public int maxHealth = 1; 
    private int currentHealth;
    private bool isDead = false;

    private float shootTimer;
    private Transform player;
    private Animator animator;

    private string currentState;
    private readonly string Anim_Idle = "Idle";
    private readonly string Anim_Attack1 = "Attack1";
    private readonly string Anim_Defeat = "Defeat";

    private bool isAttacking = false;
    private float attackAnimTimer = 0f;
    private float attackAnimDuration = 0.5f;
    private BossFightController bossFightController;

    private bool _isSpawning = false;
    private const float SpawnDuration = 2f;

    void Start()
    {
        animator = GetComponent<Animator>();
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        currentHealth = maxHealth;
        shootTimer = shootCooldown;
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
        if (shootTimer < effectiveShootCooldown) shootTimer += Time.deltaTime;

        if (isAttacking)
        {
            attackAnimTimer -= Time.deltaTime;
            if (attackAnimTimer <= 0) isAttacking = false;
            else return; 
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= shootRange)
        {
            FacePlayer();

            if (shootTimer >= effectiveShootCooldown)
            {
                PerformShoot();
            }
            else
            {
                ChangeAnimationState(Anim_Idle);
            }
        }
        else
        {
            ChangeAnimationState(Anim_Idle);
        }
    }

    private void FacePlayer()
    {
        if (player.position.x > transform.position.x)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (player.position.x < transform.position.x)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    private void PerformShoot()
    {
        shootTimer = 0f;
        isAttacking = true;
        attackAnimTimer = attackAnimDuration;
        ChangeAnimationState(Anim_Attack1);

        if (shootPrefab != null && shootborn != null)
        {
            GameObject proj = Instantiate(shootPrefab, shootborn.position, shootborn.rotation);
            EnemyShootProjectile script = proj.GetComponent<EnemyShootProjectile>();
            if (script != null) script.SetTarget(player);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        isDead = true;
        ChangeAnimationState(Anim_Defeat);
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        Destroy(gameObject, 2f);
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
