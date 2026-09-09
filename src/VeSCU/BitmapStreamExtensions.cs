using System.Buffers;
using System.Buffers.Binary;
using System.Drawing.Imaging;
using System.IO.Compression;

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
        byte[] header = System.Text.Encoding.ASCII.GetBytes($"P6\n{width} {height}\n255\n");
        destination.Write(header, 0, header.Length);

        // lock bitmap to access raw memory later
        BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb
        );

        // rent a buffer
        byte[] rowBuffer = ArrayPool<byte>.Shared.Rent(rowByteCount);

        // do the conversion
        try
        {
            unsafe
            {
                byte* scan0 = (byte*)data.Scan0.ToPointer();
                int stride = data.Stride;

                for (int y = 0; y < height; y++)
                {
                    byte* row = scan0 + (y * stride);
                    int bufferIndex = 0;

                    for (int x = 0; x < width; x++)
                    {
                        int pixelIndex = x * 4;

                        // BGRA -> RGB
                        rowBuffer[bufferIndex++] = row[pixelIndex + 2]; // red
                        rowBuffer[bufferIndex++] = row[pixelIndex + 1]; // green
                        rowBuffer[bufferIndex++] = row[pixelIndex];     // blue
                    }

                    destination.Write(rowBuffer, 0, rowByteCount);
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

            WriteChunk(destination, "IDAT", idatStream.ToArray());
        }

        // IEND chunk
        WriteChunk(destination, "IEND", []);

        static void WriteChunk(Stream stream, string type, byte[] data)
        {
            byte[] lengthBytes = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(lengthBytes, data.Length);
            stream.Write(lengthBytes);

            byte[] typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
            stream.Write(typeBytes);

            if (data.Length > 0)
            {
                stream.Write(data);
            }

            uint crc = CalculateCrc32(typeBytes, data);
            byte[] crcBytes = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
            stream.Write(crcBytes);
        }
    }

    private static uint CalculateCrc32(byte[] type, byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        void Update(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                crc ^= bytes[i];
                for (int j = 0; j < 8; j++)
                    crc = (crc >> 1) ^ ((crc & 1) * 0xEDB88320);
            }
        }
        Update(type);
        Update(data);
        return ~crc;
    }
}