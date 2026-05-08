//
// DoorInteractable links a door GameObject to its associated DoorPortal data.
// The player reads this reference via Raycast to determine where the door leads.
//

using UnityEngine;

public class DoorInteractable : MonoBehaviour
{
    public DoorPortal portal;
}
