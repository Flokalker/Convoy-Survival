using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
 [RequireComponent(typeof(Rigidbody))]
public class ZombieAI : MonoBehaviour
{
    public enum ZombieState
    {
        Idle,
        Chasing,
        Attacking,
        Dying
    }

    public enum ZombieVariant
    {
        Runner,
        Tank,
        Spitter
    }

    [System.Serializable]
    private class VariantStats
    {
        [Min(0.1f)] public float moveSpeed = 3.5f;
        [Min(1f)] public float maxHealth = 100f;
        [Min(0f)] public float attackDamage = 12f;
        [Min(0.5f)] public float meleeRange = 1.8f;
        [Min(0.1f)] public float attackRate = 1f;
        [Min(0f)] public float stoppingDistance = 1.2f;
    }

    [Header("Core")]
    [SerializeField] private ZombieVariant variant = ZombieVariant.Runner;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Health health;
    [SerializeField] private Animator animator;

    [Header("Runner Stats")]
    [SerializeField] private VariantStats runnerStats = new VariantStats
    {
        moveSpeed = 5.2f,
        maxHealth = 45f,
        attackDamage = 10f,
        meleeRange = 1.5f,
        attackRate = 1.4f,
        stoppingDistance = 1.1f
    };

    [Header("Tank Stats")]
    [SerializeField] private VariantStats tankStats = new VariantStats
    {
        moveSpeed = 2f,
        maxHealth = 140f,
        attackDamage = 28f,
        meleeRange = 2.2f,
        attackRate = 0.7f,
        stoppingDistance = 1.4f
    };

    [Header("Spitter Stats")]
    [SerializeField] private VariantStats spitterStats = new VariantStats
    {
        moveSpeed = 2.8f,
        maxHealth = 70f,
        attackDamage = 13f,
        meleeRange = 1.6f,
        attackRate = 0.8f,
        stoppingDistance = 8f
    };

    [Header("Spitter")]
    [SerializeField] private GameObject spitProjectilePrefab;
    [SerializeField] private Transform spitOrigin;
    [SerializeField, Min(1f)] private float spitRange = 12f;
    [SerializeField, Min(0.1f)] private float spitCooldown = 1.6f;
    [SerializeField, Min(1f)] private float spitProjectileSpeed = 14f;

    [Header("Death")]
    [SerializeField] private bool useRagdollOnDeath = true;
    [SerializeField, Min(0f)] private float destroyAfterDeathDelay = 6f;
    
    [Header("Fallback Movement")]
    [SerializeField, Min(0.1f)] private float nonNavmeshMoveSpeed = 3.2f;
    [SerializeField, Min(60f)] private float nonNavmeshTurnSpeed = 220f;
    
