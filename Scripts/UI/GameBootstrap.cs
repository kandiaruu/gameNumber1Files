using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

public class GameBootstrap : MonoBehaviour
{
    [Dependency(new[] { typeof(ISkillTreeManager) }, typeof(SkillLogicManager), Lifecycle.Singleton, typeof(ISkillUIManager))]
    [SerializeField] private SkillLogicManager skillLogicManager;

    [Dependency(new[] { typeof(ISkillUIManager) }, typeof(SkillUIManager), Lifecycle.Singleton, typeof(ISkillTreeManager))]
    [SerializeField] private SkillUIManager skillUIManager;

    [Dependency(new[] { typeof(ISkillInputHandler) }, typeof(SkillInputHandler), Lifecycle.Singleton, typeof(ISkillTreeManager), typeof(ISkillNotificationHandler), typeof(ITooltipManager), typeof(ISkillTreeNavigation))]
    [SerializeField] private SkillInputHandler skillInputHandler;

    [Dependency(new[] { typeof(ISkillTreeNavigation) }, typeof(SkillTreeNavigation), Lifecycle.Singleton, typeof(ISkillNotificationHandler))]
    [SerializeField] private SkillTreeNavigation skillTreeNavigation;

    [Dependency(new[] { typeof(ISkillNotificationHandler) }, typeof(SkillNotificationHandler), Lifecycle.Singleton, typeof(INotificationManager))]
    [SerializeField] private SkillNotificationHandler skillNotificationHandler;

    [Dependency(new[] { typeof(ITooltipManager) }, typeof(TooltipManager), Lifecycle.Singleton, typeof(ISkillTooltipPanel), typeof(IUIManager))]
    [SerializeField] private TooltipManager tooltipManager;

    [Dependency(new[] { typeof(INotificationManager) }, typeof(NotificationManager), Lifecycle.Singleton, typeof(ISkillTreeManager), typeof(ISkillNotificationPanel), typeof(IUIManager))]
    [SerializeField] private NotificationManager notificationManager;

    [Dependency(new[] { typeof(IUIManager) }, typeof(UIManager), Lifecycle.Singleton, typeof(ISkillTreeNavigation), typeof(ISkillTreeManager), typeof(ISkillUIManager), typeof(INotificationManager))]
    [SerializeField] private UIManager uiManager;

    [Dependency(new[] { typeof(ISkillNotificationPanel) }, typeof(SkillNotificationPanel), Lifecycle.Singleton, typeof(ISkillTreeManager), typeof(ISkillUIManager))]
    [SerializeField] private SkillNotificationPanel skillNotificationPanel;

    [Dependency(new[] { typeof(ISkillTooltipPanel) }, typeof(SkillTooltipPanel), Lifecycle.Singleton)]
    [SerializeField] private SkillTooltipPanel skillTooltipPanel;

    private static FieldInfo[] _dependencyFields;
    private static FieldInfo[] _allFields;
    private static readonly Dictionary<string, Delegate> _methodCache = new Dictionary<string, Delegate>();
    private static readonly HashSet<Type> _registeredTypes = new HashSet<Type>();

    void Awake()
    {
        Debug.Log("GameBootstrap Awake started");

        if (_dependencyFields == null)
        {
            _dependencyFields = GetDependencyFields();
            _allFields = GetAllFields();
        }

        RegisterAllDependencies();
        SetupDependencies();
        InjectDependencies();

        Debug.Log("GameBootstrap Awake finished");
    }

    private void RegisterAllDependencies()
    {
        _registeredTypes.Clear();
        var typesToRegister = new List<(Type InterfaceType, Type ImplementationType, Lifecycle Lifecycle)>();

        foreach (var field in _dependencyFields)
        {
            var attr = field.GetCustomAttribute<DependencyAttribute>();
            foreach (var interfaceType in attr.InterfaceTypes)
            {
                typesToRegister.Add((interfaceType, attr.ImplementationType, attr.Lifecycle));
            }

            foreach (var depType in attr.Dependencies)
            {
                var implField = _allFields.FirstOrDefault(f => f.FieldType == depType || f.FieldType.GetInterfaces().Contains(depType));
                if (implField != null)
                {
                    var implType = implField.FieldType;
                    typesToRegister.Add((depType, implType, Lifecycle.Singleton));
                }
                else
                {
                    string implName = depType.Name.StartsWith("I") ? depType.Name.Substring(1) : depType.Name;
                    Type implType = depType.Assembly.GetType(depType.Namespace + "." + implName);
                    if (implType != null && depType.IsAssignableFrom(implType))
                    {
                        typesToRegister.Add((depType, implType, Lifecycle.Singleton));
                    }
                    else
                    {
                        Debug.LogError($"No implementation found for {depType.Name}. Please add a [SerializeField] field for it in GameBootstrap.");
                    }
                }
            }
        }

        foreach (var (interfaceType, implType, lifecycle) in typesToRegister)
        {
            if (_registeredTypes.Add(interfaceType))
            {
                Debug.Log($"Registering {interfaceType.Name} -> {implType.Name} as {lifecycle}");
                if (!_methodCache.TryGetValue($"Register_{interfaceType.Name}_{implType.Name}", out var registerDelegate))
                {
                    var registerMethod = typeof(DependencyContainer1)
                        .GetMethod(nameof(DependencyContainer1.Register))
                        .MakeGenericMethod(interfaceType, implType);
                    registerDelegate = Delegate.CreateDelegate(typeof(Action<Lifecycle>), null, registerMethod);
                    _methodCache[$"Register_{interfaceType.Name}_{implType.Name}"] = registerDelegate;
                }
                ((Action<Lifecycle>)registerDelegate)(lifecycle);
            }
        }
    }

