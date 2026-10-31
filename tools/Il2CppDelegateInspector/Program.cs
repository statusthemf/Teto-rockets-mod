
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

if (args.Length < 2)
{
    Console.WriteLine(
        "Usage: Inspector <Il2CppInterop.Runtime.dll> <UnityEngine.CoreModule.dll>");
    return 1;
}

var directory = Path.GetDirectoryName(
    Path.GetFullPath(args[0]))!;

AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = Path.Combine(directory, name.Name + ".dll");

    return File.Exists(path)
        ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path)
        : null;
};

try
{
    var interop = AssemblyLoadContext.Default.LoadFromAssemblyPath(
        Path.GetFullPath(args[0]));

    var core = AssemblyLoadContext.Default.LoadFromAssemblyPath(
        Path.GetFullPath(args[1]));

    Console.WriteLine("=== DelegateSupport ===");

    var support = interop.GetType(
        "Il2CppInterop.Runtime.DelegateSupport",
        throwOnError: true)!;

    foreach (var method in support.GetMethods(
        BindingFlags.Public |
        BindingFlags.Static |
        BindingFlags.DeclaredOnly))
    {
        Console.WriteLine(method);
    }

    Console.WriteLine();
    Console.WriteLine("=== Loading Unity types ===");

    Type[] loadedTypes;

    try
    {
        loadedTypes = core.GetTypes();
    }
    catch (ReflectionTypeLoadException ex)
    {
        Console.WriteLine("Some Unity types could not be loaded:");

        foreach (var error in ex.LoaderExceptions)
        {
            if (error != null)
                Console.WriteLine(error.Message);
        }

        loadedTypes = ex.Types
            .Where(t => t != null)
            .Cast<Type>()
            .ToArray();
    }

    Console.WriteLine(
        $"Successfully loaded {loadedTypes.Length} types.");

    Console.WriteLine();
    Console.WriteLine("=== SceneManager types ===");

    var matches = loadedTypes
        .Where(t =>
            t.FullName != null &&
            t.FullName.Contains(
                "SceneManager",
                StringComparison.OrdinalIgnoreCase))
        .ToArray();

    if (matches.Length == 0)
        Console.WriteLine("No SceneManager types found.");

    foreach (var type in matches)
    {
        Console.WriteLine();
        Console.WriteLine("TYPE: " + type.FullName);

        Console.WriteLine("-- Events --");

        foreach (var e in type.GetEvents(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Static |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly))
        {
            Console.WriteLine("Event: " + e);
            Console.WriteLine("Handler: " + e.EventHandlerType);
            Console.WriteLine("Add method: " + e.GetAddMethod(true));

            var invoke = e.EventHandlerType?.GetMethod("Invoke");

            if (invoke != null)
            {
                Console.WriteLine("Delegate Invoke: " + invoke);

                foreach (var parameter in invoke.GetParameters())
                {
                    Console.WriteLine(
                        $"  Parameter: {parameter.ParameterType} {parameter.Name}");
                }
            }
        }

        Console.WriteLine("-- Relevant methods --");

        foreach (var method in type.GetMethods(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Static |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly)
            .Where(m =>
                m.Name.Contains(
                    "sceneLoaded",
                    StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains(
                    "SceneLoaded",
                    StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(method);
        }

        Console.WriteLine("-- SceneLoaded accessor signatures --");

        foreach (var name in new[]
        {
            "add_sceneLoaded",
            "remove_sceneLoaded",
            "get_sceneLoaded",
            "set_sceneLoaded"
        })
        {
            var method = type.GetMethod(
                name,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static |
                BindingFlags.DeclaredOnly);

            if (method == null)
                continue;

            Console.WriteLine(method);

            foreach (var parameter in method.GetParameters())
            {
                Console.WriteLine(
                    $"  Parameter: {parameter.ParameterType.AssemblyQualifiedName}");
            }

            Console.WriteLine(
                $"  Return: {method.ReturnType.AssemblyQualifiedName}");
        }
    }

    Console.WriteLine();
    Console.WriteLine("=== UnityAction<T1,T2> ===");

    var unityAction = loadedTypes.FirstOrDefault(t =>
        t.FullName == "UnityEngine.Events.UnityAction`2");

    if (unityAction == null)
    {
        Console.WriteLine("UnityAction<T1,T2> type not found.");
    }
    else
    {
        Console.WriteLine("Type: " + unityAction.AssemblyQualifiedName);

        Console.WriteLine("-- Constructors --");

        foreach (var constructor in unityAction.GetConstructors(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance))
        {
            Console.WriteLine(constructor);
        }

        Console.WriteLine("-- Invoke method --");

        var invoke = unityAction.GetMethod("Invoke");

        Console.WriteLine(invoke);

        if (invoke != null)
        {
            foreach (var parameter in invoke.GetParameters())
            {
                Console.WriteLine(
                    $"  Parameter: {parameter.ParameterType.AssemblyQualifiedName} {parameter.Name}");
            }
        }
    }

    Console.WriteLine();
    Console.WriteLine("=== Members containing SceneLoaded ===");

    foreach (var type in loadedTypes)
    {
        foreach (var member in type.GetMembers(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Static |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly)
            .Where(m =>
                m.Name.Contains(
                    "sceneLoaded",
                    StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(type.FullName + " :: " + member);
        }
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine("INSPECTOR ERROR:");
    Console.WriteLine(ex);
    return 1;
}
