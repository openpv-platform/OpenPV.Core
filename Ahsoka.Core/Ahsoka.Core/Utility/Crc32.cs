using System;
using System.IO;

namespace Ahsoka.Utility;

internal static class Crc32
{
    #region Fields
    private const UInt32 Crc32Polynomial = 0x04C11DB7;
    public const UInt32 Crc32Seed = 0xEDB88320;
    private static readonly UInt32[] Crc32Table = GenerateCrcTable(4);
    #endregion

    #region Methods
    private static UInt32[] GenerateCrcTable(int size)
    {
        UInt32 index;
        UInt32 mask = (1U << (size * 8 - 1));
        int left = (size - 1) * 8;

        UInt32[] crc32Table = new UInt32[256];

        for (index = 0; index < 256; ++index)
        {
            int i;
            UInt32 r = index << left;

            for (i = 0; i < 8; ++i)
                if ((r & mask) != 0)
                    r = (r << 1) ^ Crc32Polynomial;
                else
                    r <<= 1;
            crc32Table[index] = r;
        }
        return crc32Table;
    }

    public static UInt32 SetCrc32InitValue(UInt32 start)
    {
        UInt32 init = 0;
        UInt32 bite;
        int val;

        if (start > 0)
        {
            val = 32;
            while (val > 0)
            {
                val -= 8;
                bite = (start >> val) & 0xFF;
                init = Crc32Table[((init >> 24) ^ bite) & 0xFF] ^ (init << 8);
            }
        }
        return init;
    }

    public static UInt32 CalculateCrc32(Stream data, bool seekStart = true, int lengthToProcess = -1)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        UInt32 sum = Crc32.SetCrc32InitValue(Crc32.Crc32Seed);

        byte[] buffer = new byte[32768];

        int lengthRemaining = lengthToProcess == -1 ? (int)data.Length : lengthToProcess;

        if (seekStart)
            data.Seek(0, SeekOrigin.Begin);

        while (lengthRemaining > 0)
        {
            int lengthToRead = Math.Min(lengthRemaining, buffer.Length);

            int readCount = data.Read(buffer, 0, lengthToRead);

            for (int i = 0; i < readCount; i++)
                sum = Crc32Table[(sum >> 24) ^ buffer[i]] ^ (sum << 8);

            lengthRemaining -= readCount;

        }


        return sum;
    }
    #endregion
}
