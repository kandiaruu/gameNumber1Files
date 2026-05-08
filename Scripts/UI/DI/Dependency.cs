using System;

//
// Defines the [Dependency] attribute used to declare DI registrations on GameBootstrap fields,
// and the Lifecycle enum controlling instance reuse behavior.
//

// Declares a field as a dependency registration, specifying interface types, implementation, lifecycle, and transitive dependencies
[AttributeUsage(AttributeTargets.Field)]
public class DependencyAttribute : Attribute
{
    public Type[] InterfaceTypes { get; }
    public Type ImplementationType { get; }
    public Lifecycle Lifecycle { get; }
    public Type[] Dependencies { get; }

    // Validates that the implementation type satisfies all declared interfaces and stores the registration metadata
    public DependencyAttribute(Type[] interfaceTypes, Type implementationType, Lifecycle lifecycle, params Type[] dependencies)
    {
        InterfaceTypes = interfaceTypes ?? throw new ArgumentNullException(nameof(interfaceTypes));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        Lifecycle = lifecycle;
        Dependencies = dependencies ?? new Type[0];

        foreach (var interfaceType in InterfaceTypes)
        {
            if (!interfaceType.IsAssignableFrom(implementationType))
            {
                throw new ArgumentException($"{implementationType.Name} does not implement {interfaceType.Name}");
            }
        }
    }
}

// Controls how long a resolved instance lives within the container
public enum Lifecycle
{
    Singleton,
    Transient,
    Scoped
}
