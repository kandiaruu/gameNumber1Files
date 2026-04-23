using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItemData
{
    public int itemId;
    public int stackSize;
    public Vector2Int topLeftPosition; // Левый верхний угол
    public Vector2Int size;            // Размер предмета (ширина, высота)
    public List<Vector2Int> occupiedPositions; // Все занятые координаты
}