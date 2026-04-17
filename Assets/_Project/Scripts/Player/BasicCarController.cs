using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[ExecuteAlways]
[RequireComponent(typeof(Rigidbody))]
public class BasicCarController : MonoBehaviour
{
    [Header("Auto Build")]
    [SerializeField] private bool buildShapeOnStart = true;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Color bodyColor = new Color(0.1f, 0.35f, 0.9f, 1f);
    [SerializeField] private Color cabinColor = new Color(0.15f, 0.45f, 1f, 1f);
    [SerializeField] private Color wheelColor = new Color(0.08f, 0.08f, 0.08f, 1f);

    [Header("Movement")]
    [SerializeField, Min(0f)] private float acceleration = 18f;
    [SerializeField, Min(0f)] private float reverseAcceleration = 12f;
    [SerializeField, Min(0f)] private float maxSpeed = 12f;
    [SerializeField, Min(0f)] private float turnSpeed = 110f;
    [SerializeField, Min(0f)] private float downforce = 8f;

    [Header("Stability")]
    [SerializeField] private bool allowRollAndPitch = false;
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.4f, 0f);

    private Rigidbody body;

    private void Reset()
    {
        body = GetComponent<Rigidbody>();
        body.mass = 900f;
        body.linearDamping = 0.1f;
        body.angularDamping = 1.5f;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider>();
        }

        collider.center = new Vector3(0f, 0.35f, 0f);
        collider.size = new Vector3(1.6f, 0.7f, 3.2f);
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.centerOfMass = centerOfMassOffset;

        if (!allowRollAndPitch)
        {
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        if (buildShapeOnStart)
        {
            EnsureVisualShape();
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying && buildShapeOnStart)
        {
            EnsureVisualShape();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && buildShapeOnStart)
        {
            EnsureVisualShape();
        }
    }

    private void FixedUpdate()
    {
        if (body == null)
        {
            return;
        }

        float throttle = ReadAxis(Key.W, Key.S);
        float steer = ReadAxis(Key.D, Key.A);

        float targetAcceleration = throttle >= 0f ? acceleration : reverseAcceleration;
        Vector3 forwardForce = transform.forward * throttle * targetAcceleration;
        body.AddForce(forwardForce, ForceMode.Acceleration);

        Vector3 flatVelocity = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);
        if (flatVelocity.magnitude > maxSpeed)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * maxSpeed;
            body.linearVelocity = new Vector3(limitedVelocity.x, body.linearVelocity.y, limitedVelocity.z);
        }

        float speedFactor = Mathf.Clamp01(flatVelocity.magnitude / maxSpeed);
        float turnAmount = steer * turnSpeed * speedFactor * Time.fixedDeltaTime;
        if (Mathf.Abs(turnAmount) > 0.0001f)
        {
            Quaternion targetRotation = body.rotation * Quaternion.Euler(0f, turnAmount, 0f);
            body.MoveRotation(targetRotation);
        }

        if (downforce > 0f)
        {
            body.AddForce(Vector3.down * downforce, ForceMode.Acceleration);
        }
    }

    private static float ReadAxis(Key positive, Key negative)
    {
        Keyboard keyboard = Keyboard.current;
        float value = 0f;
        if (keyboard != null)
        {
            if (keyboard[positive].isPressed)
            {
                value += 1f;
            }

            if (keyboard[negative].isPressed)
            {
                value -= 1f;
            }
        }
        else
        {
            if (LegacyKeyDown(positive))
            {
                value += 1f;
            }

            if (LegacyKeyDown(negative))
            {
                value -= 1f;
            }
        }

        return Mathf.Clamp(value, -1f, 1f);
    }

    private static bool LegacyKeyDown(Key key)
    {
        switch (key)
        {
            case Key.W:
                return Input.GetKey(KeyCode.W);
            case Key.A:
                return Input.GetKey(KeyCode.A);
            case Key.S:
                return Input.GetKey(KeyCode.S);
            case Key.D:
                return Input.GetKey(KeyCode.D);
            default:
                return false;
        }
    }

    private void EnsureVisualShape()
    {
        if (visualRoot == null)
        {
            Transform existingRoot = transform.Find("Visual");
            if (existingRoot != null)
            {
                visualRoot = existingRoot;
            }
            else
            {
                GameObject rootObject = new GameObject("Visual");
                rootObject.transform.SetParent(transform);
                rootObject.transform.localPosition = Vector3.zero;
                rootObject.transform.localRotation = Quaternion.identity;
                visualRoot = rootObject.transform;
            }
        }

        if (visualRoot.childCount > 0)
        {
            ApplyTintsToExisting();
            return;
        }

        GameObject bodyObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bodyObject.name = "Body";
        bodyObject.transform.SetParent(visualRoot);
        bodyObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        bodyObject.transform.localScale = new Vector3(1.6f, 0.5f, 3.2f);
        ApplyTint(bodyObject, bodyColor);

        GameObject cabinObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabinObject.name = "Cabin";
        cabinObject.transform.SetParent(visualRoot);
        cabinObject.transform.localPosition = new Vector3(0f, 0.75f, -0.25f);
        cabinObject.transform.localScale = new Vector3(1.1f, 0.45f, 1.4f);
        ApplyTint(cabinObject, cabinColor);

        CreateWheel("Wheel_FL", new Vector3(-0.7f, 0.15f, 1.1f));
        CreateWheel("Wheel_FR", new Vector3(0.7f, 0.15f, 1.1f));
        CreateWheel("Wheel_RL", new Vector3(-0.7f, 0.15f, -1.1f));
        CreateWheel("Wheel_RR", new Vector3(0.7f, 0.15f, -1.1f));

        RemoveColliderIfPresent(bodyObject);
        RemoveColliderIfPresent(cabinObject);
    }

    private void CreateWheel(string name, Vector3 localPosition)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = name;
        wheel.transform.SetParent(visualRoot);
        wheel.transform.localPosition = localPosition;
        wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        wheel.transform.localScale = new Vector3(0.55f, 0.2f, 0.55f);
        ApplyTint(wheel, wheelColor);
        RemoveColliderIfPresent(wheel);
    }

    private static void RemoveColliderIfPresent(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }
    }

    private static void ApplyTint(GameObject target, Color color)
    {
        if (target == null)
        {
            return;
        }

        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            return;
        }

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        renderer.SetPropertyBlock(block);
    }

    private void ApplyTintsToExisting()
    {
        Transform body = visualRoot.Find("Body");
        if (body != null)
        {
            ApplyTint(body.gameObject, bodyColor);
        }

        Transform cabin = visualRoot.Find("Cabin");
        if (cabin != null)
        {
            ApplyTint(cabin.gameObject, cabinColor);
        }

        ApplyTintIfExists("Wheel_FL");
        ApplyTintIfExists("Wheel_FR");
        ApplyTintIfExists("Wheel_RL");
        ApplyTintIfExists("Wheel_RR");
    }

    private void ApplyTintIfExists(string name)
    {
        Transform wheel = visualRoot.Find(name);
        if (wheel != null)
        {
            ApplyTint(wheel.gameObject, wheelColor);
        }
    }

}
