using UnityEngine;
using System.Collections.Generic;

//
// Base class for all UI panels. Manages open/close state via SetActive,
// maintains a list of child panels that are closed alongside this panel,
// and provides virtual hooks for subclass-specific close logic.
//

public class BasePanel : MonoBehaviour, IPanel
{
    // The root GameObject representing this panel
    public GameObject PanelObject => gameObject;

    // Returns true if this panel's GameObject is currently active in the hierarchy
    public bool IsOpen => gameObject.activeInHierarchy;

    // Child panels that will be closed automatically when this panel closes
    public List<IPanel> Children { get; } = new List<IPanel>();

    // Activates the panel's GameObject
    public virtual void Open()
    {
        gameObject.SetActive(true);
    }

    // Runs the OnClose hook, deactivates the panel, and recursively closes all open children
    public virtual void Close()
    {
        OnClose();
        gameObject.SetActive(false);
        foreach (var child in Children)
        {
            if (child.IsOpen) child.Close();
        }
    }

    // Override in subclasses to perform custom cleanup logic before the panel is hidden
    protected virtual void OnClose()
    {
    }

    // Registers a child panel so it is closed when this panel closes
    public void AddChild(IPanel child)
    {
        if (child != null && !Children.Contains(child))
        {
            Children.Add(child);
        }
    }

    // Unregisters a previously added child panel
    public void RemoveChild(IPanel child)
    {
        if (child != null && Children.Contains(child))
        {
            Children.Remove(child);
        }
    }

    // Validates that this GameObject has an IPanel component attached
    public virtual void Awake()
    {
        if (gameObject.GetComponent<IPanel>() == null)
        {
            Debug.LogError($"{gameObject.name} does not have an IPanel component!");
        }
    }
}
