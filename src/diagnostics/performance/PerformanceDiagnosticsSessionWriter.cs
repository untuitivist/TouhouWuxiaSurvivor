using System.Text;
using System.Text.Json;

namespace TouhouWuxiaSurvivor.Diagnostics.Performance;

/// <summary>
/// 以 UTF-8 无 BOM 和缓冲 JSONL 写入诊断记录，避免逐帧控制台输出反过来制造性能问题。
/// </summary>
public sealed class PerformanceDiagnosticsSessionWriter : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly StreamWriter _writer;
    private int _recordsSinceFlush;
    private bool _disposed;

    public string FilePath { get; }

    /// <summary>
    /// 创建唯一会话文件并保持其他进程可读，测试者可在游戏运行时直接复制已经刷新的日志。
    /// </summary>
    public static PerformanceDiagnosticsSessionWriter Create(string directory, string label)
    {
        Directory.CreateDirectory(directory);
        string safeLabel = SanitizeLabel(label);
        string stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff");
        string suffix = Guid.NewGuid().ToString("N")[..8];
        string path = Path.Combine(directory,
            $"performance-{stamp}-{safeLabel}-{suffix}.jsonl");
        return new PerformanceDiagnosticsSessionWriter(path);
    }

    /// <summary>
    /// 打开指定的新文件；FileMode.CreateNew 保证不会覆盖任何既有测试者日志。
    /// </summary>
    private PerformanceDiagnosticsSessionWriter(string path)
        : this(new FileStream(path, FileMode.CreateNew, FileAccess.Write,
            FileShare.Read, 4096, FileOptions.SequentialScan), path, false)
    {
    }

    /// <summary>
    /// 用指定流创建和正式实现相同编码的测试写入器，避免测试为了验证字节格式制造临时文件。
    /// </summary>
    internal static PerformanceDiagnosticsSessionWriter CreateForTesting(Stream stream) =>
        new(stream, "memory://performance-diagnostics.jsonl", true);

    /// <summary>
    /// 统一正式文件与内存测试的 UTF-8 无 BOM 写入配置，并明确底层流的所有权。
    /// </summary>
    private PerformanceDiagnosticsSessionWriter(Stream stream, string path, bool leaveOpen)
    {
        FilePath = path;
        _writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen);
    }

    /// <summary>
    /// 写入单行 JSON，并每五条主动刷新一次，在低磁盘开销和崩溃可恢复性之间保持平衡。
    /// </summary>
    public void Write<T>(T record)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _writer.WriteLine(SerializeRecord(record));
        _recordsSinceFlush++;
        if (_recordsSinceFlush >= 5)
        {
            Flush();
        }
    }

    /// <summary>
    /// 以和正式日志完全相同的配置序列化单条记录，供无磁盘测试验证字段契约。
    /// </summary>
    public static string SerializeRecord<T>(T record) =>
        JsonSerializer.Serialize(record, JsonOptions);

    /// <summary>
    /// 把缓冲内容提交到文件并重置计数，供定期刷新和正常退出共同调用。
    /// </summary>
    public void Flush()
    {
        if (_disposed) return;
        _writer.Flush();
        _recordsSinceFlush = 0;
    }

    /// <summary>
    /// 正常关闭会话文件；重复释放保持幂等，避免场景退出与进程退出竞态。
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _writer.Flush();
        _writer.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// 把用户标签限制为短 ASCII 文件名片段，防止命令行内容形成无效路径。
    /// </summary>
    private static string SanitizeLabel(string label)
    {
        string value = new(label.Where(character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_').Take(32).ToArray());
        return string.IsNullOrWhiteSpace(value) ? "default" : value;
    }
}
