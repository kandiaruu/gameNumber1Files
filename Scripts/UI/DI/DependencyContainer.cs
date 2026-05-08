using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

//
// Static dependency injection container. Stores interface-to-implementation registrations,
// manages singleton instances, and performs property injection via [InjectAttribute1].
//

public class DependencyContainer1
{
    private static readonly Dictionary<Type, (Type ImplementationType, Lifecycle Lifecycle)> _registrations = new();
    private static readonly Dictionary<Type, object> _singletons = new();
    private static readonly Dictionary<Type, List<(string PropertyName, Action<object, object> Setter, Type DependencyType)>> _propertySetters = new();
    private static readonly Dictionary<Type, List<(string FieldName, Action<object, Dictionary<Type, object>> Setter, Type[] DependencyTypes)>> _dictionarySetters = new();

    // Registers an interface-to-implementation mapping with the specified lifecycle
    public static void Register<TInterface, TImplementation>(Lifecycle lifecycle) where TImplementation : TInterface
    {
        _registrations[typeof(TInterface)] = (typeof(TImplementation), lifecycle);
    }

    // Stores a pre-existing instance as the singleton for the given interface type
    public static void AddSingleton<TInterface>(object instance)
    {
        _singletons[typeof(TInterface)] = instance;
    }

    // Creates and attaches a MonoBehaviour component of the given type to the provided GameObject
    public static TInterface CreateMonoBehaviour<TInterface>(GameObject go) where TInterface : MonoBehaviour
    {
        return go.AddComponent<TInterface>();
    }

    // Resolves and injects all [InjectAttribute1]-marked properties and [InjectDictionaryAttribute1]-marked fields on the target object
    public static void InjectDependencies(object target)
    {
        Type type = target.GetType();

        if (!_propertySetters.TryGetValue(type, out var setters))
        {
            setters = new List<(string, Action<object, object>, Type)>();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => Attribute.IsDefined(p, typeof(InjectAttribute1)))
                .ToList();

            foreach (var prop in properties)
            {
                var setMethod = prop.GetSetMethod(true);
                if (setMethod == null) continue;

                Action<object, object> setter = (targetObj, value) => setMethod.Invoke(targetObj, new object[] { value });
                setters.Add((prop.Name, setter, prop.PropertyType));
            }
            _propertySetters[type] = setters;
        }

        foreach (var (name, setter, dependencyType) in setters)
        {
            object dependency = GetByType(dependencyType);
            if (dependency == null)
            {
                Debug.LogError($"Failed to resolve dependency for property {name} in {type.Name}.");
                continue;
            }
            setter(target, dependency);
        }

        if (!_dictionarySetters.TryGetValue(type, out var dictSetters))
        {
            dictSetters = new List<(string, Action<object, Dictionary<Type, object>>, Type[])>();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => Attribute.IsDefined(f, typeof(InjectDictionaryAttribute1)))
                .ToList();

            foreach (var field in fields)
            {
                if (field.FieldType != typeof(Dictionary<Type, object>)) continue;

                var dependencyTypes = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(p => Attribute.IsDefined(p, typeof(InjectAttribute1)))
                    .Select(p => p.PropertyType)
                    .ToArray();

                Action<object, Dictionary<Type, object>> setter = (targetObj, dict) => field.SetValue(targetObj, dict);
                dictSetters.Add((field.Name, setter, dependencyTypes));
            }
            _dictionarySetters[type] = dictSetters;
        }

        foreach (var (name, setter, dependencyTypes) in dictSetters)
        {
            var dict = new Dictionary<Type, object>();
            foreach (var depType in dependencyTypes)
            {
                object dependency = GetByType(depType);
                if (dependency == null)
                {
                    Debug.LogError($"Failed to resolve dependency {depType.Name} for dictionary {name} in {type.Name}.");
                    continue;
                }
                dict[depType] = dependency;
            }
            setter(target, dict);
        }
    }

    // Resolves an instance for the given type, returning a cached singleton or creating a new instance
    private static object GetByType(Type type)
    {
        if (_singletons.TryGetValue(type, out var singleton))
        {
            return singleton;
        }

        if (!_registrations.TryGetValue(type, out var registration))
        {
            throw new Exception($"No registration for {type}");
        }

        var (implType, lifecycle) = registration;
        object instance;

        if (typeof(MonoBehaviour).IsAssignableFrom(implType))
        {
            instance = new GameObject(implType.Name).AddComponent(implType);
        }
        else
        {
            instance = Activator.CreateInstance(implType);
        }

        if (lifecycle == Lifecycle.Singleton)
        {
            _singletons[type] = instance;
        }

        InjectDependencies(instance);
        return instance;
    }

    // Removes all cached singleton instances from the container
    public static void ClearSingletons()
    {
        _singletons.Clear();
    }

    // Placeholder for clearing scoped instances (not yet implemented)
    public static void ClearScoped()
    {
    }
}

// Marks a property for automatic dependency injection by the container
[AttributeUsage(AttributeTargets.Property)]
public class InjectAttribute1 : Attribute { }

// Marks a Dictionary<Type, object> field to be populated with all injected dependencies
[AttributeUsage(AttributeTargets.Field)]
public class InjectDictionaryAttribute1 : Attribute { }
