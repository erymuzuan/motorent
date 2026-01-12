using System.IO.Compression;
using System.Text;

namespace MotoRent.Messaging;

/// <summary>
/// Extension methods for compressing and decompressing data.
/// </summary>
public static class CompressionExtensions
{
    public static async Task<byte[]> CompressAsync(this string value)
    {
        var content = Encoding.UTF8.GetBytes(value);
        var ms = new MemoryStream();
        var sw = new GZipStream(ms, CompressionMode.Compress);

        await sw.WriteAsync(content, 0, content.Length);
        sw.Close();

        content = ms.ToArray();

        ms.Close();
        await sw.DisposeAsync();
        await ms.DisposeAsync();
        return content;
    }

    public static async Task<byte[]> CompressAsync(this byte[] content)
    {
        var ms = new MemoryStream();
        var sw = new GZipStream(ms, CompressionMode.Compress);

        await sw.WriteAsync(content);
        sw.Close();

        var zipped = ms.ToArray();

        ms.Close();
        await sw.DisposeAsync();
        await ms.DisposeAsync();
        return zipped;
    }

    public static async Task<string> DecompressAsync(this byte[] content)
    {
        await using var stream = new MemoryStream(content);
        await using var destinationStream = new MemoryStream();
        await using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        try
        {
            await gzip.CopyToAsync(destinationStream);
        }
        catch (InvalidDataException)
        {
            // Data was not compressed, use original
            stream.Position = 0;
            await stream.CopyToAsync(destinationStream);
        }
        destinationStream.Position = 0;
        using var sr = new StreamReader(destinationStream, Encoding.UTF8);
        var json = await sr.ReadToEndAsync();
        return json;
    }

    public static string ReadString(this byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var sr = new StreamReader(stream);
        return sr.ReadToEnd();
    }
}
