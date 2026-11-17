using SkiaSharp;

namespace MyAcademy_PortfolioProject.Infrastructure;

public static class UploadSafety
{
    public const long ImageLimit = 5 * 1024 * 1024;
    public const long PdfLimit = 10 * 1024 * 1024;
    public const long RequestLimit = 16 * 1024 * 1024;
    private static readonly SemaphoreSlim DecodeSlots = new(2);

    public static async Task<string?> ValidateImageAsync(IFormFile? file)
    {
        if (file == null) return null;
        try { await PrepareImageAsync(file); return null; }
        catch (InvalidDataException) { return "Geçerli bir JPG, PNG veya WEBP görsel seçiniz (en fazla 5 MB ve 16 megapiksel)."; }
    }

    public static async Task CopySafeImageAsync(IFormFile file, Stream destination)
    {
        var bytes = await PrepareImageAsync(file);
        await destination.WriteAsync(bytes);
    }

    private static async Task<byte[]> PrepareImageAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var expected = extension switch
        {
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            ".png" => SKEncodedImageFormat.Png,
            ".webp" => SKEncodedImageFormat.Webp,
            _ => throw new InvalidDataException()
        };
        var mime = expected == SKEncodedImageFormat.Jpeg ? "image/jpeg" : expected == SKEncodedImageFormat.Png ? "image/png" : "image/webp";
        CheckMime(file, mime);
        var bytes = await ReadBoundedAsync(file, ImageLimit);
        await DecodeSlots.WaitAsync();
        try
        {
            using var data = SKData.CreateCopy(bytes);
            using var codec = SKCodec.Create(data);
            if (codec == null || codec.EncodedFormat != expected || codec.Info.Width <= 0 || codec.Info.Height <= 0 ||
                codec.Info.Width > 8192 || codec.Info.Height > 8192 || (long)codec.Info.Width * codec.Info.Height > 16_000_000 || codec.FrameCount > 1)
                throw new InvalidDataException();
            var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var bitmap = new SKBitmap(info);
            if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success) throw new InvalidDataException();
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(expected, 90);
            if (encoded == null || encoded.Size > ImageLimit) throw new InvalidDataException();
            // A fresh encoding discards client metadata and any executable trailing payload.
            return encoded.ToArray();
        }
        finally { DecodeSlots.Release(); }
    }

    public static async Task<string?> ValidatePdfAsync(IFormFile? file)
    {
        if (file == null) return null;
        try
        {
            if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException();
            CheckMime(file, "application/pdf");
            var bytes = await ReadBoundedAsync(file, PdfLimit);
            if (bytes.Length < 10 || !bytes.AsSpan(0,5).SequenceEqual("%PDF-"u8)) throw new InvalidDataException();
            var tail = System.Text.Encoding.ASCII.GetString(bytes, Math.Max(0,bytes.Length-1024),Math.Min(1024,bytes.Length));
            if (!tail.TrimEnd().EndsWith("%%EOF",StringComparison.Ordinal)) throw new InvalidDataException();
            return null;
        }
        catch (InvalidDataException) { return "Geçerli bir PDF seçiniz (en fazla 10 MB)."; }
        // Signature/EOF checks are not a PDF sanitizer or malware scan. Serve as attachment + sandbox.
    }

    private static void CheckMime(IFormFile file, string expected)
    {
        var mime = file.ContentType?.Split(';')[0].Trim();
        // Browser MIME is untrusted; an absent/generic value is allowed only with content validation.
        if (!string.IsNullOrEmpty(mime) && !mime.Equals("application/octet-stream",StringComparison.OrdinalIgnoreCase) && !mime.Equals(expected,StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException();
    }

    private static async Task<byte[]> ReadBoundedAsync(IFormFile file, long maximum)
    {
        if (file.Length <= 0 || file.Length > maximum) throw new InvalidDataException();
        await using var input = file.OpenReadStream();
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(buffer)) > 0)
        {
            if (output.Length + read > maximum) throw new InvalidDataException();
            await output.WriteAsync(buffer.AsMemory(0,read));
        }
        if (output.Length != file.Length) throw new InvalidDataException();
        return output.ToArray();
    }
}
