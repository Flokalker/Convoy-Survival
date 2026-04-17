using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ZombieAI))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Rigidbody))]
public class ZombieSpecialMechanics : MonoBehaviour
{
    public enum ZombieMechanic
    {
        None,
        Berserker,
        Exploder,
        Leaper
    }

    [Header("Profile")]
    [SerializeField] private ZombieMechanic mechanic = ZombieMechanic.None;

    [Header("Berserker")]
    [SerializeField, Range(0.05f, 0.95f)] private float enrageHealthThreshold = 0.45f;
    [SerializeField, Min(1f)] private float enragedSpeedMultiplier = 1.55f;

    [Header("Exploder")]
    [SerializeField, Min(0f)] private float explosionRadius = 4f;
    [SerializeField, Min(0f)] private float explosionDamage = 28f;
    [SerializeField] private LayerMask explosionMask = ~0;

    [Header("Leaper")]
    [SerializeField, Min(0f)] private float lungeForce = 8f;
    [SerializeField, Min(0f)] private float lungeUpForce = 2.4f;
    [SerializeField, Min(0f)] private float lungeCooldown = 3f;
    [SerializeField, Min(0f)] private float lungeTriggerDistance = 7f;

    private ZombieAI ai;
    private Health health;
    private Rigidbody rb;
    private bool enraged;
    private float nextLungeTime;

    public ZombieMechanic Mechanic => mechanic;

    private void Awake()
    {
        ai = GetComponent<ZombieAI>();
        health = GetComponent<Health>();
        rb = GetComponent<Rigidbody>();

        if (health != null)
        {
            health.Died += OnDied;
        }
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
        if (health == null || health.IsDead)
        {
            return;
        }

        HandleEnrage();
        HandleLunge();
    }

    public void ConfigureRuntime(ZombieMechanic runtimeMechanic)
    {
        mechanic = runtimeMechanic;
    }

    private void HandleEnrage()
    {
        if (mechanic != ZombieMechanic.Berserker || enraged || ai == null || health == null)
        {
            return;
        }

        float ratio = health.CurrentHealth / Mathf.Max(1f, health.MaxHealth);
        if (ratio > enrageHealthThreshold)
        {
            return;
        }

        enraged = true;
        ai.SetExternalSpeedMultiplier(enragedSpeedMultiplier);
    }

    private void HandleLunge()
    {
        if (mechanic != ZombieMechanic.Leaper || Time.time < nextLungeTime)
        {
            return;
        }

        Transform target = ResolveTarget();
        if (target == null)
        {
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        float distance = toTarget.magnitude;
        if (distance > lungeTriggerDistance || distance < 1f)
        {
            return;
        }

        Vector3 planarDirection = new Vector3(toTarget.x, 0f, toTarget.z);
        if (planarDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        rb.AddForce(planarDirection.normalized * lungeForce + Vector3.up * lungeUpForce, ForceMode.VelocityChange);
        nextLungeTime = Time.time + lungeCooldown;
    }

    private void OnDied()
    {
        if (mechanic != ZombieMechanic.Exploder || explosionRadius <= 0f || explosionDamage <= 0f)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, explosionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null && damageable != health)
            {
                damageable.TakeDamage(explosionDamage, gameObject);
            }
        }
    }

    private Transform ResolveTarget()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentThreatTarget != null)
        {
            return GameManager.Instance.CurrentThreatTarget;
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
            // Player tag may not exist in prototype scene setup.
        }

        GameObject named = GameObject.Find("Player");
        return named != null ? named.transform : null;
    }
}
