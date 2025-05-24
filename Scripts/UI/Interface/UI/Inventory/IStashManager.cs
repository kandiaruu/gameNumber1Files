public interface IStashManager
{
    StashData GetCurrentStash();
    void NextStash();
    void PrevStash();
    int GetCurrentStashIndex();
}
