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
    var core = AssemblyLoadContext.Default.LoadFromAssemblyPath(
        Path.GetFullPath(args[1]));

    Console.WriteLine("=== SceneManager types ===");

    var matches = core.GetTypes()
        .Where(t => t.FullName != null &&
            t.FullName.Contains("SceneManager", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    foreach (var type in matches)
    {
        Console.WriteLine();
        Console.WriteLine("TYPE: " + type.FullName);

        Console.WriteLine("-- Events --");
        foreach (var e in type.GetEvents(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance |
            BindingFlags.DeclaredOnly))
        {
            Console.WriteLine(e);
            Console.WriteLine("  Handler: " + e.EventHandlerType);
            Console.WriteLine("  Add: " + e.GetAddMethod(true));
        }

        Console.WriteLine("-- Relevant methods --");
        foreach (var method in type.GetMethods(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance |
            BindingFlags.DeclaredOnly)
            .Where(m => m.Name.Contains("sceneLoaded",
                StringComparison.OrdinalIgnoreCase) ||
                m.Name.Contains("SceneLoaded",
                StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(method);
        }
    }

    Console.WriteLine();
    Console.WriteLine("=== Types containing SceneLoaded in their members ===");

    foreach (var type in core.GetTypes())
    {
        var members = type.GetMembers(
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance |
            BindingFlags.DeclaredOnly);

        foreach (var member in members.Where(m =>
            m.Name.Contains("sceneLoaded",
                StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine(type.FullName + " :: " + member);
        }
    }

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return 1;
}
