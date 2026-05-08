using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

//
// Entry point for the game's dependency injection system.
// Declares all MonoBehaviour dependencies via [Dependency] attributes,
// then registers, instantiates, and injects them through DependencyContainer1 on Awake.
//

public class GameBootstrap : MonoBehaviour
{
    [Dependency(new[] { typeof(ISkillTreeManager) }, typeof(SkillLogicManager), Lifecycle.Singleton, typeof(ISkillUIManager))]
    [SerializeField] private SkillLogicManager skillLogicManager;

    [Dependency(new[] { typeof(ISkillUIManager) }, typeof(SkillUIManager), Lifecycle.Singleton, typeof(ISkillTreeManager))]
    [SerializeField] private SkillUIManager skillUIManager;

    [Dependency(new[] { typeof(ISkillInputHandler) }, typeof(SkillInputHandler), Lifecycle.Singleton, typeof(ISkillTreeManager), typeof(ISkillNotificationHandler), typeof(ITooltipManager), typeof(ISkillTreeNavigation), typeof(ISkillGuiManager))]
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

    [Dependency(new[] { typeof(ISkillPanelManager) }, typeof(SkillPanelManager), Lifecycle.Singleton, typeof(ISkillTreeNavigation), typeof(ISkillPanelUI))]
    [SerializeField] private SkillPanelManager skillPanelManager;

    [Dependency(new[] { typeof(ISkillPanelUI) }, typeof(SkillPanelUI), Lifecycle.Singleton, typeof(ISkillPanelManager), typeof(IUIManager))]
    [SerializeField] private SkillPanelUI skillPanelUI;

    [Dependency(new[] { typeof(ISkillTree) }, typeof(SkillTree), Lifecycle.Singleton, typeof(ISkillPanelManager))]
    [SerializeField] private SkillTree skillTree;

    [Dependency(new[] { typeof(ISkillGuiManager) }, typeof(SkillGuiManager), Lifecycle.Singleton, typeof(ISkillTreeManager), typeof(ISkillGuiPanel), typeof(IUIManager))]
    [SerializeField] private SkillGuiManager skillGuiManager;

    [Dependency(new[] { typeof(ISkillGuiPanel) }, typeof(SkillGuiPanel), Lifecycle.Singleton)]
    [SerializeField] private SkillGuiPanel skillGuiPanel;

    [Dependency(new[] { typeof(IThirdPersonCharacter) }, typeof(ThirdPersonCharacter), Lifecycle.Singleton, typeof(IUIManager))]
    [SerializeField] private ThirdPersonCharacter thirdPersonCharacter;

    [Dependency(new[] { typeof(IPlayerStats) }, typeof(PlayerStats), Lifecycle.Singleton)]
    [SerializeField] private PlayerStats playerStats;

    [Dependency(new[] { typeof(IInventoryPanel3) }, typeof(InventoryPanel3), Lifecycle.Singleton)]
    [SerializeField] private InventoryPanel3 inventoryPanel3;

    [Dependency(new[] { typeof(IInventorySearch3) }, typeof(InventorySearch3), Lifecycle.Singleton)]
    [SerializeField] private InventorySearch3 inventorySearch3;

    [Dependency(new[] { typeof(ILootManager3) }, typeof(LootManager3), Lifecycle.Singleton)]
    [SerializeField] private LootManager3 lootManager3;

    [Dependency(new[] { typeof(IPanel) }, typeof(LootPanel3), Lifecycle.Singleton)]
    [SerializeField] private LootPanel3 lootPanel3;

    [Dependency(new[] { typeof(ILootInventoryPanel3) }, typeof(InventoryPanel3), Lifecycle.Singleton)]
    [SerializeField] private InventoryPanel3 inventoryLootPanel3;

    [Dependency(new[] { typeof(IEnemySpawner) }, typeof(EnemySpawner), Lifecycle.Singleton, typeof(IPlayerStats))]
    [SerializeField] private EnemySpawner enemySpawner;

    [Dependency(new[] { typeof(DungeonVisibilityManager) }, typeof(DungeonVisibilityManager), Lifecycle.Singleton, typeof(IThirdPersonCharacter), typeof(IEnemySpawner))]
    [SerializeField] private DungeonVisibilityManager visibilityManager;

    [Dependency(new[] { typeof(IDungeonFloorManager) }, typeof(DungeonFloorManager), Lifecycle.Singleton)]
    [SerializeField] private DungeonFloorManager dungeonFloorManager;

    [Dependency(new[] { typeof(IGameplaySkillManager) }, typeof(GameplaySkillManager), Lifecycle.Singleton)]
    [SerializeField] private GameplaySkillManager gameplaySkillManager;

    [Dependency(new[] { typeof(ISkillEquipManager) }, typeof(SkillEquipManager), Lifecycle.Singleton)]
    [SerializeField] private SkillEquipManager skillEquipManager;

    private static FieldInfo[] _dependencyFields;
    private static FieldInfo[] _allFields;
    private static readonly Dictionary<string, Delegate> _methodCache = new Dictionary<string, Delegate>();
    private static readonly HashSet<Type> _registeredTypes = new HashSet<Type>();

    // Bootstraps the entire DI system: registers types, sets up instances, and injects all dependencies
    void Awake()
    {
        if (_dependencyFields == null)
        {
            _dependencyFields = GetDependencyFields();
            _allFields = GetAllFields();
        }

        RegisterAllDependencies();
        SetupDependencies();
        InjectDependencies();
    }

    // Reads all [Dependency] attributes and registers each interface-to-implementation mapping in the container
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

    // Creates any missing MonoBehaviour instances and registers all serialized instances as singletons
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

    // Runs property injection on every registered instance and calls EnsureDependencies if it exists
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

    // Returns all fields on this class that have a [Dependency] attribute
    private FieldInfo[] GetDependencyFields()
    {
        return GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(f => f.GetCustomAttribute<DependencyAttribute>() != null)
            .ToArray();
    }

    // Returns all fields on this class that have a [SerializeField] attribute
    private FieldInfo[] GetAllFields()
    {
        return GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(f => f.GetCustomAttribute<SerializeField>() != null)
            .ToArray();
    }

    // Clears all singleton and scoped registrations when the bootstrap object is destroyed
    void OnDestroy()
    {
        DependencyContainer1.ClearSingletons();
        DependencyContainer1.ClearScoped();
    }
}
