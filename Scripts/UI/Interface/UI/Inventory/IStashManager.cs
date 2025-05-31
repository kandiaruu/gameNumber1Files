using System.Collections.Generic;

public interface IStashManager
{
    StashData GetCurrentStash();
    void NextStash();
    void PrevStash();
    int GetCurrentStashIndex();
    List<StashData> GetAllStashes();
}
