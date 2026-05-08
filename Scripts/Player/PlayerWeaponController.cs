//
// PlayerWeaponController manages the weapon the player currently has equipped.
// It tracks the equipped item by instance ID and item ID, instantiates the weapon
// prefab at a designated hold point when equipping, and destroys it when unequipping.
//

using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    [SerializeField] private Transform weaponHoldPoint;
    [SerializeField] private GameObject swordPrefab;

    public string EquippedInstanceId { get; private set; }
    public int EquippedItemId { get; private set; } = -1;

    private GameObject currentWeaponObj;

    // Returns true if the given instance ID matches the currently equipped weapon
    public bool IsEquipped(string instanceId)
    {
        return !string.IsNullOrEmpty(EquippedInstanceId) && EquippedInstanceId == instanceId;
    }

    // Unequips any current weapon, then instantiates the sword prefab at the hold point and records the new item's IDs
    public void Equip(string instanceId, int itemId)
    {
        Unequip();

        if (swordPrefab != null && weaponHoldPoint != null)
            currentWeaponObj = Instantiate(swordPrefab, weaponHoldPoint);

        EquippedInstanceId = instanceId;
        EquippedItemId = itemId;
    }

    // Destroys the current weapon GameObject and resets the equipped ID fields
    public void Unequip()
    {
        if (currentWeaponObj != null)
            Destroy(currentWeaponObj);

        currentWeaponObj = null;
        EquippedInstanceId = null;
        EquippedItemId = -1;
    }
}
