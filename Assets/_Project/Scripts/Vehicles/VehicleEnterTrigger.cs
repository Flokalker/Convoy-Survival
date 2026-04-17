using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class VehicleEnterTrigger : MonoBehaviour
{
    [SerializeField] private SimpleVehicleController vehicle;

    private GameObject playerObject;
    private int playerColliderCount;
    private bool playerInRange;

    public bool PlayerInRange => playerInRange;
    public GameObject PlayerObject => playerObject;

    public void SetVehicle(SimpleVehicleController targetVehicle)
    {
        vehicle = targetVehicle;
    }

    public bool HasPlayerInRange(GameObject preferredPlayer)
    {
        if (!playerInRange || playerObject == null)
        {
            return false;
        }

        return preferredPlayer == null || preferredPlayer == playerObject;
    }

    public GameObject GetPlayerInRange(GameObject preferredPlayer)
    {
        if (!HasPlayerInRange(preferredPlayer))
        {
            return null;
        }

        return playerObject;
    }

    public void ResetTracking()
    {
        playerColliderCount = 0;
        playerInRange = false;
        playerObject = null;
    }

    private void Reset()
    {
        vehicle = GetComponentInParent<SimpleVehicleController>();

        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        if (vehicle == null)
        {
            vehicle = GetComponentInParent<SimpleVehicleController>();
        }
    }

    private void OnDisable()
    {
        ResetTracking();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TryGetPlayerObject(other, out GameObject detectedPlayer))
        {
            return;
        }

        playerColliderCount++;
        playerObject = detectedPlayer;

        if (playerInRange)
        {
            return;
        }

        playerInRange = true;
        Debug.Log("Player entered trigger", this);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!playerInRange && TryGetPlayerObject(other, out GameObject detectedPlayer))
        {
            playerObject = detectedPlayer;
            playerColliderCount = Mathf.Max(1, playerColliderCount);
            playerInRange = true;
            Debug.Log("Player entered trigger", this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!TryGetPlayerObject(other, out GameObject detectedPlayer))
        {
            return;
        }

        if (playerObject != null && detectedPlayer != playerObject)
        {
            return;
        }

        playerColliderCount = Mathf.Max(0, playerColliderCount - 1);
        if (playerColliderCount > 0)
        {
            return;
        }

        if (!playerInRange)
        {
            return;
        }

        playerInRange = false;
        playerObject = null;
        Debug.Log("Player exited trigger", this);
    }

    private bool TryGetPlayerObject(Collider other, out GameObject detectedPlayer)
    {
        detectedPlayer = null;
        if (other == null)
        {
            return false;
        }

        Transform candidateTransform = other.transform;
        Transform candidateRoot = candidateTransform.root;

        if (!candidateTransform.CompareTag("Player") && !candidateRoot.CompareTag("Player"))
        {
            return false;
        }

        detectedPlayer = candidateRoot.gameObject;
        return true;
    }
}
