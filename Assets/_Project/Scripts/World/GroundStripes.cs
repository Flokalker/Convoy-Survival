using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public class GroundStripes : MonoBehaviour
{
    [SerializeField] private int stripeCount = 12;
    [SerializeField, Min(0.1f)] private float stripeLength = 3.5f;
    [SerializeField, Min(0.1f)] private float stripeWidth = 0.35f;
    [SerializeField, Min(0.1f)] private float stripeGap = 2.2f;
    [SerializeField, Min(0.01f)] private float stripeHeightOffset = 0.02f;
    [SerializeField] private Color stripeColor = new Color(0.9f, 0.9f, 0.9f, 1f);

    private const string StripeRootName = "Stripes";

    private void OnEnable()
    {
        BuildStripes();
    }

    private void OnValidate()
    {
        BuildStripes();
    }

    private void BuildStripes()
    {
        Transform stripeRoot = GetOrCreateStripeRoot();
        while (stripeRoot.childCount > 0)
        {
            Transform child = stripeRoot.GetChild(0);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        float totalSpacing = stripeLength + stripeGap;
        float startOffset = -((stripeCount - 1) * totalSpacing) * 0.5f;

        for (int i = 0; i < stripeCount; i++)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = $"Stripe_{i + 1:00}";
            stripe.transform.SetParent(stripeRoot, false);
            stripe.transform.localScale = new Vector3(stripeWidth, 0.02f, stripeLength);
            stripe.transform.localPosition = new Vector3(0f, stripeHeightOffset, startOffset + i * totalSpacing);
            ApplyTint(stripe, stripeColor);
            RemoveColliderIfPresent(stripe);
        }
    }

    private Transform GetOrCreateStripeRoot()
    {
        Transform stripeRoot = transform.Find(StripeRootName);
        if (stripeRoot != null)
        {
            return stripeRoot;
        }

        GameObject root = new GameObject(StripeRootName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        return root.transform;
    }

    private static void ApplyTint(GameObject target, Color color)
    {
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

        if (renderer.sharedMaterial != null)
        {
            Material materialInstance = Application.isPlaying
                ? renderer.material
                : new Material(renderer.sharedMaterial);
            materialInstance.SetColor("_BaseColor", color);
            materialInstance.SetColor("_Color", color);
            renderer.sharedMaterial = materialInstance;
        }
    }

    private static void RemoveColliderIfPresent(GameObject target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

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
