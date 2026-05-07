public interface IDungeonFloorManager
{
    bool IsInsideDungeon { get; }
    bool HasActiveDungeon { get; }
    void StartNewDungeon(int maxFloors);
    void ResumeDungeon();
    void ExitDungeonToWorld();
    void ExitAndDeleteDungeon();
    void DeleteDungeonFromWorld();
    void RespawnPlayerInWorld();
}