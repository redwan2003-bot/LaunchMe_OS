using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using LaunchMe.Contracts.Indexing;

namespace LaunchMe.Platform.Win32.Shell;

/// <summary>
/// Extracts high-resolution shell icons and Explorer thumbnails via IShellItemImageFactory
/// and encodes them as transparent 32bpp RGBA PNG bytes without System.Drawing dependencies.
/// </summary>
public sealed class Win32ShellIconService : IShellIconExtractor
{
    private const uint BiRgb = 0;
    private const uint DibRgbColors = 0;
    private const int PngBytesPerPixel = 4;

    private static readonly Guid ShellItemImageFactoryIid = new("bcc18b79-ba16-442f-80c4-8a59c30c463b");
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly uint[] Crc32Table = BuildCrc32Table();

    public byte[]? ExtractHighResPng(string filePath, int targetSize = 256, bool thumbnailIfAvailable = true)
    {
        if (string.IsNullOrWhiteSpace(filePath) || targetSize <= 0)
        {
            return null;
        }

        if (!File.Exists(filePath) && !Directory.Exists(filePath))
        {
            return null;
        }

        IShellItemImageFactory? factory = null;
        nint hBitmap = nint.Zero;

        try
        {
            int hr = NativeMethods.SHCreateItemFromParsingName(
                filePath,
                nint.Zero,
                ShellItemImageFactoryIid,
                out factory);

            if (hr != 0 || factory is null)
            {
                return null;
            }

            var requestedSize = new NativeMethods.SIZE
            {
                cx = targetSize,
                cy = targetSize
            };

            hBitmap = AcquireBitmapHandle(factory, requestedSize, thumbnailIfAvailable);
            if (hBitmap == nint.Zero)
            {
                return null;
            }

            return ConvertHBitmapToPng(hBitmap);
        }
        catch
        {
            // Degrade cleanly on COM/Shell failures or headless environments without Shell image providers.
            return null;
        }
        finally
        {
            if (hBitmap != nint.Zero)
            {
                _ = NativeMethods.DeleteObject(hBitmap);
            }

            if (factory is not null && Marshal.IsComObject(factory))
            {
                Marshal.ReleaseComObject(factory);
            }
        }
    }

    private static nint AcquireBitmapHandle(
        IShellItemImageFactory factory,
        NativeMethods.SIZE requestedSize,
        bool thumbnailIfAvailable)
    {
        if (thumbnailIfAvailable)
        {
            // Prefer Shell default (thumbnail if available, otherwise icon), then explicit thumbnail, then icon-only.
            if (TryGetImage(factory, requestedSize, SIIGBF.SIIGBF_BIGGERSIZEOK, out nint hBitmap))
            {
                return hBitmap;
            }

            if (TryGetImage(factory, requestedSize, SIIGBF.SIIGBF_BIGGERSIZEOK | SIIGBF.SIIGBF_THUMBNAILONLY, out hBitmap))
            {
                return hBitmap;
            }

            if (TryGetImage(factory, requestedSize, SIIGBF.SIIGBF_BIGGERSIZEOK | SIIGBF.SIIGBF_ICONONLY, out hBitmap))
            {
                return hBitmap;
            }

            return nint.Zero;
        }

        if (TryGetImage(factory, requestedSize, SIIGBF.SIIGBF_BIGGERSIZEOK | SIIGBF.SIIGBF_ICONONLY, out nint iconBitmap))
        {
            return iconBitmap;
        }

        if (TryGetImage(factory, requestedSize, SIIGBF.SIIGBF_BIGGERSIZEOK, out iconBitmap))
        {
            return iconBitmap;
        }

        return nint.Zero;
    }

    private static bool TryGetImage(
        IShellItemImageFactory factory,
        NativeMethods.SIZE size,
        SIIGBF flags,
        out nint hBitmap)
    {
        hBitmap = nint.Zero;
        int hr = factory.GetImage(size, flags, out nint candidate);
        if (hr == 0 && candidate != nint.Zero)
        {
            hBitmap = candidate;
            return true;
        }

        if (candidate != nint.Zero)
        {
            _ = NativeMethods.DeleteObject(candidate);
        }

        return false;
    }

    private static byte[]? ConvertHBitmapToPng(nint hBitmap)
    {
        if (NativeMethods.GetObjectW(hBitmap, Marshal.SizeOf<NativeMethods.BITMAP>(), out var bitmapInfo) == 0)
        {
            return null;
        }

        int width = bitmapInfo.bmWidth;
        int height = bitmapInfo.bmHeight;
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        long totalBytesLong = (long)width * height * PngBytesPerPixel;
        if (totalBytesLong <= 0 || totalBytesLong > int.MaxValue)
        {
            return null;
        }

        byte[] bgraPixels = new byte[(int)totalBytesLong];

        nint hdc = NativeMethods.GetDC(nint.Zero);
        if (hdc == nint.Zero)
        {
            return null;
        }

        try
        {
            var bmi = new NativeMethods.BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height, // Top-down scanlines so row 0 is the top of the image.
                biPlanes = 1,
                biBitCount = 32,
                biCompression = BiRgb,
                biSizeImage = (uint)bgraPixels.Length
            };

            int scanLines = NativeMethods.GetDIBits(
                hdc,
                hBitmap,
                0,
                (uint)height,
                bgraPixels,
                ref bmi,
                DibRgbColors);

            if (scanLines == 0)
            {
                return null;
            }
        }
        finally
        {
            _ = NativeMethods.ReleaseDC(nint.Zero, hdc);
        }

