using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace TouhouWuxiaSurvivor.Tools.TileGenerator;

/// <summary>
/// 将 PixelCanvas 编码为标准 RGBA8 PNG，包括 zlib 数据流和逐块 CRC 校验。
/// </summary>
internal static class PngWriter
{
    private static readonly byte[] Signature =
    [
        137,
        80,
        78,
        71,
        13,
        10,
        26,
        10,
    ];

    /// <summary>
    /// 创建目标目录并写出完整 PNG 签名、IHDR、IDAT 与 IEND 数据块。
    /// </summary>
    public static void Write(PixelCanvas canvas, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        using FileStream stream = File.Create(outputPath);
        stream.Write(Signature);
        WriteHeader(stream, canvas.Width, canvas.Height);
        WriteImageData(stream, canvas);
        WriteChunk(stream, "IEND", []);
    }

    /// <summary>
    /// 写出 8 位 RGBA、无隔行扫描的 PNG 图像头。
    /// </summary>
    private static void WriteHeader(Stream stream, int width, int height)
    {
        byte[] header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(0, 4), (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4, 4), (uint)height);
        header[8] = 8;
        header[9] = 6;
        header[10] = 0;
        header[11] = 0;
        header[12] = 0;
        WriteChunk(stream, "IHDR", header);
    }

    /// <summary>
    /// 为每行添加 None 过滤器字节，使用 zlib 压缩后写入 IDAT 块。
    /// </summary>
    private static void WriteImageData(Stream stream, PixelCanvas canvas)
    {
        int stride = canvas.Width * 4;
        byte[] scanlines = new byte[(stride + 1) * canvas.Height];
        ReadOnlySpan<byte> pixels = canvas.Pixels;

        for (int y = 0; y < canvas.Height; y++)
        {
            int outputOffset = y * (stride + 1);
            scanlines[outputOffset] = 0;
            pixels.Slice(y * stride, stride).CopyTo(scanlines.AsSpan(outputOffset + 1));
        }

        using MemoryStream compressed = new();
        using (ZLibStream zlib = new(compressed, CompressionLevel.SmallestSize, true))
        {
            zlib.Write(scanlines);
        }

        WriteChunk(stream, "IDAT", compressed.ToArray());
    }

    /// <summary>
    /// 按 PNG 大端格式写入长度、四字符类型、数据和 CRC。
    /// </summary>
    private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
    {
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        Span<byte> lengthBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(lengthBytes, (uint)data.Length);
        stream.Write(lengthBytes);
        stream.Write(typeBytes);
        stream.Write(data);

        uint crc = ComputeCrc(typeBytes, data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }

    /// <summary>
    /// 计算覆盖 PNG 块类型与数据的 IEEE CRC-32，并执行最终异或。
    /// </summary>
    private static uint ComputeCrc(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFFu;
        crc = UpdateCrc(crc, type);
        crc = UpdateCrc(crc, data);
        return crc ^ 0xFFFFFFFFu;
    }

    /// <summary>
    /// 用反射多项式 0xEDB88320 逐字节更新 CRC-32 状态。
    /// </summary>
    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (byte value in data)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
            {
                uint mask = (uint)-(int)(crc & 1u);
                crc = (crc >> 1) ^ (0xEDB88320u & mask);
            }
        }

        return crc;
    }
}
