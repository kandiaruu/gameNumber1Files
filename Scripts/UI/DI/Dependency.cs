using System;

[AttributeUsage(AttributeTargets.Field)]
public class DependencyAttribute : Attribute
{
    public Type[] InterfaceTypes { get; }
    public Type ImplementationType { get; }
    public Lifecycle Lifecycle { get; }
    public Type[] Dependencies { get; }

    public DependencyAttribute(Type[] interfaceTypes, Type implementationType, Lifecycle lifecycle, params Type[] dependencies)
    {
        InterfaceTypes = interfaceTypes ?? throw new ArgumentNullException(nameof(interfaceTypes));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        Lifecycle = lifecycle;
        Dependencies = dependencies ?? new Type[0];

        // Проверка, что реализация соответствует всем интерфейсам
        foreach (var interfaceType in InterfaceTypes)
        {
            if (!interfaceType.IsAssignableFrom(implementationType))
            {
                throw new ArgumentException($"{implementationType.Name} does not implement {interfaceType.Name}");
            }
        }
    }
}

public enum Lifecycle
{
    Singleton,
    Transient,
    Scoped
}