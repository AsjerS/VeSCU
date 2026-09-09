using System.Buffers;
using System.Buffers.Binary;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.IO.Hashing;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace VeSCU;

internal static class BitmapStreamExtensions
{
    public static void WriteAs(this Bitmap bitmap, AppConfig.InputFormat format, Stream stream)
    {
        switch (format)
        {
            case AppConfig.InputFormat.Ppm:
                bitmap.WriteAsPpm(stream);
                break;

            case AppConfig.InputFormat.Png:
                bitmap.WriteAsPng(stream);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported format");
        }
    }

    private static void WriteAsPpm(this Bitmap bitmap, Stream destination)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        int rowByteCount = width * 3;

        // write header
        Span<byte> header = stackalloc byte[64];
        int headerLen = System.Text.Encoding.ASCII.GetBytes($"P6\n{width} {height}\n255\n", header);
        destination.Write(header[..headerLen]);

        // lock bitmap to access raw memory later
        BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb
        );

        // rent a buffer
        byte[] rowBuffer = ArrayPool<byte>.Shared.Rent(rowByteCount);

        // define a shuffle mask for SSE3
        Vector128<byte> shuffleMask = Vector128.Create(
            2, 1, 0,
            6, 5, 4,
            10, 9, 8,
            14, 13, 12,
            0xFF, 0xFF, 0xFF, 0xFF
        );

        // do the conversion
        try
        {
            unsafe
            {
                byte* scan0 = (byte*)data.Scan0.ToPointer();
                int stride = data.Stride;

                fixed (byte* pRowBuffer = rowBuffer)
                {
                    for (int y = 0; y < height; y++)
                    {
                        byte* src = scan0 + (y * stride);
                        byte* dst = pRowBuffer;
                        int x = 0;

                        if (Vector128.IsHardwareAccelerated)
                        {
                            for (; x <= width - 4; x += 4)
                            {
                                Vector128<byte> bgra = Sse2.LoadVector128(src);
                                Vector128<byte> rgb = Ssse3.Shuffle(bgra, shuffleMask);

                                *(long*)dst = rgb.AsInt64().GetElement(0);      // first 8 bytes
                                *(int*)(dst + 8) = rgb.AsInt32().GetElement(2); // last 4 bytes

                                src += 16;
                                dst += 12;
                            }
                        }

                        // fallback
                        for (; x < width; x++)
                        {
                            dst[0] = src[2]; // red
                            dst[1] = src[1]; // green
                            dst[2] = src[0]; // blue
                            src += 4;
                            dst += 3;
                        }

                        destination.Write(rowBuffer, 0, rowByteCount);
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rowBuffer);
            bitmap.UnlockBits(data);
        }
    }

    private static void WriteAsPng(this Bitmap bitmap, Stream destination)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        // write header
        destination.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR chunk
        byte[] ihdrData = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdrData.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(ihdrData.AsSpan(4, 4), height);
        ihdrData[8] = 8; // 8 bit
        ihdrData[9] = 2; // RGB
        WriteChunk(destination, "IHDR", ihdrData);

        // IDAT chunk
        using (var idatStream = new MemoryStream())
        {
            using (var zlib = new ZLibStream(idatStream, CompressionLevel.NoCompression, leaveOpen: true))
            {
                BitmapData data = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb
                );

                byte[] rowBuffer = ArrayPool<byte>.Shared.Rent(width * 3 + 1);

                try
                {
                    unsafe
                    {
                        byte* scan0 = (byte*)data.Scan0.ToPointer();
                        int stride = data.Stride;

                        for (int y = 0; y < height; y++)
                        {
                            byte* row = scan0 + (y * stride);
                            rowBuffer[0] = 0;
                            int bufferIndex = 1;

                            for (int x = 0; x < width; x++)
                            {
                                int pixelIndex = x * 4;
                                rowBuffer[bufferIndex++] = row[pixelIndex + 2];
                                rowBuffer[bufferIndex++] = row[pixelIndex + 1];
                                rowBuffer[bufferIndex++] = row[pixelIndex];
                            }

                            zlib.Write(rowBuffer, 0, width * 3 + 1);
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rowBuffer);
                    bitmap.UnlockBits(data);
                }
            }

            WriteChunk(
                destination,
                "IDAT",
                idatStream.TryGetBuffer(out ArraySegment<byte> segment)
                    ? segment.AsSpan()
                    : idatStream.ToArray()
            );
        }

        // IEND chunk
        WriteChunk(destination, "IEND", []);

        static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
        {
            Span<byte> lengthBytes = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(lengthBytes, data.Length);
            stream.Write(lengthBytes);

            Span<byte> typeBytes = stackalloc byte[4];
            System.Text.Encoding.ASCII.GetBytes(type, typeBytes);
            stream.Write(typeBytes);

            if (!data.IsEmpty) stream.Write(data);

            var crc = new Crc32();
            crc.Append(typeBytes);
            crc.Append(data);

            Span<byte> crcBytes = stackalloc byte[4];
            crc.GetCurrentHash(crcBytes);
            crcBytes.Reverse();

            stream.Write(crcBytes);
        }
    }
}
