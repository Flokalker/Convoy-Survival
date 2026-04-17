using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class SurvivalWeaponPickup : MonoBehaviour
{
    [SerializeField] private SurvivalWeaponDefinition weaponDefinition;
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private float spinSpeed = 45f;

    private void Awake()
    {
        Collider c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    private void Update()
    {
        if (spinSpeed > 0f)
        {
            transform.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.World);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (weaponDefinition == null)
        {
            return;
        }

        SurvivalWeaponManager manager = other.GetComponentInParent<SurvivalWeaponManager>();
        if (manager == null)
        {
            return;
        }

        if (manager.AddWeapon(weaponDefinition))
        {
            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
        }
    }

    public void Configure(SurvivalWeaponDefinition definition)
    {
        weaponDefinition = definition;
    }
}
