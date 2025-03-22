using UnityEngine;
using System.Collections.Generic;

public class BasePanel : MonoBehaviour, IPanel
{
    public GameObject PanelObject => gameObject;
    public bool IsOpen => gameObject.activeInHierarchy;
    public List<IPanel> Children { get; } = new List<IPanel>();

    public virtual void Open()
    {
        //Debug.Log($"BasePanel.Open() вызван для {gameObject.name}, gameObject.activeSelf={gameObject.activeSelf}");
        gameObject.SetActive(true);
        //Debug.Log($"После BasePanel.Open(), gameObject.activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}");
    }

    public virtual void Close()
    {
        OnClose();
        gameObject.SetActive(false);
        foreach (var child in Children)
        {
            if (child.IsOpen) child.Close();
        }
    }

    protected virtual void OnClose()
    {
        // Базовая реализация пуста, наследники могут переопределить
    }

    public void AddChild(IPanel child)
    {
        if (child != null && !Children.Contains(child))
        {
            Children.Add(child);
        }
    }

    public void RemoveChild(IPanel child)
    {
        if (child != null && Children.Contains(child))
        {
            Children.Remove(child);
        }
    }

    protected virtual void Awake()
    {
        if (gameObject.GetComponent<IPanel>() == null)
        {
            Debug.LogError($"{gameObject.name} не имеет компонента IPanel!");
        }
    }
}