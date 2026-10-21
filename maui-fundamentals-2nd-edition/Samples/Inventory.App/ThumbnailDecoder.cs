using MauiInventory.Core;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace MauiInventory.App;
public static class ThumbnailDecoder
{
    public static async Task<byte[]> DecodePublicAssetAsync(ThumbnailKey key, CancellationToken token)
    {
        // Fixed allowlist: callers cannot turn the asset ID into an arbitrary file path or URL.
        if (key.PublicAssetId != "sample-thumbnail")
            throw new ArgumentException("Unknown public image asset.", nameof(key));
        await using var source = await FileSystem.OpenAppPackageFileAsync("sample-thumbnail.png");
        token.ThrowIfCancellationRequested();
        using IImage image = PlatformImage.FromStream(source);
        IImage resized = image.Downsize(key.WidthPixels, key.HeightPixels);
        try
        {
            using var output = new MemoryStream();
            resized.Save(output, ImageFormat.Png);
            token.ThrowIfCancellationRequested();
            return output.ToArray();
        }
        finally
        {
            if (!ReferenceEquals(image, resized))
                resized.Dispose();
        }
    }
}
