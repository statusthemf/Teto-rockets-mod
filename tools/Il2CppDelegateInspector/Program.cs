using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

if (args.Length < 2)
{
    Console.WriteLine("Usage: Inspector <Il2CppInterop.Runtime.dll> <UnityEngine.CoreModule.dll>");
    return 1;
}

var directory = Path.GetDirectoryName(Path.GetFullPath(args[0]))!;

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
        "Il2CppInterop.Runtime.DelegateSupport", true)!;

    foreach (var method in support.GetMethods(
        BindingFlags.Public | BindingFlags.Static |
        BindingFlags.DeclaredOnly))
    {
        Console.WriteLine(method);
    }

    Console.WriteLine();
    Console.WriteLine("=== SceneManager.sceneLoaded ===");

    var sceneManager = core.GetType(
        "UnityEngine.SceneManagement.SceneManager", true)!;

    var eventInfo = sceneManager.GetEvent(
        "sceneLoaded",
        BindingFlags.Public | BindingFlags.Static);

    if (eventInfo == null)
    {
        Console.WriteLine("sceneLoaded event not found.");
        return 2;
    }

    Console.WriteLine("Event: " + eventInfo);
    Console.WriteLine("Handler type: " + eventInfo.EventHandlerType);

    var invoke = eventInfo.EventHandlerType?.GetMethod("Invoke");

    Console.WriteLine("Delegate Invoke: " + invoke);

    if (invoke != null)
    {
        foreach (var parameter in invoke.GetParameters())
            Console.WriteLine($"Parameter: {parameter.ParameterType} {parameter.Name}");
    }

    var addMethod = eventInfo.GetAddMethod(true);
    Console.WriteLine("Add method: " + addMethod);

    if (addMethod != null)
    {
        foreach (var parameter in addMethod.GetParameters())
            Console.WriteLine($"Add parameter: {parameter.ParameterType}");
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return 1;
}