        ConvertBgraToRgbaAndNormalizeAlpha(bgraPixels);
        return EncodeRgbaToPng(bgraPixels, width, height);
    }

    private static void ConvertBgraToRgbaAndNormalizeAlpha(byte[] pixels)
    {
        bool hasNonZeroAlpha = false;
        bool hasNonZeroRgb = false;

        for (int i = 0; i < pixels.Length; i += PngBytesPerPixel)
        {
            byte b = pixels[i];
            byte g = pixels[i + 1];
            byte r = pixels[i + 2];
            byte a = pixels[i + 3];

            pixels[i] = r;
            pixels[i + 1] = g;
            pixels[i + 2] = b;
            pixels[i + 3] = a;

            if (a != 0)
            {
                hasNonZeroAlpha = true;
            }

            if ((r | g | b) != 0)
            {
                hasNonZeroRgb = true;
            }
        }

        // Legacy non-alpha GDI bitmaps return alpha=0 across every pixel; promote to opaque so the icon remains visible.
        if (!hasNonZeroAlpha && hasNonZeroRgb)
        {
            for (int i = 3; i < pixels.Length; i += PngBytesPerPixel)
            {
                pixels[i] = 255;
            }
        }
    }

    private static byte[] EncodeRgbaToPng(byte[] rgbaPixels, int width, int height)
    {
        using var output = new MemoryStream();
        output.Write(PngSignature);

        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr[0..4], width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr[4..8], height);
        ihdr[8] = 8;  // Bit depth: 8
        ihdr[9] = 6;  // Color type: 6 (Truecolor with alpha)
        ihdr[10] = 0; // Compression method: deflate
        ihdr[11] = 0; // Filter method: adaptive (0)
        ihdr[12] = 0; // Interlace method: none
        WriteChunk(output, "IHDR"u8, ihdr);

        byte[] compressedIdat;
        using (var idatStream = new MemoryStream())
        {
            using (var zlib = new ZLibStream(idatStream, CompressionLevel.Fastest, leaveOpen: true))
            {
                int stride = width * PngBytesPerPixel;
                byte[] scanline = new byte[1 + stride];
                scanline[0] = 0; // Filter type 0 (None)

                for (int y = 0; y < height; y++)
                {
                    Buffer.BlockCopy(rgbaPixels, y * stride, scanline, 1, stride);
                    zlib.Write(scanline, 0, scanline.Length);
                }
            }

            compressedIdat = idatStream.ToArray();
        }

        WriteChunk(output, "IDAT"u8, compressedIdat);
        WriteChunk(output, "IEND"u8, ReadOnlySpan<byte>.Empty);

        return output.ToArray();
    }

    private static void WriteChunk(Stream output, ReadOnlySpan<byte> chunkType, ReadOnlySpan<byte> data)
    {
        Span<byte> lenBytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lenBytes, data.Length);
        output.Write(lenBytes);
        output.Write(chunkType);
        output.Write(data);

        uint crc = 0xFFFFFFFFu;
        crc = UpdateCrc32(crc, chunkType);
        crc = UpdateCrc32(crc, data);
        crc ^= 0xFFFFFFFFu;

        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        output.Write(crcBytes);
    }

    private static uint UpdateCrc32(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (byte b in data)
        {
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] BuildCrc32Table()
    {
        var table = new uint[256];
        for (uint n = 0; n < table.Length; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }

    [Flags]
    internal enum SIIGBF : uint
    {
        SIIGBF_RESIZETOFIT = 0x00000000,
        SIIGBF_BIGGERSIZEOK = 0x00000001,
        SIIGBF_MEMORYONLY = 0x00000002,
        SIIGBF_ICONONLY = 0x00000004,
        SIIGBF_THUMBNAILONLY = 0x00000008,
        SIIGBF_INCACHEONLY = 0x00000010,
        SIIGBF_CROPTOSQUARE = 0x00000020,
        SIIGBF_WIDETHUMBNAILS = 0x00000040,
        SIIGBF_ICONBACKGROUND = 0x00000080,
        SIIGBF_SCALEUP = 0x00000100
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    internal interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(NativeMethods.SIZE size, SIIGBF flags, out nint phbm);
    }

    internal static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct SIZE
        {
            public int cx;
            public int cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public ushort bmPlanes;
            public ushort bmBitsPixel;
            public nint bmBits;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        internal static extern int SHCreateItemFromParsingName(
            string pszPath,
            nint pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory? ppv);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(nint hObject);

        [DllImport("gdi32.dll", EntryPoint = "GetObjectW", SetLastError = true)]
        internal static extern int GetObjectW(nint hObject, int cbBuffer, out BITMAP lpvObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern int GetDIBits(
            nint hdc,
            nint hbm,
            uint start,
            uint cLines,
            [Out] byte[] lpvBits,
            ref BITMAPINFOHEADER lpbmi,
            uint usage);

        [DllImport("user32.dll")]
        internal static extern nint GetDC(nint hWnd);

        [DllImport("user32.dll")]
        internal static extern int ReleaseDC(nint hWnd, nint hDC);
    }
}
