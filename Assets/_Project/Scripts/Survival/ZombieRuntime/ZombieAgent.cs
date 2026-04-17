using System;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
public class ZombieAgent : MonoBehaviour, IDamageable
{
    private enum State
    {
        Idle,
        Wander,
        Chase,
        Attack,
        Dead
    }

    [SerializeField] private float maxHealth = 75f;
    [SerializeField] private float currentHealth = 75f;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float attackRange = 2.1f;
    [SerializeField] private float attackCooldown = 1.1f;
    [SerializeField] private float attackDamage = 11f;
    [SerializeField] private float attackHitRadius = 1.45f;
    [SerializeField] private float attackReachForward = 1.15f;
    [SerializeField] private LayerMask playerHitMask = ~0;
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float wanderInterval = 3.2f;
    [SerializeField] private float despawnDistance = 120f;
    [SerializeField] private float deathDestroyDelay = 5f;
    [SerializeField] private bool debugLogs;
    [SerializeField] private float fallbackMoveSpeed = 2.8f;
    [SerializeField] private float fallbackTurnSpeed = 220f;

    private State state = State.Idle;
    private Transform playerTarget;
    private NavMeshAgent agent;
    private ZombieAnimationMachine animationMachine;
    private PlayerStats forcedPlayerStats;
    private float nextAttackTime;
    private float nextWanderTime;
    private Vector3 spawnOrigin;
    private readonly Collider[] attackHits = new Collider[8];

    public event Action<ZombieAgent> Died;

    public bool IsDead => state == State.Dead || currentHealth <= 0f;

    public void ConfigureRuntime(
        Transform target,
        ZombieAnimationMachine animation,
        float healthValue,
        float detectRange,
        float atkRange,
        float atkCooldown,
        float atkDamage,
        float wanderRad,
        float wanderDelay,
        float despawnDist,
        PlayerStats forcedStats,
        bool logs)
    {
        playerTarget = target;
        forcedPlayerStats = forcedStats;
        animationMachine = animation;
        maxHealth = Mathf.Max(1f, healthValue);
        currentHealth = maxHealth;
        detectionRange = Mathf.Max(1f, detectRange);
        attackRange = Mathf.Max(0.2f, atkRange);
        attackCooldown = Mathf.Max(0.1f, atkCooldown);
        attackDamage = Mathf.Max(1f, atkDamage);
        wanderRadius = Mathf.Max(1f, wanderRad);
        wanderInterval = Mathf.Max(0.2f, wanderDelay);
        despawnDistance = Mathf.Max(30f, despawnDist);
        debugLogs = logs;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        spawnOrigin = transform.position;
    }

    private void Update()
    {
        if (IsDead)
        {
            return;
        }

        if (playerTarget == null)
        {
            animationMachine?.SetMotion(0f, false, false);
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance > despawnDistance)
        {
            if (debugLogs)
            {
                Debug.Log($"ZombieAgent: despawned far zombie {name}");
            }

            Destroy(gameObject);
            return;
        }

        if (distance <= attackRange)
        {
            EnterAttack();
        }
        else if (distance <= detectionRange)
        {
            EnterChase();
        }
        else
        {
            EnterWander();
        }

        float speed01 = agent != null && agent.enabled ? agent.velocity.magnitude / Mathf.Max(0.01f, agent.speed) : 0f;
        bool moving = speed01 > 0.1f;
        animationMachine?.SetMotion(speed01, moving, state == State.Attack);
    }

    private void EnterWander()
    {
        if (state != State.Wander)
        {
            state = State.Wander;
        }

        if (Time.time < nextWanderTime)
        {
            return;
        }

        nextWanderTime = Time.time + wanderInterval + UnityEngine.Random.Range(0f, 1.5f);
        Vector3 target = spawnOrigin + UnityEngine.Random.insideUnitSphere * wanderRadius;
        target.y = transform.position.y;
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
    }

    private void EnterChase()
    {
        state = State.Chase;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTarget.position);
            return;
        }

        Vector3 toTarget = playerTarget.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, fallbackTurnSpeed * Time.deltaTime);
        transform.position += transform.forward * fallbackMoveSpeed * Time.deltaTime;
    }

    private void EnterAttack()
    {
        state = State.Attack;
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        Vector3 look = playerTarget.position - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(look.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 360f * Time.deltaTime);
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        animationMachine?.PlayAttack();
        TryApplyAttackDamage();
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (currentHealth > 0f)
        {
            return;
        }

        Die();
    }

    private void Die()
    {
        if (state == State.Dead)
        {
            return;
        }

        state = State.Dead;
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        animationMachine?.PlayDeath();
        Died?.Invoke(this);
        Destroy(gameObject, deathDestroyDelay);
    }

    private void TryApplyAttackDamage()
    {
        Vector3 origin = transform.position + Vector3.up * 1f + transform.forward * attackReachForward;
        int count = Physics.OverlapSphereNonAlloc(origin, attackHitRadius, attackHits, playerHitMask, QueryTriggerInteraction.Ignore);
        bool damaged = false;

        for (int i = 0; i < count; i++)
        {
            Collider hit = attackHits[i];
            if (hit == null)
            {
                continue;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead)
            {
                continue;
            }

            damageable.TakeDamage(attackDamage, gameObject);
            damaged = true;
            break;
        }

        if (damaged)
        {
            return;
        }

        PlayerStats stats = forcedPlayerStats;
        if (stats == null)
        {
            stats = playerTarget != null
            ? (playerTarget.GetComponentInParent<PlayerStats>() ?? playerTarget.GetComponentInChildren<PlayerStats>())
            : null;
        }

        if (stats == null)
        {
            stats = FindAnyObjectByType<PlayerStats>();
        }

        if (stats != null && !stats.IsDead)
        {
            stats.TakeDamage(attackDamage, gameObject);
            if (debugLogs)
            {
                Debug.Log($"ZombieAgent: {name} hit player for {attackDamage:0.0} damage. HP now {stats.CurrentHealth:0.0}/{stats.MaxHealth:0.0}");
            }
        }
        else if (debugLogs)
        {
            Debug.LogWarning($"ZombieAgent: {name} attack had no valid PlayerStats target.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 origin = transform.position + Vector3.up * 1f + transform.forward * attackReachForward;
        Gizmos.DrawWireSphere(origin, attackHitRadius);
    }
}
