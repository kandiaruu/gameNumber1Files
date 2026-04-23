using System.Collections.Generic;
using UnityEngine;

public class StashManager : MonoBehaviour, IStashManager
{
    public List<StashData> stashes = new List<StashData>();
    public int currentStashIndex = 0;

    public StashData GetCurrentStash()
    {
        if (stashes.Count == 0) return null;
        return stashes[currentStashIndex];
    }

    public void NextStash()
    {
        if (stashes.Count == 0) return;
        currentStashIndex = (currentStashIndex + 1) % stashes.Count;
    }

    public void PrevStash()
    {
        if (stashes.Count == 0) return;
        currentStashIndex = (currentStashIndex - 1 + stashes.Count) % stashes.Count;
    }

    public int GetCurrentStashIndex()
    {
        return currentStashIndex;
    }

    public List<StashData> GetAllStashes()
    {
        return stashes;
    }
}