    [Header("Gravity")]
    [SerializeField] private bool useGravity = true;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField, Min(0.1f)] private float gravity = -25f;
    [SerializeField, Min(0.01f)] private float groundCheckExtraDistance = 0.25f;

    private ZombieState currentState = ZombieState.Idle;
    private VariantStats activeStats;
    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;
    private float nextAttackTime;
    private float nextSpitTime;
    private Transform target;
    private float nextTargetSearchTime;
    private float verticalVelocity;
    private Collider bodyCollider;
    private Rigidbody rb;
    private float externalSpeedMultiplier = 1f;

    public ZombieState CurrentState => currentState;
    public ZombieVariant Variant => variant;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
    }

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (health == null)
        {
            health = GetComponent<Health>();
        }
        
        bodyCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        ConfigurePhysicsBody();

        ragdollBodies = GetComponentsInChildren<Rigidbody>(true);
        ragdollColliders = GetComponentsInChildren<Collider>(true);

        SetRagdollEnabled(false);
        if (agent != null)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        ApplyVariantStats();
        health.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= OnDied;
        }
    }

    private void Update()
    {
        if (currentState == ZombieState.Dying)
        {
            return;
        }
        
        if (agent != null && agent.enabled)
        {
            agent.nextPosition = transform.position;
        }

        target = ResolveTarget();
        if (target == null)
        {
            SwitchState(ZombieState.Idle);
            ApplyGravity();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);
        bool inMeleeRange = distance <= activeStats.meleeRange;
        bool isSpitter = variant == ZombieVariant.Spitter;
        bool inSpitRange = isSpitter && distance <= spitRange && distance > activeStats.meleeRange;

        if (inMeleeRange || inSpitRange)
        {
            SwitchState(ZombieState.Attacking);
            HandleAttack(distance);
            ApplyGravity();
            return;
        }

        SwitchState(ZombieState.Chasing);
        ChaseTarget();
        ApplyGravity();
    }

    private void HandleAttack(float distance)
    {
        if (target == null)
        {
            return;
        }

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
        Vector3 flatLook = new Vector3(target.position.x, transform.position.y, target.position.z);
        transform.LookAt(flatLook);

        bool inMeleeRange = distance <= activeStats.meleeRange;
        if (inMeleeRange && Time.time >= nextAttackTime)
        {
            PerformMeleeAttack();
            nextAttackTime = Time.time + (1f / Mathf.Max(0.1f, activeStats.attackRate));
            return;
        }

        if (variant == ZombieVariant.Spitter && distance <= spitRange && Time.time >= nextSpitTime)
        {
            PerformSpitAttack();
            nextSpitTime = Time.time + spitCooldown;
        }
    }

    private void PerformMeleeAttack()
    {
        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(activeStats.attackDamage, gameObject);
            return;
        }

        VehicleController vehicle = target.GetComponentInParent<VehicleController>();
        if (vehicle != null && vehicle.Stats != null)
        {
            vehicle.Stats.ApplyDamage(activeStats.attackDamage);
        }
    }

    private void PerformSpitAttack()
    {
        if (spitProjectilePrefab == null || spitOrigin == null)
        {
            PerformMeleeAttack();
            return;
        }

        GameObject projectileObject = Instantiate(spitProjectilePrefab, spitOrigin.position, Quaternion.identity);
        ZombieProjectile projectile = projectileObject.GetComponent<ZombieProjectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<ZombieProjectile>();
        }

        Vector3 direction = (target.position - spitOrigin.position).normalized;
        projectile.Launch(direction, spitProjectileSpeed, activeStats.attackDamage, gameObject);
    }

    private void OnDied()
    {
        SwitchState(ZombieState.Dying);
        agent.isStopped = true;
        agent.enabled = false;

        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        if (useRagdollOnDeath)
        {
            SetRagdollEnabled(true);
        }

        Destroy(gameObject, destroyAfterDeathDelay);
    }

    private Transform ResolveTarget()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentThreatTarget != null)
        {
            return GameManager.Instance.CurrentThreatTarget;
        }

        if (Time.time < nextTargetSearchTime && target != null)
        {
            return target;
        }

        nextTargetSearchTime = Time.time + 0.5f;

        PlayerStats playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats != null)
        {
            return playerStats.transform;
        }

        try
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null)
            {
                return tagged.transform;
            }
        }
        catch (UnityException)
        {
            // Ignore when Player tag is missing in early prototype scenes.
        }

        GameObject named = GameObject.Find("Player");
        return named != null ? named.transform : null;
    }

    private void ChaseTarget()
    {
        if (target == null)
        {
            return;
        }

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);

            Vector3 desiredVelocity = agent.desiredVelocity;
            Vector3 planar = new Vector3(desiredVelocity.x, 0f, desiredVelocity.z);
            if (planar.sqrMagnitude > 0.0001f)
            {
                Vector3 step = Vector3.ClampMagnitude(planar, activeStats.moveSpeed) * Time.deltaTime;
                rb.MovePosition(rb.position + step);

                Quaternion desiredRotation = Quaternion.LookRotation(planar.normalized, Vector3.up);
                Quaternion rotated = Quaternion.RotateTowards(transform.rotation, desiredRotation, nonNavmeshTurnSpeed * Time.deltaTime);
                rb.MoveRotation(rotated);
            }

            return;
        }

        Vector3 toTarget = target.position - transform.position;
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        if (flat.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion desired = Quaternion.LookRotation(flat.normalized, Vector3.up);
        Quaternion rotatedFallback = Quaternion.RotateTowards(transform.rotation, desired, nonNavmeshTurnSpeed * Time.deltaTime);
        rb.MoveRotation(rotatedFallback);
        Vector3 fallbackForward = rotatedFallback * Vector3.forward;
        rb.MovePosition(rb.position + fallbackForward * nonNavmeshMoveSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (!useGravity)
        {
            return;
        }

        if (rb != null && !rb.isKinematic)
        {
            return;
        }

        // Let NavMesh handle grounded movement when available.
        if (agent.enabled && agent.isOnNavMesh)
        {
            verticalVelocity = -2f;
            return;
        }

        float halfHeight = bodyCollider != null ? bodyCollider.bounds.extents.y : 1f;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        float checkDistance = halfHeight + groundCheckExtraDistance;

        bool grounded = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, checkDistance, groundMask, QueryTriggerInteraction.Ignore);
        if (grounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            float targetY = hit.point.y + halfHeight;
            if (transform.position.y < targetY + 0.15f)
            {
                transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
            transform.position += Vector3.up * verticalVelocity * Time.deltaTime;
        }
    }

    private void ConfigurePhysicsBody()
    {
        if (rb == null)
        {
            return;
        }

        rb.mass = 65f;
        rb.linearDamping = 0.15f;
        rb.angularDamping = 2f;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void ApplyVariantStats()
    {
        activeStats = GetVariantStats(variant);
        agent.speed = activeStats.moveSpeed * Mathf.Max(0.1f, externalSpeedMultiplier);
        agent.stoppingDistance = activeStats.stoppingDistance;
        nonNavmeshMoveSpeed = activeStats.moveSpeed * Mathf.Max(0.1f, externalSpeedMultiplier);
        health.SetMaxHealth(activeStats.maxHealth, true);
    }

    public void SetExternalSpeedMultiplier(float multiplier)
    {
        externalSpeedMultiplier = Mathf.Max(0.1f, multiplier);
        if (activeStats != null)
        {
            agent.speed = activeStats.moveSpeed * externalSpeedMultiplier;
            nonNavmeshMoveSpeed = activeStats.moveSpeed * externalSpeedMultiplier;
        }
    }

    public void ConfigureRuntimeVariant(ZombieVariant runtimeVariant, GameObject projectilePrefab = null, Transform projectileOrigin = null)
    {
        variant = runtimeVariant;
        if (projectilePrefab != null)
        {
            spitProjectilePrefab = projectilePrefab;
        }

        if (projectileOrigin != null)
        {
            spitOrigin = projectileOrigin;
        }

        if (agent != null && health != null)
        {
            ApplyVariantStats();
        }
    }

    private VariantStats GetVariantStats(ZombieVariant selectedVariant)
    {
        switch (selectedVariant)
        {
            case ZombieVariant.Runner:
                return runnerStats;
            case ZombieVariant.Tank:
                return tankStats;
            case ZombieVariant.Spitter:
                return spitterStats;
            default:
                return runnerStats;
        }
    }

    private void SwitchState(ZombieState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;
        if (animator != null)
        {
            animator.SetInteger("State", (int)currentState);
        }
    }

    private void SetRagdollEnabled(bool enabledState)
    {
        for (int i = 0; i < ragdollBodies.Length; i++)
        {
            if (ragdollBodies[i].gameObject == gameObject)
            {
                continue;
            }

            ragdollBodies[i].isKinematic = !enabledState;
        }

        for (int i = 0; i < ragdollColliders.Length; i++)
        {
            if (ragdollColliders[i].gameObject == gameObject)
            {
                continue;
            }

            ragdollColliders[i].enabled = enabledState;
        }
    }
}