    private void SetupDependencies()
    {
        foreach (var field in _dependencyFields)
        {
            var attr = field.GetCustomAttribute<DependencyAttribute>();
            var instance = field.GetValue(this) as MonoBehaviour;
            if (instance == null)
            {
                Debug.Log($"{field.Name} not assigned, creating new instance.");
                var go = new GameObject(field.Name);

                if (!_methodCache.TryGetValue($"CreateMonoBehaviour_{attr.ImplementationType.Name}", out var createDelegate))
                {
                    var createMethod = typeof(DependencyContainer1)
                        .GetMethod(nameof(DependencyContainer1.CreateMonoBehaviour))
                        .MakeGenericMethod(attr.ImplementationType);
                    createDelegate = Delegate.CreateDelegate(typeof(Func<GameObject, MonoBehaviour>), null, createMethod);
                    _methodCache[$"CreateMonoBehaviour_{attr.ImplementationType.Name}"] = createDelegate;
                }

                instance = ((Func<GameObject, MonoBehaviour>)createDelegate)(go);
                field.SetValue(this, instance);
                Debug.Log($"{field.Name} created: {instance != null}");
            }

            foreach (var interfaceType in attr.InterfaceTypes)
            {
                Debug.Log($"{field.Name} found, adding to singletons as {interfaceType.Name}.");
                if (!_methodCache.TryGetValue($"AddSingleton_{interfaceType.Name}", out var addSingletonDelegate))
                {
                    var addSingletonMethod = typeof(DependencyContainer1)
                        .GetMethod(nameof(DependencyContainer1.AddSingleton))
                        .MakeGenericMethod(interfaceType);
                    addSingletonDelegate = Delegate.CreateDelegate(typeof(Action<object>), null, addSingletonMethod);
                    _methodCache[$"AddSingleton_{interfaceType.Name}"] = addSingletonDelegate;
                }
                ((Action<object>)addSingletonDelegate)(instance);
            }
        }

        foreach (var field in _allFields)
        {
            var instance = field.GetValue(this) as MonoBehaviour;
            if (instance != null && !_dependencyFields.Any(f => f.Name == field.Name))
            {
                var interfaceType = instance.GetType().GetInterfaces().FirstOrDefault(i => _registeredTypes.Contains(i));
                if (interfaceType != null)
                {
                    Debug.Log($"{field.Name} found, adding to singletons as {interfaceType.Name}.");
                    if (!_methodCache.TryGetValue($"AddSingleton_{interfaceType.Name}", out var addSingletonDelegate))
                    {
                        var addSingletonMethod = typeof(DependencyContainer1)
                            .GetMethod(nameof(DependencyContainer1.AddSingleton))
                            .MakeGenericMethod(interfaceType);
                        addSingletonDelegate = Delegate.CreateDelegate(typeof(Action<object>), null, addSingletonMethod);
                        _methodCache[$"AddSingleton_{interfaceType.Name}"] = addSingletonDelegate;
                    }
                    ((Action<object>)addSingletonDelegate)(instance);
                }
            }
        }
    }

    private void InjectDependencies()
    {
        var allInstances = new HashSet<MonoBehaviour>();
        foreach (var field in _dependencyFields)
        {
            var instance = field.GetValue(this) as MonoBehaviour;
            if (instance != null)
            {
                allInstances.Add(instance);
            }
        }
        foreach (var field in _allFields)
        {
            var instance = field.GetValue(this) as MonoBehaviour;
            if (instance != null)
            {
                allInstances.Add(instance);
            }
        }

        foreach (var instance in allInstances)
        {
            Debug.Log($"Injecting dependencies into {instance.GetType().Name}");
            DependencyContainer1.InjectDependencies(instance);

            if (!_methodCache.TryGetValue($"EnsureDependencies_{instance.GetType().Name}", out var ensureDelegate))
            {
                var ensureMethod = instance.GetType().GetMethod("EnsureDependencies");
                if (ensureMethod != null)
                {
                    ensureDelegate = Delegate.CreateDelegate(typeof(Action), instance, ensureMethod);
                    _methodCache[$"EnsureDependencies_{instance.GetType().Name}"] = ensureDelegate;
                }
            }
            ((Action)ensureDelegate)?.Invoke();
        }
    }

    private FieldInfo[] GetDependencyFields()
    {
        return GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(f => f.GetCustomAttribute<DependencyAttribute>() != null)
            .ToArray();
    }

    private FieldInfo[] GetAllFields()
    {
        return GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(f => f.GetCustomAttribute<SerializeField>() != null)
            .ToArray();
    }

    void OnDestroy()
    {
        DependencyContainer1.ClearSingletons();
        DependencyContainer1.ClearScoped();
    }
}