using UnityEngine;
using System.Linq;

public enum PanelType
{
    SkillTree,
    Notification,
    Tooltip,
    Inventory, // Пример для будущих панелей
    Settings
}

[System.Serializable]
public class PanelConfig
{
    public PanelType panelType;
    public GameObject panelObject;
}

public class PanelFactory : MonoBehaviour
{
    [SerializeField] private PanelConfig[] panelConfigs;

    public T GetPanel<T>(PanelType panelType) where T : class
    {
        var config = FindPanelConfigByType(panelConfigs, panelType);
        if (config != null && config.panelObject != null)
        {
            var component = config.panelObject.GetComponent<T>();
            if (component != null)
            {
                if (!config.panelObject.activeSelf)
                {
                    config.panelObject.SetActive(true);
                    Debug.Log($"PanelFactory: Активирован объект для {panelType}");
                }
                return component;
            }
            else
            {
                Debug.LogError($"{typeof(T).Name} не найден на объекте для {panelType}!");
                return null;
            }
        }
        else
        {
            Debug.LogError($"Конфигурация для панели {panelType} не найдена!");
            return null;
        }
    }

    private PanelConfig FindPanelConfigByType(PanelConfig[] configs, PanelType type)
    {
        return configs.FirstOrDefault(c => c.panelType == type);
    }
}