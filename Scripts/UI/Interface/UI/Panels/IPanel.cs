using UnityEngine;
using System.Collections.Generic; // Добавляем эту строку

public interface IPanel
{
    void Open();
    void Close();
    bool IsOpen { get; }
    GameObject PanelObject { get; }
    List<IPanel> Children { get; } // Список дочерних панелей
    void AddChild(IPanel child);  // Добавление дочернего элемента
    void RemoveChild(IPanel child); // Удаление дочернего элемента
}