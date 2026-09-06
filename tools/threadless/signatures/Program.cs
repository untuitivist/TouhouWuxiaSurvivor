using System.Reflection;
using System.Text;

if (args.Length != 2) throw new ArgumentException("Expected GodotSharp assembly and output source paths.");
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
var signatures = new SortedSet<string>(StringComparer.Ordinal);
foreach (var type in assembly.GetTypes())
{
    foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        Collect(field.FieldType);
    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
    {
        Collect(method.ReturnType);
        foreach (var parameter in method.GetParameters()) Collect(parameter.ParameterType);
    }
}
if (!signatures.Contains("IL")) throw new InvalidOperationException("Missing instance lookup signature from GodotSharp.");
var output = new StringBuilder("using System.Runtime.InteropServices;\ninternal static class NativeSignatures\n{\n");
foreach (var signature in signatures)
{
    output.AppendLine("""    [DllImport("godot-signature-catalog")]""");
    output.Append("    internal static extern ").Append(ManagedType(signature[0])).Append(" Invoke_").Append(signature).Append('(');
    output.AppendJoin(", ", signature.Skip(1).Select((kind, index) => ManagedType(kind) + " argument" + index));
    output.AppendLine(");");
}
output.AppendLine("}");
var text = output.ToString();
if (!File.Exists(args[1]) || File.ReadAllText(args[1], Encoding.UTF8) != text)
    File.WriteAllText(args[1], text, new UTF8Encoding(false));
Console.WriteLine("GODOT_NATIVE_SIGNATURES " + signatures.Count + " " + string.Join(",", signatures));

void Collect(Type type)
{
    if (!type.IsFunctionPointer || !type.IsUnmanagedFunctionPointer) return;
    var parameters = type.GetFunctionPointerParameterTypes();
    var result = type.GetFunctionPointerReturnType();
    var prefix = IsAggregate(result) && ScalarType(result) == null ? "VI" : AbiType(result).ToString();
    signatures.Add(prefix + string.Concat(parameters.Select(AbiType)));
    Collect(type.GetFunctionPointerReturnType());
    foreach (var parameter in parameters) Collect(parameter);
}

char AbiType(Type type)
{
    if (type.IsByRef || type.IsPointer || type.IsFunctionPointer || type == typeof(IntPtr) || type == typeof(UIntPtr)) return 'I';
    if (type.IsEnum) return AbiType(Enum.GetUnderlyingType(type));
    if (type == typeof(void)) return 'V';
    if (type == typeof(long) || type == typeof(ulong)) return 'L';
    if (type == typeof(float)) return 'F';
    if (type == typeof(double)) return 'D';
    if (IsAggregate(type)) return ScalarType(type) is { } scalar ? AbiType(scalar) : 'I';
    if (type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint) || type == typeof(char)) return 'I';
    throw new NotSupportedException("Unsupported Godot native ABI type: " + type.FullName);
}

bool IsAggregate(Type type) => type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(void) && type != typeof(IntPtr) && type != typeof(UIntPtr);

Type? ScalarType(Type type)
{
    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    if (fields.Length != 1) return null;
    var fieldType = fields[0].FieldType;
    return IsAggregate(fieldType) ? ScalarType(fieldType) : fieldType;
}

string ManagedType(char kind) => kind switch
{
    'V' => "void", 'I' => "int", 'L' => "long", 'F' => "float", 'D' => "double",
    _ => throw new NotSupportedException("Unsupported ABI code: " + kind)
};
