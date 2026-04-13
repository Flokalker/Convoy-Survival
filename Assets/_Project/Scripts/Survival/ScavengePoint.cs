using UnityEngine;

[DisallowMultipleComponent]
public class ScavengePoint : MonoBehaviour, IInteractable
{
    [SerializeField] private string rewardItemId = "truck_part";
    [SerializeField] private InventorySystem.ItemType rewardType = InventorySystem.ItemType.UpgradePart;
    [SerializeField, Min(1)] private int rewardAmount = 1;
    [SerializeField] private bool singleUse = true;
    [SerializeField] private string prompt = "Press E to scavenge";

    private bool used;

    public string InteractionPrompt => used ? "Empty" : prompt;

    public bool CanInteract(FirstPersonController player)
    {
        return !used && player != null;
    }

    public void Interact(FirstPersonController player)
    {
        if (!CanInteract(player))
        {
            return;
        }

        InventorySystem inventory = player.GetComponent<InventorySystem>();
        if (inventory != null)
        {
            inventory.AddItem(rewardItemId, rewardType, rewardAmount);
        }

        if (singleUse)
        {
            used = true;
        }
    }
}
