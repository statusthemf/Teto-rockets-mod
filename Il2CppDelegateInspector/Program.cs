using System.Reflection;
using System.Runtime.Loader;

if (args.Length < 1)
{
Console.Error.WriteLine("Usage: Il2CppDelegateInspector <path-to-Il2CppInterop.Runtime.dll>");
return 2;
}

var targetPath = Path.GetFullPath(args[0]);
var targetDirectory = Path.GetDirectoryName(targetPath)!;

AssemblyLoadContext.Default.Resolving += (_, name) =>
{
var candidate = Path.Combine(targetDirectory, name.Name + ".dll");
return File.Exists(candidate)
? AssemblyLoadContext.Default.LoadFromAssemblyPath(candidate)
: null;
};

try
{
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(targetPath);

Console.WriteLine($"Assembly: {assembly.FullName}");

var type = assembly.GetType(
    "Il2CppInterop.Runtime.DelegateSupport",
    throwOnError: true)!;

Console.WriteLine($"TYPE: {type.FullName}");
Console.WriteLine();

const BindingFlags flags =
    BindingFlags.Public |
    BindingFlags.NonPublic |
    BindingFlags.Static |
    BindingFlags.Instance |
    BindingFlags.DeclaredOnly;

foreach (var method in type.GetMethods(flags).OrderBy(m => m.MetadataToken))
{
    try
    {
        var generic = method.IsGenericMethodDefinition
            ? "<" + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ">"
            : "";

        var parameters = string.Join(
            ", ",
            method.GetParameters().Select(FormatParameter));

        Console.WriteLine(
            $"{Visibility(method)} {(method.IsStatic ? "static " : "")}{FormatType(method.ReturnType)} {method.Name}{generic}({parameters})");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Formatting error: {ex.Message}]");
    }
}

Console.WriteLine();
Console.WriteLine("Related delegate types:");

foreach (var related in assembly.GetTypes()
    .Where(t => t.FullName != null &&
                t.FullName.Contains("Delegate", StringComparison.OrdinalIgnoreCase))
    .OrderBy(t => t.FullName))
{
    Console.WriteLine($"- {related.FullName}");
}

return 0;

}
catch (Exception ex)
{
Console.Error.WriteLine(ex);
return 1;
}

static string FormatParameter(ParameterInfo p) =>
$"{(p.IsOut ? "out " : p.ParameterType.IsByRef ? "ref " : "")}{FormatType(p.ParameterType)} {p.Name}";

static string FormatType(Type t)
{
if (t.IsByRef)
return FormatType(t.GetElementType()!) + "&";

if (t.IsArray)
    return FormatType(t.GetElementType()!) + "[]";

if (t.IsGenericParameter)
    return t.Name;

if (!t.IsGenericType)
    return t.FullName ?? t.Name;

var name = t.GetGenericTypeDefinition().FullName ?? t.Name;
var tick = name.IndexOf("`", StringComparison.Ordinal);

if (tick >= 0)
    name = name[..tick];

return name + "<" +
    string.Join(", ", t.GetGenericArguments().Select(FormatType)) + ">";

}

static string Visibility(MethodBase m) =>
m.IsPublic ? "public" :
m.IsFamily ? "protected" :
m.IsAssembly ? "internal" :
m.IsPrivate ? "private" : "nonpublic";
