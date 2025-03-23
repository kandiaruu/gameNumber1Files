using System;
using System.Collections.Generic;
using UnityEngine;

public class DependencyContainer : MonoBehaviour
{
    private static DependencyContainer instance;
    public static DependencyContainer Instance => instance;

    private readonly Dictionary<Type, object> dependencies = new Dictionary<Type, object>();

    [SerializeField] private GameObject uiManagerPrefab;
    [SerializeField] private GameObject skillTreeManagerPrefab;
    [SerializeField] private GameObject notificationManagerPrefab;
    [SerializeField] private GameObject skillTreeNavigationPrefab;
    [SerializeField] private GameObject tooltipManagerPrefab;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            UnityEngine.Object.Destroy(gameObject);
            return;
        }
        instance = this;
        // Изменение: отсоединяем объект от родителя, чтобы сделать его корневым
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        UnityEngine.Object.DontDestroyOnLoad(gameObject);

        InitializeDependencies();
    }

    private void InitializeDependencies()
    {
        RegisterExisting<UIManager>();
        RegisterExisting<SkillTreeManager>();
        RegisterExisting<NotificationManager>();
        RegisterExisting<SkillTreeNavigation>();
        RegisterExisting<TooltipManager>(); // Возвращаем регистрацию TooltipManager

        // Возвращаем регистрацию ISkillTreeManager
        if (dependencies.TryGetValue(typeof(SkillTreeManager), out object skillTreeManager))
        {
            Register<ISkillTreeManager>(skillTreeManager as ISkillTreeManager);
        }

        if (!dependencies.ContainsKey(typeof(UIManager)))
            Register(InstantiatePrefab<UIManager>(uiManagerPrefab));
        if (!dependencies.ContainsKey(typeof(SkillTreeManager)))
            Register(InstantiatePrefab<SkillTreeManager>(skillTreeManagerPrefab));
        if (!dependencies.ContainsKey(typeof(NotificationManager)))
            Register(InstantiatePrefab<NotificationManager>(notificationManagerPrefab));
        if (!dependencies.ContainsKey(typeof(SkillTreeNavigation)))
            Register(InstantiatePrefab<SkillTreeNavigation>(skillTreeNavigationPrefab));
        if (!dependencies.ContainsKey(typeof(TooltipManager))) // Возвращаем создание из префаба
            Register(InstantiatePrefab<TooltipManager>(tooltipManagerPrefab));
    }

    private void RegisterExisting<T>() where T : MonoBehaviour
    {
        T instance = UnityEngine.Object.FindAnyObjectByType<T>();
        if (instance != null)
        {
            Register(instance);
        }
        else
        {
            Debug.LogWarning($"No existing {typeof(T).Name} found in scene.");
        }
    }

    public void Register<T>(T instance)
    {
        Type type = typeof(T);
        if (instance == null)
        {
            Debug.LogError($"Cannot register null instance for {type}");
            return;
        }
        if (dependencies.ContainsKey(type))
        {
            Debug.LogWarning($"Dependency of type {type} is already registered. Overwriting.");
        }
        dependencies[type] = instance;
    }

    public T Resolve<T>()
    {
        Type type = typeof(T);
        if (dependencies.TryGetValue(type, out object instance))
        {
            return (T)instance;
        }
        Debug.LogError($"No dependency registered for type {type}");
        return default;
    }

    private T InstantiatePrefab<T>(GameObject prefab) where T : MonoBehaviour
    {
        if (prefab == null)
        {
            Debug.LogError($"Prefab for {typeof(T).Name} is not assigned!");
            return null;
        }
        GameObject instance = UnityEngine.Object.Instantiate(prefab);
        T component = instance.GetComponent<T>();
        if (component == null)
        {
            Debug.LogError($"Prefab {prefab.name} does not contain {typeof(T).Name} component!");
            UnityEngine.Object.Destroy(instance);
            return null;
        }
        return component;
    }

    public void RegisterManual<T>(T instance) => Register(instance);
}