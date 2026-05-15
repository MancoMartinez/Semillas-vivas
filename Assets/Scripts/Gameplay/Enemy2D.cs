using System.Collections;
using SemillasVivas.Gameplay.Demo;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class Enemy2D : MonoBehaviour
{
    
    [Header("Rangos de comportamiento")]
    [Tooltip("Distancia máxima a la que el enemigo detecta al player. " +
             "Dentro de este rango camina hacia él y dispara.")]
    [SerializeField] private float detectRange = 8f;

    [Tooltip("Distancia mínima para el ataque melee (Attack1). " +
             "Cuando el player entra aquí, el enemigo para y golpea.")]
    [SerializeField] private float meleeRange  = 1.5f;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Vida")]
    [SerializeField] private int maxHealth = 3;

    [Header("Disparo")]
    [Tooltip("Prefab del proyectil. Asignar en Inspector.")]
    [SerializeField] private GameObject shootPrefab;
    [Tooltip("Segundos entre cada disparo.")]
    [SerializeField] private float shootInterval   = 2f;
    [SerializeField] private float projectileSpeed = 5f;
    [Tooltip("Nombre del hijo desde donde se instancia el proyectil (ej: 'shootborn').")]
    [SerializeField] private string shootBornName  = "shootborn";

    [Header("Ataque melee")]
    [SerializeField] private int   meleeDamage    = 1;
    [SerializeField] private float meleeCooldown  = 1.2f;

    [Header("Estados del Animator (cambiar si tienen otro nombre)")]
    [SerializeField] private string idleAnim   = "Idle";
    [SerializeField] private string walkAnim   = "Walk";
    [SerializeField] private string attackAnim = "Attack1";
    [SerializeField] private string defeatAnim = "Defeat";

    private Animator       _anim;
    private SpriteRenderer _sr;
    private Rigidbody2D    _rb;
    private Transform      _player;
    private Transform      _shootBorn;
    private int            _health;
    private bool           _isDead;
    private float          _shootTimer;
    private float          _meleeTimer;

    private void Start()
    {
        _anim   = GetComponent<Animator>();
        _sr     = GetComponentInChildren<SpriteRenderer>(true);
        _rb     = GetComponent<Rigidbody2D>();
        _health = maxHealth;

        _rb.freezeRotation         = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation          = RigidbodyInterpolation2D.Interpolate;

        if (!string.IsNullOrEmpty(shootBornName))
        {
            _shootBorn = transform.Find(shootBornName);
            if (_shootBorn == null)
                Debug.LogWarning($"[Enemy2D] '{name}': no encontré hijo '{shootBornName}'. " +
                                 $"El proyectil saldrá del centro del enemigo.");
        }

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            _player = playerGO.transform;
        else
            Debug.LogWarning($"[Enemy2D] '{name}': no se encontró objeto con tag 'Player'.");
    }

    private void Update()
    {
        if (_isDead || _player == null) return;

        _shootTimer -= Time.deltaTime;
        _meleeTimer -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, _player.position);

        if (dist > detectRange)
        {
            
            Idle();
        }
        else if (dist <= meleeRange)
        {
            
            SetVelocityX(0f);
            DoMelee();
        }
        else
        {
            
            WalkAndShoot();
        }
    }

    private void Idle()
    {
        SetVelocityX(0f);
        
        if (!IsAttackPlaying())
            PlayAnim(idleAnim);
    }

    private void WalkAndShoot()
    {
        
        float dir = Mathf.Sign(_player.position.x - transform.position.x);
        SetVelocityX(dir * moveSpeed);
        Flip(dir);

        if (!IsAttackPlaying())
            PlayAnim(walkAnim);

        TryShoot();
    }

    private void DoMelee()
    {
        
        if (_meleeTimer > 0f)
        {
            if (!IsAttackPlaying())
                PlayAnim(idleAnim);
            return;
        }

        _meleeTimer = meleeCooldown;
        PlayAnim(attackAnim, force: true);

        DemoPlayerHealth hp = _player.GetComponent<DemoPlayerHealth>()
                           ?? _player.GetComponentInParent<DemoPlayerHealth>();
        hp?.TakeDamage(meleeDamage);
    }

    private void TryShoot()
    {
        if (_shootTimer > 0f || shootPrefab == null) return;

        _shootTimer = shootInterval;

        Vector2 spawnPos = _shootBorn != null
            ? (Vector2)_shootBorn.position
            : (Vector2)transform.position;

        Vector2 dir = ((Vector2)_player.position - spawnPos).normalized;

        GameObject proj = Instantiate(shootPrefab, spawnPos, Quaternion.identity);
        Rigidbody2D projRb = proj.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.gravityScale   = 0f;
            projRb.linearVelocity = dir * projectileSpeed;
        }

        PlayAnim(attackAnim, force: true);
        Flip(dir.x);
    }

    public void TakeDamage(int amount)
    {
        if (_isDead) return;
        _health -= amount;
        if (_health <= 0)
            StartCoroutine(DieRoutine());
        else
            StartCoroutine(FlashRoutine());
    }

    private IEnumerator DieRoutine()
    {
        _isDead = true;
        SetVelocityX(0f);
        PlayAnim(defeatAnim, force: true);
        yield return new WaitForSeconds(1.2f);
        Destroy(gameObject);
    }

    private IEnumerator FlashRoutine()
    {
        if (_sr == null) yield break;
        Color orig = _sr.color;
        _sr.color = new Color(1f, 0.35f, 0.35f, 1f);
        yield return new WaitForSeconds(0.1f);
        if (_sr != null) _sr.color = orig;
    }

    private bool IsAttackPlaying()
    {
        if (_anim == null) return false;
        AnimatorStateInfo s = _anim.GetCurrentAnimatorStateInfo(0);
        return s.IsName(attackAnim) && s.normalizedTime < 0.9f;
    }

    private void PlayAnim(string state, bool force = false)
    {
        if (_anim == null || string.IsNullOrEmpty(state)) return;
        if (!force && _anim.GetCurrentAnimatorStateInfo(0).IsName(state)) return;
        _anim.Play(state, 0, 0f);
    }

    private void SetVelocityX(float x)
    {
        if (_rb != null)
            _rb.linearVelocity = new Vector2(x, _rb.linearVelocity.y);
    }

    private void Flip(float dirX)
    {
        if (_sr == null || Mathf.Abs(dirX) < 0.01f) return;
        _sr.flipX = dirX < 0f;
    }

    private void OnDrawGizmosSelected()
    {
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
