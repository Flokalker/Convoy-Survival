using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
public class RendererTint : MonoBehaviour
{
    [SerializeField] private Color tint = new Color(0.55f, 0.55f, 0.55f, 1f);

    private void OnEnable()
    {
        ApplyTint();
    }

    private void OnValidate()
    {
        ApplyTint();
    }

    private void ApplyTint()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            return;
        }

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", tint);
        block.SetColor("_Color", tint);
        renderer.SetPropertyBlock(block);

        if (renderer.sharedMaterial != null)
        {
            Material materialInstance = Application.isPlaying
                ? renderer.material
                : new Material(renderer.sharedMaterial);
            materialInstance.SetColor("_BaseColor", tint);
            materialInstance.SetColor("_Color", tint);
            renderer.sharedMaterial = materialInstance;
        }
    }
}
