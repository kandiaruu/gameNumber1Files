using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    [SerializeField] private Transform weaponHoldPoint;
    [SerializeField] private GameObject swordPrefab;

    public string EquippedInstanceId { get; private set; }
    public int EquippedItemId { get; private set; } = -1;

    private GameObject currentWeaponObj;

    public bool IsEquipped(string instanceId)
    {
        return !string.IsNullOrEmpty(EquippedInstanceId) && EquippedInstanceId == instanceId;
    }

    public void Equip(string instanceId, int itemId)
    {
        Unequip();

        if (swordPrefab != null && weaponHoldPoint != null)
            currentWeaponObj = Instantiate(swordPrefab, weaponHoldPoint);

        EquippedInstanceId = instanceId;
        EquippedItemId = itemId;
    }

    public void Unequip()
    {
        if (currentWeaponObj != null)
            Destroy(currentWeaponObj);

        currentWeaponObj = null;
        EquippedInstanceId = null;
        EquippedItemId = -1;
    }
}